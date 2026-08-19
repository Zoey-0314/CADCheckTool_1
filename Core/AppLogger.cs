using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Correct_test1.Core
{
    public static class AppLogger
    {
        private static readonly object syncRoot = new object();

        private static string LogDirectory
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Correct_test1",
                    "Logs");
                try
                {
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }
                catch
                {
                    // 写入阶段统一处理目录创建失败。
                }
                return dir;
            }
        }

        private static string GetLogFilePath()
        {
            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            return Path.Combine(LogDirectory, fileName);
        }

        private static void Write(string level, string message, Exception ex, string module, string file)
        {
            try
            {
                var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                int threadId = Thread.CurrentThread.ManagedThreadId;
                var sb = new StringBuilder();
                sb.Append(ts);
                sb.Append(" [");
                sb.Append(level);
                sb.Append("] [TID:");
                sb.Append(threadId);
                sb.Append("]");

                if (!string.IsNullOrEmpty(module))
                {
                    sb.Append(" [Module:");
                    sb.Append(module);
                    sb.Append("]");
                }

                if (!string.IsNullOrEmpty(file))
                {
                    sb.Append(" [File:");
                    sb.Append(file);
                    sb.Append("]");
                }

                sb.Append(" ");
                sb.Append(message ?? "");
                sb.AppendLine();

                if (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                }

                string path = GetLogFilePath();

                lock (syncRoot)
                {
                    File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // 日志失败不能中断 AutoCAD 主流程。
                try
                {
                    string fallback = Path.Combine(Path.GetTempPath(), "Correct_test1_fallback.log");
                    lock (syncRoot)
                    {
                        File.AppendAllText(fallback, DateTime.Now.ToString("o") + " [LOGFAILED] " + message + Environment.NewLine);
                    }
                }
                catch
                {
                }
            }
        }

        public static void Info(string message, string module = null, string file = null)
        {
            Write("INFO", message, null, module, file);
        }

        public static void Debug(string message, string module = null, string file = null)
        {
            Write("DEBUG", message, null, module, file);
        }

        public static void Warn(string message, string module = null, string file = null)
        {
            Write("WARN", message, null, module, file);
        }

        public static void Error(string message, string module = null, string file = null)
        {
            Write("ERROR", message, null, module, file);
        }

        public static void Error(Exception ex, string module = null, string file = null, string message = null)
        {
            Write("ERROR", message ?? ex?.Message, ex, module, file);
        }

        public static void Exception(Exception ex, string module)
        {
            Write("EXCEPTION", ex?.Message, ex, module, null);
        }
    }
}
