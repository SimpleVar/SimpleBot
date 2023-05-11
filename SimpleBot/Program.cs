using System.Diagnostics;

namespace SimpleBot
{
  internal static class Program
  {
    [STAThread]
    static void Main()
    {
      ApplicationConfiguration.Initialize();
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException += (o, e) => { ChatterDataMgr._save_noLock(); Environment.FailFast(null, e.Exception); };
      Application.ApplicationExit += (o, e) => ChatterDataMgr._save_noLock();
      Application.Run(new MainForm());
    }
  }
}