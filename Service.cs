using System.Net.Sockets;
using HTTP.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using NETCONLib;
using NetFwTypeLib;
using Npgsql;
using Timer = System.Timers.Timer;

namespace BalanceChecker
{
	partial class Service : ServiceBase
	{
		private List<UsbDevice> modemList;
		private HttpServer httpServer;
		private Timer timer;
		public Service()
		{
			InitializeComponent();
			InitializeHTTPServer();
			InitializeTimer();
			
#if (DEBUG)
			GetModemList();
			StartHTTPServer();
			timer.Start();
#endif
			
		}

		private void InitializeTimer()
		{
			timer = new Timer();
			timer.Interval = Settings.Default.CheckerInterval * 1000 * 60 * 60;
			timer.Enabled = true;
			timer.Elapsed += timer_Elapsed;
		}

		private void GetModemList()
		{
			StopSipGsmService();
			modemList = Modem.GetList();
			
			if (0 == modemList.Count)
			{
				Log.Write("Service.GetModemList", Log.WARNING, string.Format("Немає доступних модемів. Службу/програму буде завершено"));
				Environment.Exit(0);
			}
			Log.Write("Service.GetModemList", Log.INFO, string.Format("Знайдено модемів: {0}", modemList.Count));
			foreach (var modem in modemList)
			{
				Log.Write("Service.GetModemList", Log.INFO, string.Format("IMEI: {0} :: Порт {1}", modem.IMEI, modem.PortName));
			}

			int mCount = (modemList.Where(modem => (modem.ReceivePort == null))).Count();

			if (0 < mCount)
			{
				Log.Write("Service.GetModemList", Log.WARNING, string.Format("Не проініціалізовано модемів: {0}", mCount));
			}
			if (modemList.Count == mCount)
			{
				Log.Write("Service.GetModemList", Log.WARNING, string.Format("Немає доступних модемів. Службу/програму буде завершено"));
				Environment.Exit(0);
			}
			StartSipGsmService();
		}

		protected override void OnStart(string[] args)
		{
			// TODO: Добавьте код для запуска службы.
			GetModemList();
			StartHTTPServer();

			if (Settings.Default.UseTimer)
			{
				Log.Write("Service", Log.WARNING, string.Format("Таймер увімкнено з інтервалом: {0} год.", Settings.Default.CheckerInterval));
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
				Log.Write("Service.InitializeHTTPServer", Log.WARNING, string.Format("Порт HTTP-сервера: {0}", Settings.Default.HTTPPort));
			}
			else
			{
				httpServer = new SimpleHttpServer(8182);
				Log.Write("Service.InitializeHTTPServer", Log.WARNING, string.Format("Порт HTTP-сервера: {0}", "8080"));
			}
			httpServer.OnGETRequest += httpServer_OnGETRequest;
			httpServer.OnPOSTRequest += httpServer_OnPOSTRequest;
			httpServer.OnHTTPServerStarted += httpServer_OnHTTPServerStarted;
		}

		void httpServer_OnHTTPServerStarted(TcpListener listener)
		{
			Log.Write("Service.httpServer.OnHTTPServerStarted", Log.WARNING, string.Format("Додавання правила в Windows Firewall для HTTP-сервера..."));
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
<h3>Перевірка балансу...</h3>
");
					p.outputStream.WriteLine(@"
</body>
</html>
");
					StartCheckBalance();
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

		private void timer_Elapsed(object sender, EventArgs e)
		{
			if (DateTime.Now.Date != Settings.Default.LastCheckBalanceDate.Date)
			{
				Log.Write("Service.timer_Elapsed", Log.WARNING, string.Format("Планова перевірка балансу за таймером"));
				StartCheckBalance();
				Settings.Default.LastCheckBalanceDate = DateTime.Now.Date;
				Settings.Default.Save();
			}
			else
			{
				Log.Write("Service.timer_Elapsed", Log.WARNING, string.Format("Плановий перезапуск служби SipGSMSercice"));
				RestartSipGsmService();
			}
		}

		private void RestartSipGsmService()
		{
			StopSipGsmService();
			Thread.Sleep(Settings.Default.ServiceStopTime * 1000);
			StartSipGsmService();
		}

		private void StartCheckBalance()
		{
			Log.Write("Service.StartCheckBalance", Log.WARNING, string.Format("Перевірка балансу"));
			StopSipGsmService();
			foreach (UsbDevice item in modemList)
			{
				item.StartProcesses();
			}
			Thread.Sleep(Settings.Default.ServiceStopTime * 1000);
			StartSipGsmService();
		}

		public static void StartSipGsmService()
		{
			Log.Write("Service.StartService", Log.WARNING, string.Format("Запуск служби :: {0}", Settings.Default.SipGsmServiceName));
			ServiceController service = new ServiceController(Settings.Default.SipGsmServiceName);
			try
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(Settings.Default.WaitForStatusTimeout);

				service.Start();
				service.WaitForStatus(ServiceControllerStatus.Running, timeout);
				Log.Write("Service.StartService", Log.INFO, string.Format("Службу запущено :: {0}", Settings.Default.SipGsmServiceName)); 
			}
			catch (Exception ex)
			{
				Log.Write("Service.StartService", Log.ERROR, string.Format("{0} :: {1}", Settings.Default.SipGsmServiceName, ex.Message)); 
			}
		}

		public static void StopSipGsmService()
		{
			Log.Write("Service.StopService", Log.WARNING, string.Format("Зупинка служби :: {0}", Settings.Default.SipGsmServiceName)); 
			ServiceController service = new ServiceController(Settings.Default.SipGsmServiceName);
			try
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(Settings.Default.WaitForStatusTimeout);
				if (service.Status != ServiceControllerStatus.Stopped)
					service.Stop();
				service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
				Log.Write("Service.StopService", Log.INFO, string.Format("Службу зупинено :: {0}", Settings.Default.SipGsmServiceName)); 
			}
			catch (Exception ex)
			{
				Log.Write("Service.StopService", Log.ERROR, string.Format("{0} :: {1}", Settings.Default.SipGsmServiceName, ex.Message)); 
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