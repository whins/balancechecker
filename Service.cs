using System.Net.Sockets;
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

		void httpServer_OnHTTPServerStarted(TcpListener listener)
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