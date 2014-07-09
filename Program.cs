using System.IO;
using System.ServiceProcess;
using NETCONLib;
using NetFwTypeLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace BalanceChecker
{
	class Program
	{	
		/// <summary>
		/// Главная точка входа для приложения.
		/// </summary>
		static void Main()
		{
#if (DEBUG)
			new Service();
			return;
#endif
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] 
			{ 
				new Service()
			};
			ServiceBase.Run(ServicesToRun);

		}
	}
}
