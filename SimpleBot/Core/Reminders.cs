using AxWMPLib;
using Humanizer;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Timer = System.Threading.Timer;

namespace SimpleBot
{
    static class Reminders
    {
        class UserAlarm
        {
            public string DisplayName, Title;
            public DateTime utc;
            [JsonIgnore]
            public Timer timer;
        }
        static List<UserAlarm> _alarms = [];
        static readonly object _lock = new();
        static string _filePath;

        public static void Load(string filePath)
        {
            lock (_lock)
            {
                _filePath = filePath;
                try
                {
                    _alarms = File.ReadAllText(_filePath).FromJson<List<UserAlarm>>();
                }
                catch (FileNotFoundException) { }
                catch
                {
                    Debugger.Break();
                }
                _alarms ??= [];
            }
        }

        static void _save_noLock()
        {
#if DEBUG
            return;
#endif
            if (string.IsNullOrWhiteSpace(_filePath))
                return;
            try
            {
                File.WriteAllText(_filePath, _alarms.ToJson());
            }
            catch
            {
                Debugger.Break();
            }
        }

        public static void Init()
        {
            var deadAlarms = new List<UserAlarm>();
            var now = DateTime.UtcNow;
            lock (_lock)
            {
                foreach (var a in _alarms)
                {
                    var dur = a.utc - now;
                    if (dur.Ticks <= 0)
                    {
                        deadAlarms.Add(a);
                        continue;
                    }
                    _InitAlarmTimer(a, dur);
                }
            }
            _ = Task.Run(async () =>
            {
                while (!Bot.ONE._tw.IsConnected)
                    await Task.Delay(200);
                foreach (var a in deadAlarms)
                    OnAlarmDeadline(a, false);
                lock (_lock)
                {
                    _save_noLock();
                }
            });
        }

        public static int DeleteAllByUser(string canonicalName)
        {
            int count = 0;
            lock (_lock)
            {
                for (int i = _alarms.Count - 1; i >= 0; i--)
                {
                    var a = _alarms[i];
                    if (string.Equals(canonicalName, a.DisplayName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _alarms.RemoveAt(i);
                        if (a.timer != null)
                        {
                            try
                            {
                                a.timer.Dispose();
                            }
                            catch { }
                        }
                        count++;
                    }
                }
                if (count > 0)
                    _save_noLock();
            }
            return count;
        }

        public static void Show(Bot bot, Chatter chatter)
        {
            string dispName = chatter.displayName;
            string userAlarms = "";
            lock (_lock)
            {
                for (int i = 0; i < _alarms.Count; i++)
                {
                    var a = _alarms[i];
                    if (a.DisplayName == dispName)
                    {
                        if (userAlarms != "")
                            userAlarms += " | ";
                        userAlarms += (string.IsNullOrWhiteSpace(a.Title) ? "untitled timer" : a.Title) + " (in " + (a.utc - DateTime.UtcNow).Humanize(precision: 3, minUnit: TimeUnit.Second) + ")";
                    }
                }
            }
            if (userAlarms == "")
                userAlarms = "You have no timers running";
            bot.TwSendMsg(userAlarms, chatter);
        }

        public static void Add(Bot bot, Chatter chatter, string timeArg, string argsStr)
        {
            if (!tryParseDuration(timeArg, out TimeSpan dur))
            {
                bot.TwSendMsg("Invalid duration e.g. 2h30m5s");
                return;
            }
            var title = argsStr[timeArg.Length..].Trim();
            ChatActivity.IncCommandCounter(chatter, BotCommandId.Reminders_Add);
            var alarm = new UserAlarm() { DisplayName = chatter.DisplayName, Title = title, utc = DateTime.UtcNow.Add(dur) };
            lock (_lock)
            {
                _alarms.Add(alarm);
                _save_noLock();
            }
            bot.TwSendMsg("Alarm set SeemsGood", chatter);
            _InitAlarmTimer(alarm, dur);
        }

        static void _InitAlarmTimer(UserAlarm alarm, TimeSpan dur)
        {
            if (dur.TotalDays > 2)
                return;
            alarm.timer = new Timer(_OnAlarmDeadline, alarm, dur, Timeout.InfiniteTimeSpan /* not repeating */);
        }

        static void _OnAlarmDeadline(object state) => OnAlarmDeadline(state as UserAlarm, true);
        static void OnAlarmDeadline(UserAlarm alarm, bool save)
        {
            if (alarm == null)
            {
                Bot.Log("[ERR] at " + nameof(OnAlarmDeadline) + " alarm object is null");
                Bot.ONE.TwSendMsg("[ERR] alarm object is null D:");
                return;
            }
            Bot.ONE.TwSendMsg("@" + alarm.DisplayName + " 🔔 Time is up 🔔 " + alarm.Title);
            lock (_lock)
            {
                alarm.timer?.Dispose();
                _alarms.Remove(alarm);
                if (save)
                    _save_noLock();
            }
        }

        // 2h30m5s
        // 1h90m0s
        static readonly Regex RGX_DURATION = new Regex(@"^(?<h>\d+h)?(?<m>\d+m)?(?<s>\d+s)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static bool tryParseDuration(string timeArg, out TimeSpan dur)
        {
            dur = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(timeArg))
                return false;
            var match = RGX_DURATION.Match(timeArg);
            if (!match.Success)
                return false;
            int H, M, S;
            H = M = S = 0;
            var h = match.Groups["h"];
            if (h.Success && !int.TryParse(h.Value.AsSpan(0, h.Value.Length - 1), out H))
                return false;
            var m = match.Groups["m"];
            if (m.Success && !int.TryParse(m.Value.AsSpan(0, m.Value.Length - 1), out M))
                return false;
            var s = match.Groups["s"];
            if (s.Success && !int.TryParse(s.Value.AsSpan(0, s.Value.Length - 1), out S))
                return false;
            dur = TimeSpan.FromSeconds(S + M * 60 + H * 3600);
            return true;
        }
    }
}
