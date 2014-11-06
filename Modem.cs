using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace BalanceChecker
{
	static class Modem
	{
		public static List<UsbDevice> GetList()
		{

			ManagementObjectCollection collection;
			using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_POTSModem "))
				collection = searcher.Get();

			var devices =
				(from ManagementBaseObject device in collection
				 select new UsbDevice(
				 (string)device.GetPropertyValue("DeviceID"),
				 (string)device.GetPropertyValue("Description"),
				 (string)device.GetPropertyValue("AttachedTo"),
				 (string)device.GetPropertyValue("Status"))).ToList();

			collection.Dispose();

			var modemList = (from d in devices where d.DeviceId.Contains("VID_") && d.DeviceId.Contains("PID_") && d.Status == "OK" select d).ToList();

			foreach (var item in modemList)
			{
				item.SetIMEI();
			}
			var excludePortList = (from modem in modemList select modem.PortName).ToList();

			foreach (ManagementObject mo in new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort").Get())
			{
				Regex _lineSplitter = new Regex("COM", RegexOptions.Singleline);
				if (_lineSplitter.IsMatch((string)mo["DeviceId"]))
				{
					excludePortList.Add((string)mo["DeviceId"]);
				}
			}

			var receivePortList = SerialPort.GetPortNames().ToList().Except(excludePortList).ToList();

			foreach (string item in receivePortList)
			{
				var s = GetIMEI(item);
				if (string.IsNullOrEmpty(s))
				{
					continue;
				}
				var modem = (from m in modemList where m.IMEI == s select m).FirstOrDefault();
				if (null != modem)
				{
					modem.ReceivePort = item;
				}
			}
			return modemList;
		}
		public static string GetIMEI(string portName)
		{
			if (string.IsNullOrEmpty(portName)) return null;
			SerialPort port = null;
			string responce = "";
			try
			{
				port = new SerialPort(portName, 9600); //9600 baud is supported by most modems
				port.ReadTimeout = 200;
				port.Open();
				port.Write("ATI\r");
				Thread.Sleep(500);
				responce = port.ReadExisting();
				Regex _lineSplitter = new Regex("IMEI: (\\d{15})", RegexOptions.Singleline);
				if (_lineSplitter.IsMatch(responce))
				{
					string line = port.ReadExisting();
					var f = _lineSplitter.Match(responce);
					responce = f.Groups[1].ToString().Trim();
				}
			}
			catch
			{
				return null;
			}
			finally
			{
				if (port != null)
				{
					port.Close();
				}
			}
			return responce;
		}
	}

	
	public class UsbDevice
	{
		Receiver receiver;
		Sender sender;

		public UsbDevice(string deviceId, string description, string attachedTo, string status)
		{
			DeviceId = deviceId;
			Description = description;
			PortName = attachedTo;
			Status = status;
		}


		#region Properties
		public string DeviceId { get; private set; }
		public string Description { get; private set; }
		public string PortName { get; private set; }
		public string Status { get; private set; }
		public string IMEI { get; private set; }
		public string ReceivePort { get; set; }
		public int VID
		{
			get { return int.Parse(GetIdentifierPart("VID_"), NumberStyles.HexNumber); }
		}
		public int PID
		{
			get { return int.Parse(GetIdentifierPart("PID_"), NumberStyles.HexNumber); }
		}

		#endregion Properties
		
		#region Methods
		public void SetIMEI()
		{
			if (string.IsNullOrEmpty(PortName)) return;			
			IMEI = Modem.GetIMEI(PortName);
		}
		private string GetIdentifierPart(string identifier)
		{
			var vidIndex = DeviceId.IndexOf(identifier, StringComparison.Ordinal);
			var startingAtVid = DeviceId.Substring(vidIndex + 4);
			return startingAtVid.Substring(0, 4);
		}

		public void StartProcesses()
		{
			Thread modemReceiver = new Thread(new ThreadStart(StartReceiver));
			modemReceiver.Start();
			Thread modemSender = new Thread(new ThreadStart(StartSender));
			modemSender.Start();
		}

		private void StartSender()
		{
			sender = new Sender(PortName);
		}

		private void StartReceiver()
		{
			receiver = new Receiver(ReceivePort);
			receiver.OnReceiveAmount += receiver_OnReceiveAmount;
			receiver.Start();
		}

		void receiver_OnReceiveAmount(float balanceAmount)
		{

		}

		#endregion Methods
	}
}
