using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BalanceChecker
{
	public class Receiver
	{
		SerialPort port;
		public delegate void DAmountReseivedHandler(float balanceAmount);
		public event DAmountReseivedHandler OnReceiveAmount;

		public Receiver(string portName)
		{
			if (string.IsNullOrEmpty(portName))
			{
				Log.Write("[Receiver]", Log.WARNING, "Не визначений COM-порт для модема"); 
				return;
			}
			port = new SerialPort();
			port.PortName = portName;
			port.WriteTimeout = Settings.Default.SerialPortTimeout;
			port.ReadTimeout = Settings.Default.SerialPortTimeout;
			port.DataReceived += port_DataReceived;
		}

		public void Start()
		{
			try
			{
				port.Open();
			}
			catch (Exception ex)
			{
				Log.Write("[Receiver.Start]", Log.ERROR, ex.Message); 
				port.Dispose();
				return;
			}
		}

		void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort spL = (SerialPort)sender;
			byte[] buf = new byte[spL.BytesToRead];
			spL.Read(buf, 0, buf.Length);
			var result = Encoding.ASCII.GetString(buf);
			Regex lineSplitter = new Regex("\\+CUSD: \\d,\"(.*)\",15", RegexOptions.Singleline);
			if (!lineSplitter.IsMatch(result))
			{
				return;
			}

			var f = lineSplitter.Match(result);
			var hexString = f.Groups[1].ToString().Trim();
			result = Encoding.ASCII.GetString(PduBitPacker.UnpackBytes(PduBitPacker.ConvertHexToBytes(hexString)));

			//Log.Write("Receiver.port_DataReceived", Log.INFO, string.Format("Відповідь USSD:\n{0}", result));

			foreach (var amountRegExItem in Settings.Default.AmountRegExList)
			{
				lineSplitter = new Regex(amountRegExItem, RegexOptions.Singleline);
				if (!lineSplitter.IsMatch(result))
				{
					continue;
				}
				f = lineSplitter.Match(result);
				result = f.Groups[1].ToString().Trim();
				float amount;
				try
				{
					amount = float.Parse(result.Replace('.',','));
				}
				catch (Exception ex)
				{
					Log.Write("Receiver.port_DataReceived", Log.ERROR, ex.Message); 
					continue;
				}
				Log.Write("Баланс", Log.INFO, string.Format("{0}", amount));
				if (null != OnReceiveAmount)
				{
					OnReceiveAmount.Invoke(amount);

				}
				spL.Close();
			}			
			
		}
	}
}
