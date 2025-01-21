using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using SimpleBot.Properties;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Forms;
using CharSet = System.Runtime.InteropServices.CharSet;
using DllImportAttribute = System.Runtime.InteropServices.DllImportAttribute;

// TODO
// 2. liked songs

namespace SimpleBot
{
    public partial class MainForm : Form
    {
        public static MainForm Get { get; private set; }
        static IntPtr HotkeyRegisteredHandle = IntPtr.Zero;

        Bot bot = null;
        bool _freezeChattersList = false;
        bool _isMultiselectingChatters = false;
        bool _loadedPosAndSize = false;

        public MainForm()
        {
            Get = this;
            InitializeComponent();
            this.Icon = Resources.s_logo;
            this.ResizeRedraw = true;

#if DEBUG
      Text += " (debug)";
#endif
        }

        public void DebugMsg(string msg)
        {
            Invoke(() =>
            {
                txtDbg.Text = msg + "\r\n" + txtDbg.Text;
            });
        }

        protected override void OnMove(EventArgs e)
        {
#if !DEBUG
            if (_loadedPosAndSize)
            {
                Settings.Default.LastMainFormPosition = this.Location;
                Settings.Default.Save();
            }
#endif
            base.OnMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
#if !DEBUG
            if (_loadedPosAndSize)
            {
                Settings.Default.LastMainFormSize = this.Size;
                Settings.Default.Save();
            }
#endif
            base.OnResize(e);
        }

        Point _initialLocation;
        Size _initialSize;
        private async void MainForm_Load(object sender, EventArgs e)
        {
            Point lastPos = Settings.Default.LastMainFormPosition;
            if (lastPos.X < -10 || lastPos.Y < -10)
                lastPos = Point.Empty;
            Location = lastPos;
            var lastSize = Settings.Default.LastMainFormSize;
            if (!lastSize.IsEmpty)
                Size = lastSize;
            _initialLocation = Location;
            _initialSize = Size;
            _loadedPosAndSize = true;

            listChatters.DrawMode = DrawMode.OwnerDrawFixed;
            listChatters.DrawItem += ListChatters_DrawItem;

            var ytWebView = new WebView2 { Dock = DockStyle.Fill };
            await ytWebView.EnsureCoreWebView2Async();

            bot = new Bot(this);
            bot.UpdatedTwitchConnected += Bot_UpdatedTwitchConnected;
            bot.BadCredentials += Bot_BadCredentials;
            bot.Follow += Bot_Follow;
            ChatActivity.UpdatedUsersInChat += ((EventHandler)Bot_UpdatedUsersInChat).Debounce(10000);
            await bot.Init(ytWebView).ThrowMainThread();
            Bot.Log("bot initialized");

            if (Settings.Default.EnableMediaHotkeys)
            {
                HotkeyRegisteredHandle = this.Handle;
                bool ok =
                    RegisterHotKey(HotkeyRegisteredHandle, 0, 0, VK_MEDIA_NEXT_TRACK) &
                    RegisterHotKey(HotkeyRegisteredHandle, 0, 0, VK_MEDIA_PREV_TRACK) &
                    RegisterHotKey(HotkeyRegisteredHandle, 0, 0, VK_MEDIA_PLAY_PAUSE) &
                    RegisterHotKey(HotkeyRegisteredHandle, 0, 0, VK_PAUSE);
                if (!ok)
                    Bot.Log("[ERR] Failed to register global hotkeys");
            }
        }

