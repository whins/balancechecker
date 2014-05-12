using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace BalanceChecker
{
	class Program
	{
		static void Main()
		{
			StopService(Settings.Default.SipGsmServiceName, 1000);
			var modemList = Modem.GetList();
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
				if(service.Status != ServiceControllerStatus.Stopped)
					service.Stop();
				service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
				Log.Write(serviceName + " service stopped!");
			}
			catch
			{
				Log.Write("Error " + serviceName + " service stopping :( Exit...");
				Environment.Exit(0);
			}
		}

	}
}
