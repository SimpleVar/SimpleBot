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

    static Thread _thread;

    public static event EventHandler<ForegroundWindowData> ForgroundWindowChanged;

    public static void Init()
    {
      if (_thread != null)
        throw new ApplicationException("Init should be called exactly once");

      _thread = new Thread(() =>
      {
        IntPtr prevHwnd = 0;
        while (true)
        {
          Thread.Sleep(1000);
          var hwnd = GetForegroundWindow();
          if (hwnd == prevHwnd)
            continue;
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
            continue;
          }
          prevHwnd = hwnd;
          try
          {
            ForgroundWindowChanged?.Invoke(null, new ForegroundWindowData { procName = name, title = title });
          }
          catch (Exception ex)
          {
            Task.FromException(ex).ThrowMainThread();
          }
        }
      })
      { IsBackground = true };
      _thread.Start();
    }
  }
}
