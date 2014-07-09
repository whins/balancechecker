using HTTP.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

using NETCONLib;
using NetFwTypeLib;
using Npgsql;

namespace BalanceChecker
{
	partial class Service : ServiceBase
	{
		private List<UsbDevice> modemList;
		private HttpServer httpServer;
		public Service()
		{
			InitializeComponent();
			InitializeHTTPServer();
			

#if (DEBUG)
			StartHTTPServer();
			modemList = Modem.GetList();
#endif
			timer.Interval = Settings.Default.CheckerInterval * 1000;



		}

		protected override void OnStart(string[] args)
		{
			// TODO: Добавьте код для запуска службы.
			modemList = Modem.GetList();
			StartHTTPServer();

			if (Settings.Default.UseTimer)
			{
				timer.Start();
			}
		}

		protected override void OnStop()
		{
			timer.Stop();
			modemList = null;
		}

		private void InitializeHTTPServer()
		{
			if (0 < Settings.Default.HTTPPort && 65000 > Settings.Default.HTTPPort)
			{
				httpServer = new SimpleHttpServer(Settings.Default.HTTPPort);
			}
			else
			{
				httpServer = new SimpleHttpServer(8080);
			}
			httpServer.OnGETRequest += httpServer_OnGETRequest;
			httpServer.OnPOSTRequest += httpServer_OnPOSTRequest;
			httpServer.OnHTTPServerStarted += httpServer_OnHTTPServerStarted;
		}

		void httpServer_OnHTTPServerStarted(System.Net.Sockets.TcpListener listener)
		{
			Log.Write("Добавление правила в Windows Firewall...");
			Firewall.AddRule();
		}
		

		private void StartHTTPServer()
		{
			if (Settings.Default.UseHTTP)
			{
				Thread thread = new Thread(new ThreadStart(httpServer.listen));
				thread.Start();
			}
		}

		private void httpServer_OnPOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			switch (p.http_url)
			{
				case "/settings":
					{
						string data = inputData.ReadToEnd();
						var dataList = GetDataList(data);
						var command = (from item in dataList where item.Key == "command" select item).SingleOrDefault().Value;

						p.writeSuccess();
						Settings.Default.Save();
						p.outputStream.WriteLine("<html><body><h3>Balance Checker settings saved</h3>");
						p.outputStream.WriteLine("<a href=/>main</a><p>");
					}
					break;
				case "/sipgsmsettings":
					{
						string data = inputData.ReadToEnd();
						var dataList = GetDataList(data);
						p.outputStream.WriteLine("<html><body><h3>Balance Checker settings saved</h3>");
						p.outputStream.WriteLine("<a href=/>main</a><p>");
					}
					break;
				case "/conf":
					Log.Write("Получение конфигурации с сервера");
					string postData = inputData.ReadToEnd();
					try
					{
						Log.Write("Запись конфигурации в файл");
						File.WriteAllText(@"../Cfg/conf.xml", postData);
					}
					catch (Exception ex)
					{
						Log.Write("[Ошибка] :: " + ex.Message);
					}
					break;
				default:
					break;
			}
		}

