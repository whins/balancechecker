using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BalanceChecker
{
	class Receiver
	{
		SerialPort port;
		public delegate void DAmountReseivedHandler(float amount);
		public event DAmountReseivedHandler OnReceiveAmount;

		public Receiver(string portName)
		{
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
			catch (Exception)
			{
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
			Regex _lineSplitter = new Regex("\\+CUSD: \\d,\"(.*)\",15", RegexOptions.Singleline);
			if (!_lineSplitter.IsMatch(result))
			{
				return;
			}

			var f = _lineSplitter.Match(result);
			var hexString = f.Groups[1].ToString().Trim();
			result = Encoding.ASCII.GetString(PduBitPacker.UnpackBytes(PduBitPacker.ConvertHexToBytes(hexString)));
			foreach (var amountRegExItem in Settings.Default.AmountRegExList)
			{
				_lineSplitter = new Regex(amountRegExItem, RegexOptions.Singleline);
				if (!_lineSplitter.IsMatch(result))
				{
					continue;
				}
				f = _lineSplitter.Match(result);
				result = f.Groups[1].ToString().Trim();
				var amount = float.Parse(result.Replace('.',','));
				if (null != OnReceiveAmount)
				{
					OnReceiveAmount.Invoke(amount);
				}
				spL.Close();
			}					
		}
	}
}
