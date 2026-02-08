using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automated_Employee_Attendance_System.Services
{
    public static class SystemServices
    {
        private static readonly string LogFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemLogs");

        private static readonly string LogFile =
            Path.Combine(LogFolder, "system_status.txt");

        private static DateTime _lastClearDate = DateTime.MinValue;
        private static readonly object _logLock = new object();

        // ---------------- SAVE STATUS ----------------
        public static void Log(string message)
        {
            try
            {
                if (!Directory.Exists(LogFolder))
                    Directory.CreateDirectory(LogFolder);

                // Only clear old logs once per day instead of every call
                if (_lastClearDate.Date != DateTime.Now.Date)
                {
                    _lastClearDate = DateTime.Now;
                    ClearOldLogs();
                }

                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
                lock (_logLock)
                {
                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // silent fail (system service ekak nisa)
            }
        }

        // ---------------- READ ALL STATUS ----------------
        public static string ReadAll()
        {
            try
            {
                if (!File.Exists(LogFile))
                    return "";

                lock (_logLock)
                {
                    return File.ReadAllText(LogFile);
                }
            }
            catch
            {
                return "";
            }
        }

        // ---------------- ASYNC READ (for UI timer use) ----------------
        public static async Task<string> ReadAllAsync()
        {
            try
            {
                if (!File.Exists(LogFile))
                    return "";

                return await Task.Run(() =>
                {
                    lock (_logLock)
                    {
                        return File.ReadAllText(LogFile);
                    }
                });
            }
            catch
            {
                return "";
            }
        }

        // ---------------- AUTO CLEAR AFTER 7 DAYS ----------------
        private static void ClearOldLogs()
        {
            if (!File.Exists(LogFile))
                return;

            var lines = File.ReadAllLines(LogFile);

            var validLines = lines.Where(l =>
            {
                var parts = l.Split('|');
                if (DateTime.TryParse(parts[0], out DateTime time))
                {
                    return (DateTime.Now - time).TotalDays <= 7;
                }
                return false;
            });

            File.WriteAllLines(LogFile, validLines);
        }
    }
}
