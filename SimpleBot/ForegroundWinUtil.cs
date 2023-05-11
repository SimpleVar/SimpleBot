using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimpleBot
{
  struct ForegroundWindowData
  {
    public string procName, title;
  }

  internal static class ForegroundWinUtil
  {
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();

    public static LongRunningPeriodicTask _task;

    public static event EventHandler<ForegroundWindowData> ForgroundWindowChanged;

    public static void Init()
    {
      if (_task != null)
        throw new ApplicationException("Init should be called exactly once");

      IntPtr prevHwnd = 0;
      _task = LongRunningPeriodicTask.Start(0, true, 1000, 0, 0, _ =>
      {
        var hwnd = GetForegroundWindow();
        if (hwnd == prevHwnd)
          return;
        GetWindowThreadProcessId(hwnd, out var pid);
        string name, title;
        try
        {
          var p = Process.GetProcessById((int)pid);
          name = p.ProcessName;
          title = p.MainWindowTitle;
        }
        catch
        {
          return;
        }
        prevHwnd = hwnd;
        ForgroundWindowChanged?.Invoke(null, new ForegroundWindowData { procName = name, title = title });
      });
    }
  }
}
