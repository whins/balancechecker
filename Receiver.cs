using System;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace BalanceChecker
{
    public class Receiver
    {
        private readonly SerialPort _port;

        public delegate void DAmountReseivedHandler(float balanceAmount);

        public event DAmountReseivedHandler OnReceiveAmount;

        public Receiver(string portName)
        {
            if (string.IsNullOrEmpty(portName))
            {
                Log.Write("[Receiver]", Log.Warning, "Не визначений COM-порт для модема");
                return;
            }
            _port = new SerialPort
            {
                PortName = portName,
                WriteTimeout = Settings.Default.SerialPortTimeout,
                ReadTimeout = Settings.Default.SerialPortTimeout
            };
            _port.DataReceived += port_DataReceived;
        }

        public void Start()
        {
            try
            {
                _port.Open();
            }
            catch (Exception ex)
            {
                Log.Write("[Receiver.Start]", Log.Error, ex.Message);
                _port.Dispose();
                return;
            }
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var spL = (SerialPort)sender;
            var buf = new byte[spL.BytesToRead];
            spL.Read(buf, 0, buf.Length);
            var result = Encoding.ASCII.GetString(buf);
            var lineSplitter = new Regex("\\+CUSD: \\d,\"(.*)\",15", RegexOptions.Singleline);
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
                    amount = float.Parse(result.Replace('.', ','));
                }
                catch (Exception ex)
                {
                    Log.Write("Receiver.port_DataReceived", Log.Error, ex.Message);
                    continue;
                }
                Log.Write("Баланс", Log.Info, $"{amount}");
                OnReceiveAmount?.Invoke(amount);
                spL.Close();
            }
        }
    }
}