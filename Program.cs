using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;


namespace BalanceChecker
{
	class Program
	{
		static void Main()
		{
			var modemList = Modem.GetList();
			foreach (UsbDevice item in modemList)
			{
				item.StartProcesses();
			}
		}





		public static void StartService(string serviceName, int timeoutMilliseconds)
		{
			ServiceController service = new ServiceController(serviceName);
			try
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

				service.Start();
				service.WaitForStatus(ServiceControllerStatus.Running, timeout);
			}
			catch
			{
				// ...
			}
		}
		
		public static void StopService(string serviceName, int timeoutMilliseconds)
		{
			ServiceController service = new ServiceController(serviceName);
			try
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

				service.Stop();
				service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
			}
			catch
			{
				// ...
			}
		}

	}
}
