using System.Runtime.CompilerServices;

namespace SimpleBot.v2
{
    static class Err
    {
        public static event Action OnFatal = delegate { };

        public static void Fatal(string msg, [CallerMemberName] string callerMember = null, [CallerFilePath] string callerFilepath = null)
            => Fatal(new ApplicationException(msg), callerMember, callerFilepath);

        public static void Fatal(Exception ex, [CallerMemberName] string callerMember = null, [CallerFilePath] string callerFilepath = null)
        {
            Log.Err("FATAL " + ex, callerFilepath, callerMember);
            OnFatal();
#if DEBUG
            MessageBox.Show(ex.ToString(), "Fatal Error", MessageBoxButtons.OK);
#endif
            Log.Info("final words");
            Environment.FailFast("Fatal error occured");
        }
    }
}