        private void ListChatters_DrawItem(object sender, DrawItemEventArgs e)
        {
#if DEBUG
            try
            {
#endif
                if (e.Index < 0) return;
                e.DrawBackground();
                bool selected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);
                var chatter = (Chatter)listChatters.Items[e.Index];
                var backcolor = selected ? SystemColors.MenuHighlight : Color.FromArgb(255, Color.FromArgb(chatter.groupColorRGB));
                float oneOver255 = 0.003921568627451f;
                var brightness = backcolor.R * oneOver255 * .2126f + backcolor.G * oneOver255 * .7152f + backcolor.B * oneOver255 * .0722f;
                var forecolor = brightness > .5f ? Color.Black : Color.White;

                e.Graphics.FillRectangle(new SolidBrush(backcolor), e.Bounds);
                e.Graphics.DrawString(chatter.DisplayName, e.Font, new SolidBrush(forecolor), e.Bounds, StringFormat.GenericDefault);
                e.DrawFocusRectangle();
#if DEBUG
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
#endif
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            IntPtr h = GetSystemMenu(Handle, false);
            AppendMenu(h, MF_SEPARATOR, 0xDEAD, string.Empty);
            AppendMenu(h, MF_STRING, 0, "Re&position");
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SYSCOMMAND:
                    if (m.WParam == 0)
                    {
                        Location = _initialLocation;
                        Size = _initialSize;
                    }
                    break;
                case WM_HOTKEY:
                    if (m.WParam.ToInt32() == 0)
                    {
                        switch (m.LParam >> 16)
                        {
                            case VK_MEDIA_NEXT_TRACK:
                                _ = Task.Run(SongRequest.Next);
                                return;
                            case VK_MEDIA_PREV_TRACK:
                                _ = Task.Run(SongRequest.PlaylistBackOne);
                                return;
                            case VK_MEDIA_PLAY_PAUSE:
                                _ = Task.Run(SongRequest.PlayPause);
                                return;
                            case VK_PAUSE:
#if !DEBUG
                                _ = Task.Run(bot.DoShowBrb);
#endif
                                return;
                        }
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        #region Global Hotkeys

        public static void UnregisterHotKeys()
        {
            if (HotkeyRegisteredHandle != IntPtr.Zero)
                UnregisterHotKey(HotkeyRegisteredHandle, 0);
        }

        const int VK_MEDIA_NEXT_TRACK = 0xB0;
        const int VK_MEDIA_PREV_TRACK = 0xB1;
        const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        const int VK_PAUSE = 0x13;
        const int WM_HOTKEY = 0x0312;
        const int WM_SYSCOMMAND = 0x112;
        const int MF_STRING = 0x0;
        const int MF_SEPARATOR = 0x800;
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool InsertMenu(IntPtr hMenu, int uPosition, int uFlags, int uIDNewItem, string lpNewItem);

        #endregion

        private void Bot_Follow(object sender, string followerName)
        {
            BeginInvoke(() =>
            {
                listRecentFollows.Items.Add(followerName);
            });
        }

        List<int> _recentFollowsIndicesToRemove = new();
        private async void btnBanSelectedFollows_Click(object sender, EventArgs e)
        {
            if (listRecentFollows.SelectedIndices.Count == 0)
                return;
            var ogText = btnBanSelectedFollows.Text;
            btnBanSelectedFollows.Text = "banning...";
            btnBanSelectedFollows.Enabled = false;

            _recentFollowsIndicesToRemove.Clear();
            for (int i = 0; i < listRecentFollows.SelectedIndices.Count; i++)
                _recentFollowsIndicesToRemove.Add(listRecentFollows.SelectedIndices[i]);

            int totAttempts = 0;
            int totBans = 0;
            int totFails = 0;
            const string reason = "From recent-follows list";
            try
            {
                foreach (int _idx in _recentFollowsIndicesToRemove)
                {
                    int idx = _idx - totAttempts;
                    var name = (string)listRecentFollows.Items[idx];
                    totAttempts++;
                    if (await bot.Ban(name, reason).ConfigureAwait(true))
                        totBans++;
                    else
                        totFails++;
                    listRecentFollows.Items.RemoveAt(idx);
                }
            }
            catch (Exception ex)
            {
                Bot.Log($"[Ban recent-follows] ERROR with {totAttempts} attempts, {totBans} bans and {totFails} fails. Err: {ex}");
                btnBanSelectedFollows.Text = "error, see logs";
                await Task.Delay(2000).ConfigureAwait(true);
            }
            finally
            {
                Bot.Log($"[Ban recent-follows] Finished with {totAttempts} attempts, {totBans} bans and {totFails} fails");
                btnBanSelectedFollows.Enabled = true;
                btnBanSelectedFollows.Text = ogText;
                listRecentFollows.TopIndex = _recentFollowsIndicesToRemove[0]; // doesn't throw
            }
        }

        private void Bot_BadCredentials(object sender, EventArgs e)
        {
            MessageBox.Show("Failed to authenticate or something like that, closing now...");
            Application.Exit();
        }

        private async void Bot_UpdatedTwitchConnected(object sender, EventArgs e)
        {
            await Task.Delay(100);
            var isConnected = bot._tw.IsConnected;
            this.Invoke(() =>
            {
                if (isConnected)
                {
                    lblTwConnected.Text = "Twitch connected (" + bot.CHANNEL + ")";
                    lblTwConnected.ForeColor = Color.ForestGreen;
                }
                else
                {
                    lblTwConnected.Text = "Twitch diconnected";
                    lblTwConnected.ForeColor = Color.Firebrick;
                }
            });
        }

        private void Bot_UpdatedUsersInChat(object sender, EventArgs e)
        {
            if (_freezeChattersList)
                return;
            RefreshUsersInChat();
        }

        void RefreshUsersInChat()
        {
            var users = ChatActivity.UsersInChat().Select(ChatterDataMgr.Get).ToArray();
            Array.Sort(users, (a, b) =>
            {
                int cmp = (b.groupColorRGB - a.groupColorRGB);
                if (cmp != 0) return cmp;
                return a.name.CompareTo(b.name);
            });
            this.Invoke(() =>
            {
                labelChatters.Text = $"Chatters: ({users.Length})";
                listChatters.Items.Clear();
                listChatters.SuspendLayout();
                foreach (var user in users)
                    listChatters.Items.Add(user);
                listChatters.ResumeLayout(true);
            });
        }

        // https://twitchinsights.net/bots
        private async void btnMassBan_Click(object sender, EventArgs e)
        {
            ofd.Title = "Choose a list of twitch names to ban, separated by space/comma/tabs/lines";
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            var ogText = btnMassBan.Text;
            btnMassBan.Enabled = false;
            btnMassBan.Text = "banning...";
            int totBans = 0;
            int totFails = 0;
            var fileName = ofd.FileName;
            var listName = Path.GetFileNameWithoutExtension(fileName) ?? fileName;
            var reason = "mass ban list: " + listName;
            try
            {
                foreach (var line in File.ReadLines(fileName))
                {
                    int i = 0;
                    for (int j = 0; j < line.Length; j++)
                    {
                        if (!(line[j] is ' ' or ',' or '\n' or '\r' or '\t'))
                            continue;
                        if (i != j)
                        {
                            if (await bot.Ban(line[i..j], reason).ConfigureAwait(true))
                                totBans++;
                            else
                                totFails++;
                            btnMassBan.Text = $"banning...\r\nbanned {totBans}, failed {totFails}";
                        }
                        i = j + 1;
                    }
                    if (i != line.Length)
                    {
                        if (await bot.Ban(line[i..], reason).ConfigureAwait(true))
                            totBans++;
                        else
                            totFails++;
                        btnMassBan.Text = $"banning...\r\nbanned {totBans}, failed {totFails}";
                    }
                }
            }
            catch (Exception ex)
            {
                Bot.Log($"[Mass ban: {listName}] ERROR: {ex}");
                btnMassBan.Text = "error, see logs";
                await Task.Delay(2000).ConfigureAwait(true);
            }
            finally
            {
                btnMassBan.Text = $"DONE\r\nbanned {totBans}, failed {totFails}";
                Bot.Log($"[Mass ban: {listName}] {totBans} bans and {totFails} fails");
                await Task.Delay(2000).ConfigureAwait(true);
                btnMassBan.Enabled = true;
                btnMassBan.Text = ogText;
            }
        }

        private void listChatters_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                listChatters.ClearSelected();
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                Clipboard.SetText(string.Join("\r\n", listChatters.SelectedItems.Cast<object>()));
            }
        }

