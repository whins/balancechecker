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
		private static void Main()
		{
#if (DEBUG)
			new Service();
			return;
#endif
		    var servicesToRun = new ServiceBase[] 
		    { 
		        new Service()
		    };
		    ServiceBase.Run(servicesToRun);
		}
	}
}
