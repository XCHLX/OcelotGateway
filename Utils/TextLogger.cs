using System;
using System.IO;
using System.Threading.Tasks;

namespace OcelotGateway.Utils
{
    public static class TextLogger
    {
        private static readonly object _lock = new(); // 保证多线程安全
        private static string _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

        /// <summary>
        /// 写日志，支持自定义文件名。如果 fileName 为 null，则按日期生成默认文件。
        /// </summary>
        public static async Task LogAsync(string message, string? fileName = null)
        {
            try
            {
                // 确保目录存在
                Directory.CreateDirectory(_logDirectory);

                // 使用自定义文件名或默认按日期生成
                string filePath = Path.Combine(_logDirectory,
                    string.IsNullOrWhiteSpace(fileName)
                    ? $"health_alerts_{DateTime.Now:yyyy-MM-dd}.log"
                    : $"{fileName}_{DateTime.Now:yyyy-MM-dd}.log");

                string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";

                // 异步写入文件，保证线程安全
                lock (_lock)
                {
                    File.AppendAllText(filePath, logLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("写日志失败: " + ex.Message);
            }
        }
    }
}