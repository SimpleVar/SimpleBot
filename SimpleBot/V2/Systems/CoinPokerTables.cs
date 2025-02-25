using System.Diagnostics;

namespace SimpleBot.v2
{
    static class CoinPokerTables
    {
        static readonly WindowMonitor Monitor;
        public static bool Enabled
        {
            get => Monitor.Enabled;
            set => Monitor.Enabled = value;
        }
        public static void ClearCache()
        {
            lock (PokerTables)
            {
                PokerTables.Clear();
            }
            Monitor.ClearCache();
        }

        const int W = 1500;
        const int H = 1032;

        static CoinPokerTables()
        {
            Monitor = new WindowMonitor(p =>
            {
                if (p.procName != @"C:\CoinPoker\game.exe")
                    return false;
                if (p.title.StartsWith("checking for updates", StringComparison.InvariantCultureIgnoreCase))
                    return false;
                if (p.title == "CoinPoker" || p.title == "CoinPoker - Lobby")
                {
                    _ = User32.SetWindowPos(p.hwnd, IntPtr.Zero, 0, 0, W, H, 4); // 4 = SWP_NOZORDER
                    return false;
                }
                if (p.title.EndsWith(" Lobby", StringComparison.InvariantCultureIgnoreCase))
                    return false;
                return true;
            })
            {
                Enabled = false
            };
            Monitor.OnCreated += OnPokerWindowCreated;
            Monitor.OnExit += OnPokerWindowExit;
        }

        static readonly ObsItem[] ObsTables = [
            new ObsItem("CoinPokerTable 2", "CODE"),
            new ObsItem("CoinPokerTable 2 2", "CODE"),
            new ObsItem("CoinPokerTable 2 2 2", "CODE"),
            new ObsItem("CoinPokerTable 2 2 2 2", "CODE"),
        ];
        static readonly List<WindowInfo> PokerTables = [];
        static void OnPokerWindowExit(WindowInfo e)
        {
            lock (PokerTables)
            {
                for (int i = 0; i < PokerTables.Count; i++)
                {
                    if (PokerTables[i].hwnd == e.hwnd)
                    {
                        PokerTables.RemoveAt(i);
                    }
                }
            }
            _ = Task.Run(DelayArrange);
        }

        static void OnPokerWindowCreated(WindowInfo e)
        {
            lock (PokerTables)
            {
                PokerTables.Add(e);
            }
            _ = Task.Run(DelayArrange);
        }

        static async Task DelayArrange()
        {
            while (true)
            {
                lock (PokerTables)
                {
                    bool anyLoading = false;
                    for (int i = 0; i < PokerTables.Count; i++)
                    {
                        PokerTables[i].RefreshTitle();
                        if (PokerTables[i].title != "Loading...")
                            continue;
                        anyLoading = true;
                        break;
                    }
                    if (!anyLoading)
                    {
                        arrange_noLock();
                        return;
                    }
                }
                await Task.Delay(1000);
            }
        }

        static void arrange_noLock()
        {
            for (int i = 0; i < ObsTables.Length && i < PokerTables.Count; i++)
            {
                string source;
                try { source = PokerTables[i].title + ":Qt673QWindowIcon:game.exe"; }
                catch { source = ""; }
                ObsTables[i].SetWindowSource(source);
            }
            for (int i = PokerTables.Count; i < ObsTables.Length; i++)
            {
                ObsTables[i].SetWindowSource("");
            }
            switch (PokerTables.Count)
            {
                case 0: return;
                case 1:
                    _ = User32.SetWindowPos(PokerTables[0].hwnd, IntPtr.Zero, 0, 0, W - 59, H, 4); // 4 = SWP_NOZORDER
                    break;
                case 2:
                    const int SMALL_W = 784;
                    const int SMALL_H = 561;
                    _ = User32.SetWindowPos(PokerTables[0].hwnd, IntPtr.Zero, 0, 0, SMALL_W, SMALL_H, 4);
                    _ = User32.SetWindowPos(PokerTables[1].hwnd, IntPtr.Zero, W - SMALL_W, H - SMALL_H, SMALL_W, SMALL_H, 4);
                    break;
                default:
                    // click the built-in "Tile" button on a poker table
                    _ = User32.SetWindowPos(PokerTables[0].hwnd, IntPtr.Zero, 0, 0, W / 2, H / 2, 0);
                    _ = User32.SendInput(2, [
                        // normalized coordinates (upto ushort.MaxValue)
                        new(21114, 1213, 0, User32.MOUSEEVENTF.ABSOLUTE | User32.MOUSEEVENTF.VIRTUALDESK | User32.MOUSEEVENTF.MOVE | User32.MOUSEEVENTF.LEFTDOWN),
                        new(0, 0, 0, User32.MOUSEEVENTF.LEFTUP),
                    ], User32.INPUT_mouse.Size);
                    break;
            }
        }
    }
}