        private void cbFreezeChattersList_CheckedChanged(object sender, EventArgs e)
        {
            btnUpdateChatters.Enabled = _freezeChattersList;
            _freezeChattersList = !_freezeChattersList;
        }

        private void listChatters_SelectedIndexChanged(object sender, EventArgs e)
        {
            var multi = listChatters.SelectedIndices.Count > 1;
            if (_isMultiselectingChatters == multi)
                return;
            _isMultiselectingChatters = multi;
            // automatically freeze when multiselecting and unfreeze when dropping multiselection
            cbFreezeChattersList.Checked = multi;
        }

        private async void btnBanKnownBotsInChat_Click(object sender, EventArgs e)
        {
            try
            {
                btnBanKnownBotsInChat.Enabled = false;
                var users = ChatActivity.UsersInChat().ToHashSet();
                using var www = new HttpClient();
                var knownBots = JsonConvert.DeserializeObject<KnownBots>(await www.GetStringAsync("https://api.twitchinsights.net/v1/bots/all"));
                var successCount = 0;
                var botNameCanonical = bot.BOT_NAME.CanonicalUsername();
                foreach (var knownBot in knownBots.bots)
                {
                    long stalkCount = (long)knownBot[1];
                    if (stalkCount < 30)
                        continue;
                    string name = (knownBot[0] + "").CanonicalUsername();
                    if (name == "commanderroot" || name == "minecool_yt" || name == botNameCanonical || !users.Contains(name) || ChatActivity.IsIgnoredBot(name))
                        continue;
                    var success = await bot.Ban(name, $"known bot (stalkCount {stalkCount})");
                    if (success)
                        successCount++;
                }
                if (successCount > 0)
                {
                    Bot.Log("[Ban known bots] Banned " + successCount + " users");
                    MessageBox.Show("Banned " + successCount + " users");
                }
            }
            catch (Exception ex)
            {
                Bot.Log("[Ban known bots] ERROR: " + ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBanKnownBotsInChat.Enabled = true;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listChatters.SelectedItems.Count == 0)
                return;

            colorDialog.Color = Color.FromArgb(255, Color.FromArgb(((Chatter)listChatters.SelectedItems[0]).groupColorRGB));
            if (colorDialog.ShowDialog() != DialogResult.OK)
                return;

            var color = colorDialog.Color;
            foreach (Chatter chatter in listChatters.SelectedItems)
                chatter.groupColorRGB = color.ToArgb();
            ChatterDataMgr.Update();
            RefreshUsersInChat();
        }

        struct KnownBots
        {
            public object[][] bots;
        }
    }
}