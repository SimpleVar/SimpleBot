namespace SimpleBot.v2
{
    class WindowMonitor
    {
        static readonly ShellHook _hook = new();

        readonly Predicate<WindowInfo> _condition;
        readonly Dictionary<nint, WindowInfo> _hwnds = [];

        public event Action<WindowInfo> OnCreated = delegate { };
        public event Action<WindowInfo> OnFocus = delegate { };
        public event Action<WindowInfo> OnExit = delegate { };

        public bool Enabled = true;

        public void ClearCache() => _hwnds.Clear();

        public WindowMonitor(Predicate<WindowInfo> condition)
        {
            ArgumentNullException.ThrowIfNull(condition, nameof(condition));
            _condition = condition;
            _hook.WindowCreated += _hook_WindowCreated;
            _hook.WindowFocused += _hook_WindowFocused;
            _hook.WindowExited += _hook_WindowExited;
        }

        private void _hook_WindowExited(nint hwnd)
        {
            if (!Enabled)
                return;
            WindowInfo w = null;
            bool fire = false;
            lock (_hwnds)
            {
                if (_hwnds.Remove(hwnd, out w))
                    fire = true;
            }
            if (fire)
                OnExit(w);
        }

        private void _hook_WindowFocused(WindowInfo w)
        {
            if (!Enabled)
                return;
            _hook_WindowCreated(w);
            bool fire = false;
            lock (_hwnds)
            {
                fire = _hwnds.ContainsKey(w.hwnd);
            }
            if (fire)
                OnFocus(w);
        }

        private void _hook_WindowCreated(WindowInfo w)
        {
            if (!Enabled)
                return;
            try
            {
                if (!_condition(w))
                    return;
            }
            catch
            {
                return;
            }
            bool fire = false;
            lock (_hwnds)
            {
                if (_hwnds.TryAdd(w.hwnd, w))
                    fire = true;
            }
            if (fire)
                OnCreated(w);
        }
    }
}