		private void httpServer_OnGETRequest(HttpProcessor p)
		{
			switch (p.http_url)
			{
				case "/":
					{
						p.outputStream.WriteLine(@"
<html>
<head>" +
"\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/>\n" +
@"</head>
<body>
<h3>Balance Checker</h3>
</body>
</html>
");
					}
					break;
				case "/reconf":
					{
						p.writeSuccess();
						p.outputStream.WriteLine(@"
<html>
<head>" +
"\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/>\n" +
@"</head>
<body>
<h4>Добавлены конфигурации для IMEI :</h4>
");
						UpdateSipGSMConfig(p);
						p.outputStream.WriteLine(@"
<h4>Конфигурация обновлена!</h4>
</body>
</html>
");
					}
					break;
				case "/settings":
					{
						p.Show("html/settings.html");
					}
					break;
				case "/sipgsmsettings":
					{
						p.Show("html/sipgsmsettings.html");
					}
					break;
				case "/onoff":
					{
						OnOff();
						var result = CheckSipGsm();
						string htmlText = string.Format(Settings.Default.SipGsmStatusHtml, "Stopped" == result ? "#CC3300" : "#006600", result);
						p.writeSuccess();
						p.outputStream.WriteLine(htmlText);

					}
					break;
				case "/checksipgsm":
					{
						var result = CheckSipGsm();
						string htmlText = string.Format(Settings.Default.SipGsmStatusHtml, ("Stopped" == result ? "#CC3300" : "#006600"), result);
						p.writeSuccess();
						p.outputStream.WriteLine(htmlText);
					}
					break;
				case "/check":
					p.writeSuccess();
					p.outputStream.WriteLine(@"
<html>
<head>" +
"\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/>\n" +
@"<title>Checking Balance</title>
</head>
<body>
<h3>Processing...</h3>
");
					StartCheckBalance();
					p.outputStream.WriteLine(@"
</body>
</html>
");
					break;
				case "/favicon.ico" :
					Stream fs = File.Open("html/favicon.ico", FileMode.Open);
					p.writeSuccess("image/x-icon");
					fs.CopyTo (p.outputStream.BaseStream);
					p.outputStream.BaseStream.Flush ();
					break;
				default:
					p.writeSuccess();
					p.outputStream.WriteLine("<html><body><h5>Command \"" + p.http_url + "\" not founded</h5>");
					break;
			}
		}

		private string CheckSipGsm()
		{
			var serviceName = Settings.Default.SipGsmServiceName;
			ServiceController service = new ServiceController(serviceName);
			TimeSpan timeout = TimeSpan.FromMilliseconds(3000);
			return service.Status.ToString();
		}


		private void OnOff()
		{
			var serviceName = Settings.Default.SipGsmServiceName;
			ServiceController service = new ServiceController(serviceName);
			TimeSpan timeout = TimeSpan.FromMilliseconds(1000);
			switch (service.Status)
			{
				case ServiceControllerStatus.Running:
					try
					{
						service.Stop();						
					}
					catch
					{
						Log.Write("Error " + serviceName + " service stopping :( Exit...");
					}
					finally
					{
						service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
						Log.Write(serviceName + " " + service.Status.ToString());
					}
					break;
				case ServiceControllerStatus.Stopped:
					try
					{
						service.Start();
					}
					catch
					{
						Log.Write("Error " + serviceName + " service running :( Exit...");
					}
					finally
					{
						service.WaitForStatus(ServiceControllerStatus.Running, timeout);
						Log.Write(serviceName + " " + service.Status.ToString());
					}
					break;
				default :
					break;
			}
		}


		private string SipGSMPath
		{
			get
			{
				string p = "";
				if (8 == IntPtr.Size
					|| (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
				{
					p = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
				}
				else
				{
					p = Environment.GetEnvironmentVariable("ProgramFiles");
				}
				return p + @"\SipGSMGateway\Cfg\";
			}
		}

		private void UpdateSipGSMConfig(HttpProcessor p)
		{
			StopService(Settings.Default.SipGsmServiceName, 1000);

			Log.Write("Обновление списка модемов");
			modemList = null;
			modemList = Modem.GetList();
			int i = 1;

			Log.Write("Найдено модемов :: " + modemList.Count);
			p.outputStream.WriteLine(string.Format("<h5>Найдено модемов :: {0}</h5>", i));
			foreach (var item in modemList)
			{
				Log.Write(string.Format("{0} ::  {1}", i, item.IMEI));
				p.outputStream.WriteLine(string.Format("<h5>{0} ::  {1}</h5>", i, item.IMEI));
				i++;
			}
			


			DeleteConfigFiles(p);

			Thread.Sleep(5000);
			i = 1;
			foreach (UsbDevice item in modemList)
			{
				Log.Write("Чтение настроек сервера");

				using (var connection = PostgreSQL.Get())
				{

					try
					{
						Log.Write("Открытие соединения");
						connection.Open();
						NpgsqlCommand dbcmd = connection.CreateCommand();
#if DEBUG
						string imei = "354183026627980";
#else		
					string imei = item.IMEI;
#endif
						dbcmd.Parameters.AddWithValue("@imei", imei);

						dbcmd.CommandText = @"
SELECT
	id,
	port
FROM " + "\"Getaways\"" +
	@"
WHERE imei = @imei
";
						Log.Write("Чтение данных с сервера");
						NpgsqlDataReader reader = dbcmd.ExecuteReader();
						if(reader.Read())
						{
							string id = reader[0].ToString();
							string port = reader[1].ToString();
							Log.Write(string.Format("Добавление конфигурации для {0} :: p:{1}, id:{2}", imei, port, id));							
							WriteConfigFile(imei, port, id);
							p.outputStream.WriteLine(string.Format("<h5>Добавление конфигурации для {0} :: p:{1}, id:{2}<h5>", imei, port, id));
						}
					}
					catch (NpgsqlException ex)
					{
						Log.Write(ex.Message);
					}
					finally
					{
						Log.Write("Закрытие соединения");
						connection.Close();
					}
				}
				i++;
			}
			StartService(Settings.Default.SipGsmServiceName, 1000);			
		}

		private void DeleteConfigFiles(HttpProcessor p)
		{
			Log.Write("Удаление старых файлов конфигурации");
			int i = 0;
			if (Directory.Exists(SipGSMPath))
			{
				foreach (string path in Directory.GetFiles(SipGSMPath))
				{
					try
					{
						File.Delete(path);
						i++;
					}
					catch (Exception e)
					{
						p.outputStream.WriteLine(string.Format("<h5>{0} ::  {1}</h5>", "Ошибка", e.Message));
					}
					
				}
			}
			Log.Write(string.Format("Удалено файлов :: {0} ", i));
		}

		private void WriteConfigFile(string imei, string port, string id)
		{

			string configData = string.Format(Settings.Default.ConfigTemplate, port, id, id);

			try
			{
				Log.Write("Запись конфигурации в файл");
				File.WriteAllText(string.Format(SipGSMPath + @"{0}", imei), configData);
			}
			catch (Exception ex)
			{
				Log.Write("[Ошибка] :: " + ex.Message);
			}
		}

		private Dictionary<string, string> GetDataList(string data)
		{
			Dictionary<string, string> dataList = new Dictionary<string, string>();
			if (string.IsNullOrEmpty(data))
			{
				return dataList;
			}

			foreach (var item in data.Split('&'))
			{
				var s = item.Split('=');
				dataList.Add(s[0], s[1]);
			}

			return dataList;
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			StartCheckBalance();
		}

		private void StartCheckBalance()
		{
			StopService(Settings.Default.SipGsmServiceName, 1000);
			foreach (UsbDevice item in modemList)
			{
				item.StartProcesses();
			}
			Thread.Sleep(Settings.Default.ServiceStopTime * 1000);
			StartService(Settings.Default.SipGsmServiceName, 1000);
		}

		public static void StartService(string serviceName, int timeoutMilliseconds)
		{
			Log.Write(serviceName + " service starting...");
			ServiceController service = new ServiceController(serviceName);
			try
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

				service.Start();
				service.WaitForStatus(ServiceControllerStatus.Running, timeout);
				Log.Write(serviceName + " service started!");
			}
			catch
			{
				Log.Write("Error " + serviceName + " service starting :(");
			}
		}

		public static void StopService(string serviceName, int timeoutMilliseconds)
		{
			Log.Write(serviceName + " service stopping...");
			ServiceController service = new ServiceController(serviceName);
			try
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
				if (service.Status != ServiceControllerStatus.Stopped)
					service.Stop();
				service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
				Log.Write(serviceName + " service stopped!");
			}
			catch
			{
				Log.Write("Error " + serviceName + " service stopping :( Exit...");
			}
		}

		public class SimpleHttpServer : HttpServer
		{
			public SimpleHttpServer(int port)
				: base(port)
			{
			}
		}
	}
}