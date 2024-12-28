using System.Runtime.CompilerServices;

namespace SimpleBot.v2
{
    static class Log
    {
        static string _logFilePath;

        static Log()
        {
#if DEBUG
            _logFilePath = Application.StartupPath + "logs\\";
#else
            _logFilePath = Application.StartupPath + "logs_dbg\\";
#endif
            Directory.CreateDirectory(_logFilePath);
            _logFilePath += $"{DateTime.Now:s}.txt";
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Debug(string msg, [CallerMemberName] string callerMember = null, [CallerFilePath] string callerFilepath = null) => System.Diagnostics.Debug.WriteLine(_log(msg, "DBG", callerMember, callerFilepath));

        public static void Info(string msg, [CallerMemberName] string callerMember = null, [CallerFilePath] string callerFilepath = null) => _log(msg, "INFO", callerMember, callerFilepath);

        public static void Err(string msg, [CallerMemberName] string callerMember = null, [CallerFilePath] string callerFilepath = null) => _log(msg, "ERR", callerMember, callerFilepath);

        static string _log(string msg, string logLevel, string callerMember, string callerFilepath)
        {
            string formatted = "<>";
            try
            {
                formatted = $"{DateTime.Now:s} [{Path.GetFileName(callerFilepath) ?? ""} :: {callerMember}] {logLevel} {msg}\n";
                File.AppendAllText(_logFilePath, formatted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Break();
            }
            return formatted;
        }
    }
}
