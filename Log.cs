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
		public static void Write(string text)
		{
			if (Settings.Default.EnableLogging && !string.IsNullOrEmpty(text))
			{
				using (StreamWriter w = File.AppendText(Settings.Default.LogFilePath))
				{
					w.WriteLine("{0} :: {1} ", DateTime.Now.ToString(), text);
				}
			}			
		}
	}
}
