using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace BalanceChecker
{
    internal class Sender
    {
        private readonly SerialPort _port;

        public Sender(string portName)
        {
            _port = new SerialPort
            {
                PortName = portName,
                WriteTimeout = Settings.Default.SerialPortTimeout,
                ReadTimeout = Settings.Default.SerialPortTimeout
            };

            try
            {
                _port.Open();
                foreach (var item in Settings.Default.CheckBalanceNumberList)
                {
                    Log.Write("USSD запит :: " + item);
                    SendUssd(item);
                    Thread.Sleep(10000);
                }
                _port.Close();
            }
            catch (Exception ex)
            {
                Log.Write("Sender.SendUSSD", Log.Error, $"{ex.Message}");
                _port.Dispose();
            }
        }

        public void SendUssd(string code)
        {
            var hexString = PduBitPacker.ConvertBytesToHex(PduBitPacker.PackBytes(Encoding.ASCII.GetBytes(code)));
            _port.Write("AT+CUSD=1," + hexString + ",15\r");
        }
    }
}