using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Timer = System.Threading.Timer;

namespace SimpleBot.v2
{
    class ChatActivity
    {
        readonly object _lock = new();
        readonly Bot _bot;
        readonly string _fileName;
        readonly HashSet<string> _ignoredBots = [];
        readonly HashSet<string> _usersInChat = [];
        readonly List<(DateTime ts, Chatter chatter)> _activityHistory = [];

        // twitch seems to buffer join/part events and notify every 30 seconds, no point in polling quicker than that
        const int UPDATE_WATCHTIME_PERIOD_MS = 32700; // a bit over 30 seconds
        readonly Timer _updateWatchtimeTimer;

        public event EventHandler UpdatedUsersInChat;

        public ChatActivity(Bot bot)
        {
            _bot = bot;
            _fileName = bot.UserPath("ignored_bots");
            _ignoredBots = [.. File.ReadAllText(_fileName).FromJson<string[]>()];
            _bot._tw.OnExistingUsersDetected += twOnExistingUsersDetected;
            _bot._tw.OnUserJoined += twOnUserJoined;
            _bot._tw.OnUserLeft += twOnUserLeft;
            _updateWatchtimeTimer = new Timer(tick_updateWatchtimeTimer, null, UPDATE_WATCHTIME_PERIOD_MS, UPDATE_WATCHTIME_PERIOD_MS);
        }

        void _save_noLock()
        {
#if DEBUG
            return;
#endif
            File.WriteAllText(_fileName, _ignoredBots.ToArray().ToJson());
        }

        HashSet<string> _watchtime_prevUsers = [];
        HashSet<string> _watchtime_users = [];
        void tick_updateWatchtimeTimer(object _)
        {
            if (!_bot.IsStreaming) return;
            lock (_lock)
            {
                _watchtime_users.UnionWith(_usersInChat);
            }
            _bot.chatters.ForEach(_watchtime_users.Where(u => !_watchtime_prevUsers.Contains(u)), u =>
            {
                u.watchtime_ms += UPDATE_WATCHTIME_PERIOD_MS;
                u.SetDirty();
            });
            (_watchtime_prevUsers, _watchtime_users) = (_watchtime_users, _watchtime_prevUsers);
            _watchtime_users.Clear();
        }

        /// <summary>
        /// Called when message is recieved, returning null for ignored users, or the associated ChatterData.
        /// Used for stats like watchtime, activity monitoring, etc
        /// </summary>
        public Chatter _OnMessage(ChatMessage msg)
        {
            // TODO? log all chat
            var name = msg.Username.CanonicalUsername();
            if (IsIgnoredBot(name))
                return null;

            var chatter = _bot.chatters.Get(name);
            lock (_lock)
            {
                _activityHistory.Add((DateTime.UtcNow, chatter));
            }

            var msgUserLevel = msg.GetUserLevel();
            if (chatter.DisplayName != msg.DisplayName ||
                chatter.userLevel != msgUserLevel ||
                chatter.uid != msg.UserId)
            {
                chatter.DisplayName = msg.DisplayName;
                chatter.userLevel = msgUserLevel;
                chatter.uid = msg.UserId;
                chatter.SetDirty();
            }
            return chatter;
        }

        public int AddIgnoredBot(string[] names)
        {
            lock (_lock)
            {
                int added = 0;
                foreach (var name in names)
                {
                    if (_ignoredBots.Add(name))
                        added++;
                }
                if (added != 0) _save_noLock();
                return added;
            }
        }

        public int RemoveIgnoredBot(string[] names)
        {
            lock (_lock)
            {
                int removed = 0;
                foreach (var name in names)
                {
                    if (_ignoredBots.Remove(name))
                        removed++;
                }
                if (removed != 0) _save_noLock();
                return removed;
            }
        }

        public int GetIgnoredBotsCount()
        {
            lock (_lock)
            {
                return _ignoredBots.Count;
            }
        }

        public bool IsIgnoredBot(string canonicalName)
        {
            lock (_lock)
            {
                return _ignoredBots.Contains(canonicalName);
            }
        }

        public bool IsUserInChat(string canonicalName)
        {
            lock (_lock)
            {
                return _usersInChat.Contains(canonicalName);
            }
        }

        public string[] UsersInChat()
        {
            lock (_lock)
            {
                return _usersInChat.ToArray();
            }
        }

        public string RandomChatter()
        {
            lock (_lock)
            {
                return _usersInChat.ToArray().AtRand() ?? "";
            }
        }

        public HashSet<Chatter> GetActiveChatters(TimeSpan span, int maxChattersNeeded = 0)
        {
            var minTime = DateTime.UtcNow.Subtract(span);
            var res = new HashSet<Chatter>();
            if (maxChattersNeeded <= 0)
                maxChattersNeeded = int.MaxValue;
            lock (_lock)
            {
                for (int i = _activityHistory.Count - 1; i >= 0; i--)
                {
                    var (ts, chatter) = _activityHistory[i];
                    if (ts < minTime)
                        break;
                    if (chatter.userLevel == UserLevel.Streamer)
                        continue;
                    res.Add(chatter);
                    if (res.Count >= maxChattersNeeded)
                        break;
                }
            }
            return res;
        }

        private void twOnUserLeft(object sender, OnUserLeftArgs e)
        {
            var name = e.Username.CanonicalUsername();
            if (IsIgnoredBot(name))
                return;

            lock (_lock)
            {
                _usersInChat.Remove(name);
            }
            UpdatedUsersInChat?.Invoke(null, EventArgs.Empty);
        }

        private void twOnUserJoined(object sender, OnUserJoinedArgs e)
        {
            _twOnUserJoined(e.Username);
        }

        private void twOnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
        {
            foreach (var user in e.Users)
                _twOnUserJoined(user);
        }

        private void _twOnUserJoined(string name)
        {
            name = name.CanonicalUsername();
            if (IsIgnoredBot(name))
                return;

            lock (_lock)
            {
                _usersInChat.Add(name);
            }
            UpdatedUsersInChat?.Invoke(null, EventArgs.Empty);
        }
    }
}
