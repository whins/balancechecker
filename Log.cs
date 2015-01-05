using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanceChecker
{
	public static class Log
	{

		public static readonly string ERROR = "ПОМИЛКА";
		public static readonly string WARNING = "УВАГА";
		public static readonly string INFO = "ІНФО";

		public static void Write(string text)
		{
			if (Settings.Default.EnableLogging && !string.IsNullOrEmpty(text))
			{
				try
				{
					using (StreamWriter w = File.AppendText(Settings.Default.LogFilePath))
					{
						w.WriteLine("{0} :: {1} ", DateTime.Now.ToString(), text);
					}
				}
				catch
				{
				}				
			}			
		}

		public static void Write(string source, string logType, string text)
		{
			if (Settings.Default.EnableLogging && !string.IsNullOrEmpty(text))
			{
				try
				{
					using (StreamWriter w = File.AppendText(Settings.Default.LogFilePath))
					{
						w.WriteLine("{0} :: [{1}][{2}] {3}", DateTime.Now.ToString(), logType, source,  text);
					}
				}
				catch
				{
				}
			}
		}

	}

}
