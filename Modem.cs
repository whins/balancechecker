using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace BalanceChecker
{
    internal static class Modem
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
                item.SetImei();
            }
            var excludePortList = (from modem in modemList select modem.PortName).ToList();

            excludePortList.AddRange(from ManagementObject mo in new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort").Get() let lineSplitter = new Regex("COM", RegexOptions.Singleline) where lineSplitter.IsMatch((string)mo["DeviceId"]) select (string)mo["DeviceId"]);

            var receivePortList = SerialPort.GetPortNames().ToList().Except(excludePortList).ToList();

            foreach (var item in receivePortList)
            {
                var s = GetImei(item);
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                var modem = (from m in modemList where m.Imei == s select m).FirstOrDefault();
                if (null != modem)
                {
                    modem.ReceivePort = item;
                }
            }
            return modemList;
        }

        public static string GetImei(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return null;
            SerialPort port = null;
            var responce = "";
            try
            {
                port = new SerialPort(portName, 9600) { ReadTimeout = 200 }; //9600 baud is supported by most modems
                port.Open();
                port.Write("ATI\r");
                Thread.Sleep(500);
                responce = port.ReadExisting();
                var lineSplitter = new Regex("IMEI: (\\d{15})", RegexOptions.Singleline);
                if (lineSplitter.IsMatch(responce))
                {
                    string line = port.ReadExisting();
                    var f = lineSplitter.Match(responce);
                    responce = f.Groups[1].ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Write("Modem.GetIMEI", Log.Error, $"Port :: {portName} :: {ex.Message}");
                return null;
            }
            finally
            {
                port?.Close();
            }
            return responce;
        }
    }

    public class UsbDevice
    {
        private Receiver _receiver;
        private Sender _sender;

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
        public string Imei { get; private set; }
        public string ReceivePort { get; set; }
        public int Vid => int.Parse(GetIdentifierPart("VID_"), NumberStyles.HexNumber);

        public int Pid => int.Parse(GetIdentifierPart("PID_"), NumberStyles.HexNumber);

        #endregion Properties

        #region Methods

        public void SetImei()
        {
            if (string.IsNullOrEmpty(PortName)) return;
            Imei = Modem.GetImei(PortName);
        }

        private string GetIdentifierPart(string identifier)
        {
            var vidIndex = DeviceId.IndexOf(identifier, StringComparison.Ordinal);
            var startingAtVid = DeviceId.Substring(vidIndex + 4);
            return startingAtVid.Substring(0, 4);
        }

        public void StartProcesses()
        {
            var modemReceiver = new Thread(new ThreadStart(StartReceiver));
            modemReceiver.Start();
            var modemSender = new Thread(new ThreadStart(StartSender));
            modemSender.Start();
        }

        private void StartSender()
        {
            _sender = new Sender(PortName);
        }

        private void StartReceiver()
        {
            _receiver = new Receiver(ReceivePort);
            _receiver.OnReceiveAmount += receiver_OnReceiveAmount;
            _receiver.Start();
        }

        private static void receiver_OnReceiveAmount(float balanceAmount)
        {
        }

        #endregion Methods
    }
}