using HBS.Logging;
using System;
using System.IO;

namespace LootMagnet {
    public class Logger {

        private static StreamWriter LogStream;
        private static string LogFile;
        private readonly ILog HBSLogger;

        public Logger(string modDir, string logName) {
            if (LogFile == null) {
                LogFile = Path.Combine(modDir, $"{logName}.log");
            }
            if (File.Exists(LogFile)) {
                File.Delete(LogFile);
            }

            LogStream = File.AppendText(LogFile);

            HBSLogger = HBS.Logging.Logger.GetLogger(Mod.HarmonyPackage);
        }

        public Logger() { }

        public void Debug(string message)
        {
            if (Mod.Config.Debug)
            {
                Log(message);
            }
        }

        public void Info(string message)
        {
            Log(message);
        }

        public void Warn(string message)
        {
            Log("WARNING:" + message);
            HBSLogger.LogAtLevel(LogLevel.Warning, "<LOOTMAGNET>" + message);
        }

        public void Error(string message, Exception e)
        {
            Log("ERROR:" + message);
            Log("ERROR:" + e.Message);

            HBSLogger.LogAtLevel(LogLevel.Error, "<LOOTMAGNET>" + message, e);
        }

        private void Log(string message)
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine($"{now} - {message}");
            LogStream.Flush();
        }

    }
}
