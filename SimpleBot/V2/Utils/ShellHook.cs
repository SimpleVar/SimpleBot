using System.Diagnostics;
using System.Text;

namespace SimpleBot.v2
{
    public class WindowInfo(nint hwnd, string title, string procName)
    {
        public readonly nint hwnd = hwnd;
        public readonly string procName = procName;
        public string title = title;

        public void RefreshTitle()
        {
            title = null;
            try
            {
                var sb = new StringBuilder(512);
                _ = User32.GetWindowText(hwnd, sb, (int)sb.Capacity);
                title = sb.ToString();
            }
            catch { }
        }
    }

    public class ShellHook : NativeWindow
    {
        readonly uint WM_ShellHook;
        public ShellHook()
        {
            CreateHandle(new CreateParams());
            if (!User32.RegisterShellHookWindow(Handle))
                Err.Fatal("Failed to register ShellHook window");
            WM_ShellHook = User32.RegisterWindowMessage("SHELLHOOK");
        }
        public void Deregister()
        {
            User32.RegisterShellHook(Handle, 0);
        }

        public event Action<WindowInfo> WindowCreated = delegate { };
        public event Action<WindowInfo> WindowFocused = delegate { };
        public event Action<nint> WindowExited = delegate { };

        public enum ShellEvents
        {
            HSHELL_WINDOWCREATED = 1,
            HSHELL_WINDOWDESTROYED = 2,
            HSHELL_WINDOWACTIVATED = 4,
            HSHELL_HIGHBIT = 0x8000,
            HSHELL_RUDEAPPACTIVATED = (HSHELL_WINDOWACTIVATED | HSHELL_HIGHBIT)
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ShellHook)
            {
                var hwnd = m.LParam;
                WindowInfo p;
                switch ((ShellEvents)m.WParam)
                {
                    case ShellEvents.HSHELL_WINDOWCREATED:
                        if (User32.IsWindowVisible(hwnd) && (p = ProcFromHwnd(hwnd)) != null)
                            _ = Task.Run(() => WindowCreated(p)).LogErr();
                        break;
                    case ShellEvents.HSHELL_WINDOWDESTROYED:
                        _ = Task.Run(() => WindowExited(hwnd)).LogErr();
                        break;
                    case ShellEvents.HSHELL_RUDEAPPACTIVATED:
                    case ShellEvents.HSHELL_WINDOWACTIVATED:
                        if ((p = ProcFromHwnd(hwnd)) != null)
                            _ = Task.Run(() => WindowFocused(p)).LogErr();
                        break;
                }
            }
            base.WndProc(ref m);
        }

        static WindowInfo ProcFromHwnd(nint hwnd)
        {
            try
            {
                _ = User32.GetWindowThreadProcessId(hwnd, out var procId);
                var proc = Process.GetProcessById((int)procId);
                string procName = proc.MainModule.FileName;
                if (string.IsNullOrEmpty(procName))
                    return null;
                var w = new WindowInfo(hwnd, "", procName);
                w.RefreshTitle();
                if (string.IsNullOrEmpty(w.title))
                    return null;
                return w;
            }
            catch
            {
                return null;
            }
        }
    }
}