using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;
using Timer = System.Timers.Timer;

namespace BalanceChecker
{
    partial class Service : ServiceBase
    {
        private List<UsbDevice> _modemList;
        private HttpServer _httpServer;
        private Timer _timer;

        public Service()
        {
            InitializeComponent();
            InitializeHttpServer();
            InitializeTimer();

#if (DEBUG)
			GetModemList();
			StartHTTPServer();
			timer.Start();
#endif
        }

        private void InitializeTimer()
        {
            _timer = new Timer
            {
                Interval = Settings.Default.CheckerInterval * 1000 * 60 * 60,
                Enabled = true
            };
            _timer.Elapsed += timer_Elapsed;
        }

        private void GetModemList()
        {
            StopSipGsmService();
            _modemList = Modem.GetList();

            if (0 == _modemList.Count)
            {
                Log.Write("Service.GetModemList", Log.Warning, "Немає доступних модемів. Службу/програму буде завершено");
                Environment.Exit(0);
            }
            Log.Write("Service.GetModemList", Log.Info, $"Знайдено модемів: {_modemList.Count}");
            foreach (var modem in _modemList)
            {
                Log.Write("Service.GetModemList", Log.Info, $"IMEI: {modem.Imei} :: Порт {modem.PortName}");
            }

            var mCount = (_modemList.Where(modem => (modem.ReceivePort == null))).Count();

            if (0 < mCount)
            {
                Log.Write("Service.GetModemList", Log.Warning, $"Не проініціалізовано модемів: {mCount}");
            }
            if (_modemList.Count == mCount)
            {
                Log.Write("Service.GetModemList", Log.Warning, "Немає доступних модемів.");
            }
            StartSipGsmService();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Добавьте код для запуска службы.
            GetModemList();
            StartHttpServer();

            if (!Settings.Default.UseTimer) return;
            Log.Write("Service", Log.Warning, $"Таймер увімкнено з інтервалом: {Settings.Default.CheckerInterval} год.");
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer.Stop();
            _modemList = null;
        }

        private void InitializeHttpServer()
        {
            if (0 < Settings.Default.HTTPPort && 65000 > Settings.Default.HTTPPort)
            {
                _httpServer = new SimpleHttpServer(Settings.Default.HTTPPort);
                Log.Write("Service.InitializeHTTPServer", Log.Warning, $"Порт HTTP-сервера: {Settings.Default.HTTPPort}");
            }
            else
            {
                _httpServer = new SimpleHttpServer(8182);
                Log.Write("Service.InitializeHTTPServer", Log.Warning, $"Порт HTTP-сервера: {"8080"}");
            }
            _httpServer.OnGetRequest += httpServer_OnGETRequest;
            _httpServer.OnPostRequest += httpServer_OnPOSTRequest;
            _httpServer.OnHttpServerStarted += httpServer_OnHTTPServerStarted;
        }

        private void httpServer_OnHTTPServerStarted(TcpListener listener)
        {
            Log.Write("Service.httpServer.OnHTTPServerStarted", Log.Warning, "Додавання правила в Windows Firewall для HTTP-сервера...");
            Firewall.AddRule();
        }

        private void StartHttpServer()
        {
            if (!Settings.Default.UseHTTP) return;
            var thread = new Thread(new ThreadStart(_httpServer.Listen));
            thread.Start();
        }

        private void httpServer_OnPOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            switch (p.HttpUrl)
            {
                case "/settings":
                    {
                        var data = inputData.ReadToEnd();
                        var dataList = GetDataList(data);
                        var command = (from item in dataList where item.Key == "command" select item).SingleOrDefault().Value;

                        p.WriteSuccess();
                        Settings.Default.Save();
                        p.OutputStream.WriteLine("<html><body><h3>Balance Checker settings saved</h3>");
                        p.OutputStream.WriteLine("<a href=/>main</a><p>");
                    }
                    break;

                case "/sipgsmsettings":
                    {
                        var data = inputData.ReadToEnd();
                        var dataList = GetDataList(data);
                        p.OutputStream.WriteLine("<html><body><h3>Balance Checker settings saved</h3>");
                        p.OutputStream.WriteLine("<a href=/>main</a><p>");
                    }
                    break;

                default:
                    break;
            }
        }

