using HBS.Logging;
using System;
using System.IO;

namespace LootMagnet
{
    public class DeferringLogger
    {
        private readonly string LogFile;

        private readonly StreamWriter LogStream;
        public readonly string LogLabel;

        public readonly bool IsDebug;
        public readonly bool IsTrace;

        readonly ModLogWriter ModOnlyWriter;
        readonly ModLogWriter CombinedWriter;

        public DeferringLogger(string modDir, string logFilename = "mod", bool isDebug = false, bool isTrace = false) : this(modDir, logFilename, "mod", isDebug, isTrace)
        {
        }

        public DeferringLogger(string modDir, string logFilename = "mod", string logLabel = "mod", bool isDebug = false, bool isTrace = false)
        {
            if (LogFile == null)
            {
                LogFile = Path.Combine(modDir, $"{logFilename}.log");
            }

            if (File.Exists(LogFile))
            {
                File.Delete(LogFile);
            }

            LogStream = File.AppendText(LogFile);
            LogLabel = "<" + logLabel + ">";

            IsDebug = isDebug;
            IsTrace = isTrace;

            ModOnlyWriter = new ModLogWriter(LogStream, null);
            CombinedWriter = new ModLogWriter(LogStream, LogLabel);
        }

        public Nullable<ModLogWriter> Trace
        {
            get { return IsTrace ? (Nullable<ModLogWriter>)ModOnlyWriter : null; }
            private set { }
        }

        public Nullable<ModLogWriter> Debug
        {
            get { return IsDebug ? (Nullable<ModLogWriter>)ModOnlyWriter : null; }
            private set { }
        }

        public Nullable<ModLogWriter> Info
        {
            get { return (Nullable<ModLogWriter>)ModOnlyWriter; }
            private set { }
        }

        public Nullable<ModLogWriter> Warn
        {
            get
            {
                if (CombinedWriter.HBSLogger.IsWarningEnabled) return CombinedWriter;
                else return (Nullable<ModLogWriter>)ModOnlyWriter;
            }
            private set { }
        }

        public Nullable<ModLogWriter> Error
        {
            get
            {
                if (CombinedWriter.HBSLogger.IsErrorEnabled) return CombinedWriter;
                else return (Nullable<ModLogWriter>)ModOnlyWriter;
            }
            private set { }
        }
    }

    public struct ModLogWriter
    {
        readonly StreamWriter LogStream;
        public readonly ILog HBSLogger;
        public readonly string Label;
        LogLevel Level;

        public ModLogWriter(StreamWriter sw, string label)
        {
            LogStream = sw;
            if (label != null)
            {
                HBSLogger = HBS.Logging.Logger.GetLogger(label);
                Label = label;
            }
            else
            {
                HBSLogger = null;
                Label = null;
            }
            Level = LogLevel.Warning;
        }

        public void Write(string message)
        {
            // Write our internal log
            string now = DateTime.UtcNow.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine(now + " " + message);
            LogStream.Flush();

            if (HBSLogger != null)
            {
                // Write the HBS logging
                HBSLogger.LogAtLevel(Level, now + " " + Label + " " + message);
            }
        }

        public void Write(Exception e, string message = null)
        {
            // Write our internal log
            string now = DateTime.UtcNow.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            if (message != null) LogStream.WriteLine(now + " " + message);
            LogStream.WriteLine(now + " " + e?.Message);
            LogStream.WriteLine(now + " " + e?.StackTrace);
            LogStream.Flush();

            if (HBSLogger != null)
            {
                // Write the HBS logging
                if (message != null) LogStream.WriteLine(now + message);
                HBSLogger.LogAtLevel(Level, now + " " + Label + " " + e?.Message);
                HBSLogger.LogAtLevel(Level, now + " " + Label + "Stacktrace available in mod logs");
            }
        }
    }
}
