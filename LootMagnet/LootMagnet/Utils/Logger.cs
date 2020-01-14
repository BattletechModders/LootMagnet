using System;
using System.IO;
using static LootMagnet.LootMagnet;

namespace LootMagnet {
    public class Logger {

        private static StreamWriter LogStream;
        private static string LogFile;

        public Logger(string modDir, string logName) {
            if (LogFile == null) {
                LogFile = Path.Combine(modDir, $"{logName}.log");
            }
            if (File.Exists(LogFile)) {
                File.Delete(LogFile);
            }

            LogStream = File.AppendText(LogFile);
        }

        public Logger() { }

        public void Debug(string message) { if (Mod.Config.Debug) { Info(message); } }

        public void Info(string message) {
            if (Logger.LogStream == null) { return;  }

            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine($"{now} - {message}");
            LogStream.Flush();
        }

    }
}