        private void httpServer_OnGETRequest(HttpProcessor p)
        {
            switch (p.HttpUrl)
            {
                case "/":
                    {
                        p.OutputStream.WriteLine(@"
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
                        var result = CheckSipGsmServiceStatus();
                        var htmlText = string.Format(Settings.Default.SipGsmStatusHtml, "Stopped" == result ? "#CC3300" : "#006600", result);
                        p.WriteSuccess();
                        p.OutputStream.WriteLine(htmlText);
                    }
                    break;

                case "/checksipgsm":
                    {
                        var result = CheckSipGsmServiceStatus();
                        var htmlText = string.Format(Settings.Default.SipGsmStatusHtml, ("Stopped" == result ? "#CC3300" : "#006600"), result);
                        p.WriteSuccess();
                        p.OutputStream.WriteLine(htmlText);
                    }
                    break;

                case "/check":
                    p.WriteSuccess();
                    p.OutputStream.WriteLine(@"
<html>
<head>" +
"\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/>\n" +
@"<title>Checking Balance</title>
</head>
<body>
<h3>Перевірка балансу...</h3>
");
                    p.OutputStream.WriteLine(@"
</body>
</html>
");
                    StartCheckBalance();
                    break;

                case "/favicon.ico":
                    Stream fs = File.Open("html/favicon.ico", FileMode.Open);
                    p.WriteSuccess("image/x-icon");
                    fs.CopyTo(p.OutputStream.BaseStream);
                    p.OutputStream.BaseStream.Flush();
                    break;

                default:
                    p.WriteSuccess();
                    p.OutputStream.WriteLine("<html><body><h5>Command \"" + p.HttpUrl + "\" not founded</h5>");
                    break;
            }
        }

        private static string CheckSipGsmServiceStatus()
        {
            var serviceName = Settings.Default.SipGsmServiceName;
            var service = new ServiceController(serviceName);
            var timeout = TimeSpan.FromMilliseconds(3000);
            return service.Status.ToString();
        }

        private static void OnOff()
        {
            var serviceName = Settings.Default.SipGsmServiceName;
            var service = new ServiceController(serviceName);
            var timeout = TimeSpan.FromMilliseconds(1000);
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
            }
        }

        private string SipGsmPath
        {
            get
            {
                var p = "";
                if (8 == IntPtr.Size
                    || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
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
            var dataList = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(data))
            {
                return dataList;
            }

            foreach (var s in data.Split('&').Select(item => item.Split('=')))
            {
                dataList.Add(s[0], s[1]);
            }

            return dataList;
        }

        private void timer_Elapsed(object sender, EventArgs e)
        {
            if (DateTime.Now.Date != Settings.Default.LastCheckBalanceDate.Date)
            {
                Log.Write("Service.timer_Elapsed", Log.Warning, "Планова перевірка балансу за таймером");
                StartCheckBalance();
                Settings.Default.LastCheckBalanceDate = DateTime.Now.Date;
                Settings.Default.Save();
            }
            else
            {
                Log.Write("Service.timer_Elapsed", Log.Warning, "Плановий перезапуск служби SipGSMSercice");
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
            Log.Write("Service.StartCheckBalance", Log.Warning, "Перевірка балансу");
            StopSipGsmService();
            foreach (var item in _modemList)
            {
                item.StartProcesses();
            }
            Thread.Sleep(Settings.Default.ServiceStopTime * 1000);
            StartSipGsmService();
        }

        public static void StartSipGsmService()
        {
            Log.Write("Service.StartService", Log.Warning, $"Запуск служби :: {Settings.Default.SipGsmServiceName}");
            var service = new ServiceController(Settings.Default.SipGsmServiceName);
            try
            {
                var timeout = TimeSpan.FromMilliseconds(Settings.Default.WaitForStatusTimeout);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                Log.Write("Service.StartService", Log.Info, $"Службу запущено :: {Settings.Default.SipGsmServiceName}");
            }
            catch (Exception ex)
            {
                Log.Write("Service.StartService", Log.Error, $"{Settings.Default.SipGsmServiceName} :: {ex.Message}");
            }
        }

        public static void StopSipGsmService()
        {
            Log.Write("Service.StopService", Log.Warning, $"Зупинка служби :: {Settings.Default.SipGsmServiceName}");
            var service = new ServiceController(Settings.Default.SipGsmServiceName);
            try
            {
                var timeout = TimeSpan.FromMilliseconds(Settings.Default.WaitForStatusTimeout);
                if (service.Status != ServiceControllerStatus.Stopped)
                    service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                Log.Write("Service.StopService", Log.Info, $"Службу зупинено :: {Settings.Default.SipGsmServiceName}");
            }
            catch (Exception ex)
            {
                Log.Write("Service.StopService", Log.Error, $"{Settings.Default.SipGsmServiceName} :: {ex.Message}");
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