using System;
using System.Diagnostics;
using System.IO;

namespace DriveSync.Shared
{
    public static class Logger
    {
        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        private static void Write(string level, string message)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DriveSync");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var path = Path.Combine(folder, "sync.log");
                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + message + Environment.NewLine;
                File.AppendAllText(path, line);

                if (!EventLog.SourceExists("DriveSync"))
                    EventLog.CreateEventSource("DriveSync", "Application");

                EventLog.WriteEntry("DriveSync", line, EventLogEntryType.Information);
            }
            catch
            {
            }
        }
    }
}
