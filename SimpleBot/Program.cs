namespace SimpleBot
{
  internal static class Program
  {
    [STAThread]
    static void Main()
    {
      ApplicationConfiguration.Initialize();
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException += (o, e) => { ChatterDataMgr._save_noLock(); Bot.Log("[final words] thread exception"); Environment.FailFast(null, e.Exception); };
      Application.ApplicationExit += (o, e) => { ChatterDataMgr._save_noLock(); Bot.Log("[final words] application exit"); };
      Application.Run(new MainForm());
    }
  }
}