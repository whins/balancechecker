using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace BalanceChecker
{
	class Sender
	{
		SerialPort port;
		public Sender(string portName)
		{
			port = new SerialPort();
			port.PortName = portName;
			port.WriteTimeout = Settings.Default.SerialPortTimeout;
			port.ReadTimeout = Settings.Default.SerialPortTimeout;

			try
			{
				port.Open();
				foreach (var item in Settings.Default.CheckBalanceNumberList)
				{
					Log.Write("USSD запит :: " + item); 
					SendUSSD(item);
					Thread.Sleep(10000);
				}
				port.Close();
			}
			catch (Exception ex)
			{
				Log.Write("Sender.SendUSSD", Log.ERROR, string.Format("{0}", ex.Message));
				port.Dispose();
				return;
			}
		}

		public void SendUSSD(string code)
		{
			var hexString = PduBitPacker.ConvertBytesToHex(PduBitPacker.PackBytes(Encoding.ASCII.GetBytes(code)));
			port.Write("AT+CUSD=1," + hexString + ",15\r");
		}
	}
}
