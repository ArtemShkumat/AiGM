using System;
using System.IO;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services
{
    public class LoggingService
    {
        private readonly string _logPath;
        private readonly object _lockObj = new object();

        public LoggingService()
        {
            var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            
            _logPath = Path.Combine(logsDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        private void Log(string level, string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            
            lock (_lockObj)
            {
                try
                {
                    File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                    
                    // Also write to console in development
                    Console.WriteLine(logMessage);
                }
                catch (Exception ex)
                {
                    // Fallback to console if file logging fails
                    Console.WriteLine($"Logging error: {ex.Message}");
                    Console.WriteLine(logMessage);
                }
            }
        }
    }
}
