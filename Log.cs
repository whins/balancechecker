using System;
using System.IO;

namespace BalanceChecker
{
    public static class Log
    {
        public static readonly string Error = "ERROR";
        public static readonly string Warning = "WARNING";
        public static readonly string Info = "INFO";

        public static void Write(string text)
        {
            if (Settings.Default.EnableLogging && !string.IsNullOrEmpty(text))
            {
                try
                {
                    using (var w = File.AppendText(Settings.Default.LogFilePath))
                    {
                        w.WriteLine($"{DateTime.Now} :: {text}");
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        public static void Write(string source, string logType, string text)
        {
            if (Settings.Default.EnableLogging && !string.IsNullOrEmpty(text))
            {
                try
                {
                    using (var w = File.AppendText(Settings.Default.LogFilePath))
                    {
                        w.WriteLine($"{DateTime.Now} :: [{logType}][{source}] {text}");
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}