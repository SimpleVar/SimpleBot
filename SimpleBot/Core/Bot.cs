using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using SimpleBot.Properties;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Chat;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Extensions;

namespace SimpleBot
{
    // TODO when we have chat overlay - FLAG badge (credit EveSingularity)
    class Bot
    {
        public readonly string CHANNEL = Settings.Default.Channel;
        public readonly string BOT_NAME = Settings.Default.TwitchBotUsername;
        public readonly string USER_DATA_FOLDER = Settings.Default.UserDataFolder;
        // There is an implicit assumption that CMD_PREFIX begins with a non-alphanumeric char
#if DEBUG
    public readonly string CMD_PREFIX = "!" + Settings.Default.CommandsPrefix;
#else
        public readonly string CMD_PREFIX = Settings.Default.CommandsPrefix;
#endif

        #region UI events

        public event EventHandler BadCredentials;
        public event EventHandler UpdatedTwitchConnected;
        public event EventHandler UpdatedOBSConnected;
        public event EventHandler<string> Follow;

        #endregion

        public TwitchClient _tw;
        public TwitchAPI _twApi;
        public TwitchApi_MoreEdges _twApi_More;
        public OBSWebsocket _obs;

        public bool IsOnline { get; private set; } = true;
        public string CHANNEL_ID { get; private set; }
        public string BOT_ID { get; private set; }
        readonly JoinedChannel _twJC; // fake object with no data for quick TwSendMessage
        readonly ConcurrentDictionary<string, int> _redeemCounts = new(); // value = count, key = "user_id;reward_id"
        static string _rewardsKey(string userId, string rewardId) => userId + ";" + rewardId;

#if !DEBUG
        static string _logFilePath;
#endif
        bool _isBrbEnabled;

        public Bot()
        {
#if !DEBUG
            _logFilePath = Application.StartupPath + "logs\\";
            Directory.CreateDirectory(_logFilePath);
            _logFilePath += $"{DateTime.Now:yyyy_MM_dd}.txt";
#endif

            Log("[init] Bot ctor");
            _twJC = new JoinedChannel(CHANNEL);

            // Load persistent data
            if (string.IsNullOrWhiteSpace(USER_DATA_FOLDER))
                return;

            Log("[init] loading user data");
            Directory.CreateDirectory(Path.Combine(USER_DATA_FOLDER, "data"));
            Directory.CreateDirectory(Path.Combine(USER_DATA_FOLDER, "obs_labels"));
            ChatterDataMgr.Load(Path.Combine(USER_DATA_FOLDER, "data\\chatters_data.txt"));
            ChatActivity.Load(Path.Combine(USER_DATA_FOLDER, "data\\chat_activity.txt")); // has IgnoredBotNames
            ViewersQueue.Load(Path.Combine(USER_DATA_FOLDER, "data\\viewers_queue.txt"));
            Quotes.Load(Path.Combine(USER_DATA_FOLDER, "data\\quotes.txt"));
            SongRequest.Load(Path.Combine(USER_DATA_FOLDER, "data\\song_request.txt"));
            LoadCustomCommands(Path.Combine(USER_DATA_FOLDER, "data\\custom_commands.txt"));
            Log("[init] loaded user data");
        }

        bool _init = false;
        public async Task Init(WebView2 ytWebView)
        {
            if (_init)
                throw new ApplicationException("Init should be called exactly once");
            _init = true;
            await Task.Yield();
            ChatterDataMgr.Init();

            if (Settings.Default.obs_enabled)
            {
                Log("[init] _obs");
                _obs = new OBSWebsocket();
                _obs.Connected += (o, e) =>
                {
                    Log("[OBS] connected"); UpdatedOBSConnected?.Invoke(this, EventArgs.Empty);
                    IsOnline = _obs.GetStreamStatus().IsActive;
                };
                _obs.Disconnected += (o, e) =>
                {
                    Log("[OBS] disconnected: " + e.ToJson()); UpdatedOBSConnected?.Invoke(this, EventArgs.Empty);
                };
                _obs.StreamStateChanged += (o, e) =>
                {
                    Log("[OBS] state changed: " + e.OutputState.StateStr);
                    switch (e.OutputState.State)
                    {
                        case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                            IsOnline = true;
                            break;
                        case OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                            IsOnline = false;
                            break;
                    }
                };
                _obs.ConnectAsync(Settings.Default.ObsWebsocketUrl, Settings.Default.ObsWebsocketPassword);
                //_obs.SetInputSettings("VS", new JObject { { "text", "LETS FUCKING GO" } });
                // TODO test around obs disconnecting or not existing on init
                // TODO? when obs process closes, stop being a bot and maybe close -- _obs.ExitStarted
            }

            Log("[init] _tw");
            _tw = new TwitchClient(new WebSocketClient(new ClientOptions { DisconnectWait = 5000 }));
            _tw.Initialize(new ConnectionCredentials(BOT_NAME, File.ReadAllText(Settings.Default.TwitchOAuthBot)), CHANNEL);

            _tw.OnLog += (o, e) => Log("[twLog] " + e.Data);
            _tw.OnConnected += (o, e) =>
            {
                Log("[tw] connected");
                UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty);
#if !DEBUG
                TwSendMsg("/me is connected");
#endif
            };
            _tw.OnReconnected += (o, e) => { Log("[tw] reconnected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
            _tw.OnDisconnected += (o, e) => { Log("[tw] disconnected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
            _tw.OnError += (o, e) => { Log("[twErr] " + e.Exception); };
            _tw.OnConnectionError += (o, e) => { Log("[twErr]:conn " + e?.Error?.Message); };
            _tw.OnNoPermissionError += (o, e) => { Log("[twErr]:perm"); };
            _tw.OnIncorrectLogin += (o, e) => { Log("[twErr]:login"); BadCredentials?.Invoke(this, EventArgs.Empty); };
            _tw.OnMessageReceived += (o, e) =>
            {
                try
                {
                    twOnMessage(o, e);
                }
                catch (Exception ex)
                {
                    Application.OnThreadException(ex);
                }
            };

            Log("[init] _twApi");
            _twApi = new TwitchAPI(settings: new ApiSettings { ClientId = Settings.Default.TwitchClientId, AccessToken = File.ReadAllText(Settings.Default.TwitchOAuth) });
            Log("[init] _twApi_More");
            _twApi_More = new TwitchApi_MoreEdges(_twApi.Settings);
            Log("[init] fetching user ids");
            CHANNEL_ID = GetUserIdOrNull(CHANNEL);
            BOT_ID = GetUserIdOrNull(BOT_NAME);
            if (CHANNEL_ID == null || BOT_ID == null)
                throw new ApplicationException("Failed to get twitch user id of the streamer or the bot account");
            /* TODO for !redeems TwitchApi broken? I'm dumb? maybe one day get back to it
            var customRewards = (await _twApi.Helix.ChannelPoints.GetCustomRewardAsync(CHANNEL_ID).ConfigureAwait(true)).Data;
            var redemptions = new List<RewardRedemption>();
            foreach (var r in customRewards)
            {
              redemptions.AddRange(await TwitchApiExtensions.AggregatePages(after => _twApi.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(CHANNEL_ID, r.Id, status: "FULFILLED", first: "50", after: after), x => x.Pagination, x => x.Data));
              redemptions.AddRange(await TwitchApiExtensions.AggregatePages(after => _twApi.Helix.ChannelPoints.GetCustomRewardRedemptionAsync(CHANNEL_ID, r.Id, status: "UNFULFILLED", first: "50", after: after), x => x.Pagination, x => x.Data));
            }
            */

            // Init custom thingies
            Log("[init] various systems");
            await SongRequest.Init(this, ytWebView);
            ChatActivity.Init(this);
            if (Settings.Default.enable_all_SimpleVar_systems)
            {
                SneakyJapan.Init(this);
                LearnHiragana.Init(this);
                LongRunningPeriodicTask.Start(0, true, 1200123, 600000, 0, async _ =>
                {
                    if (!IsOnline) return;
                    if (ChatActivity.GetActiveChatters(TimeSpan.FromMilliseconds(1200123), maxChattersNeeded: 2).Count < 2)
                        return;
                    await _twApi_More.Announce(CHANNEL_ID, CHANNEL_ID,
              "Simple Tree House https://discord.gg/48dDcAPwvD is where I chill and hang out :)",
              AnnouncementColors.Blue);
                });
                /*
                Chatter[] shoutouts = (new[]
                {
                  "oBtooce",
                }).Select(x => ChatterDataMgr.GetOrNull(x.CanonicalUsername()))
                  .Where(x => x != null)
                  .ToArray();
                string[] shoutoutIds = shoutouts.Select(x => x.uid).ToArray();
                Log("Auto-Shoutouts: " + string.Join(' ', shoutouts.Select(x => x.DisplayName)));
                if (shoutoutIds.Length > 0)
                {
                  LongRunningPeriodicTask.Start(0, true, 300042, 570000, 0, rid =>
                  {
                    var uid = shoutoutIds[rid % shoutoutIds.Length];
                    var res = _twApi_More.Shoutout(CHANNEL_ID, CHANNEL_ID, uid).GetAwaiter().GetResult();
                    return res ? null : 120000;
                  });
                }
                */
            }

#if !DEBUG
            if (_obs != null && Settings.Default.active_window_tracking_enabled)
            {
                ForegroundWinUtil.Init();
                bool isVsVisible = false;
                int browserItemId = -1;

#if false
                string IDE_PROC_NAME = "Code";
                string IDE_OBS_ITEM_NAME = "vscode";
#else
                string IDE_PROC_NAME = "devenv";
                string IDE_OBS_ITEM_NAME = "VS";
#endif
                ForegroundWinUtil.ForgroundWindowChanged += (o, e) =>
                {
                    var TWITCH_CHAT_TITLE = CHANNEL + " - Chat - Twitch";
                    var shouldShowVS = e.procName == IDE_PROC_NAME;
                    if (!shouldShowVS && (e.procName != "brave" || e.title.StartsWith(TWITCH_CHAT_TITLE)))
                        return;
                    if (isVsVisible == shouldShowVS)
                        return;
                    isVsVisible = shouldShowVS;

                    if (!_obs.IsConnected)
                        return;
                    if (browserItemId == -1) browserItemId = _obs.GetSceneItemId("CODE", "Browser", 0);
                    var vsItemId = _obs.GetSceneItemId("CODE", IDE_OBS_ITEM_NAME, 0);
                    var vsIdx = _obs.GetSceneItemIndex("CODE", vsItemId);
                    var browserIdx = _obs.GetSceneItemIndex("CODE", browserItemId);
                    // looks stupid but obs is stupid
                    var newVsIdx = browserIdx + (shouldShowVS ? (vsIdx > browserIdx ? 1 : 0) : (vsIdx > browserIdx ? 0 : -1));
                    _obs.SetSceneItemIndex("CODE", vsItemId, newVsIdx);
                };
            }
#endif

            _tw.Connect();

            #region Events Sub

            Log("[twEventSub] cleanup start");
            // Event Subs continue reacting to events even when offline
            var existingEventSubs = await TwitchApiExtensions.AggregatePages(after => _twApi.Helix.EventSub.GetEventSubSubscriptionsAsync(after: after), x => x.Pagination, x => x.Subscriptions).ConfigureAwait(true);
            try
            {
                foreach (var e in existingEventSubs)
                {
                    if (e.Status == "enabled")
                        continue;
                    await _twApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(e.Id);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            Log("[twEventSub] init start");
            var serviceProvider = new ServiceCollection().AddLogging().AddTwitchLibEventSubWebsockets().BuildServiceProvider();
            var twEventSub = serviceProvider.GetService<EventSubWebsocketClient>();
            twEventSub.ErrorOccurred += (o, e) =>
            {
                Log("[twEventSub ERR] " + e.ToJson());
            };
            twEventSub.WebsocketConnected += async (o, e) =>
            {
                if (e.IsRequestedReconnect)
                    return;
                Log("[twEventSub] starting to listen to events");
                // subscribe to events https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
                var evSubs = new[]
          {
          new TwEventSubReq(1, "channel.channel_points_custom_reward_redemption.add").Cond("broadcaster_user_id", CHANNEL_ID),
          new TwEventSubReq(2, "channel.follow").Cond("broadcaster_user_id", CHANNEL_ID).Cond("moderator_user_id", CHANNEL_ID),
          new TwEventSubReq(1, "channel.subscribe").Cond("broadcaster_user_id", CHANNEL_ID),
          new TwEventSubReq(1, "channel.subscription.gift").Cond("broadcaster_user_id", CHANNEL_ID),
          new TwEventSubReq(1, "channel.subscription.message").Cond("broadcaster_user_id", CHANNEL_ID),
          new TwEventSubReq(1, "channel.cheer").Cond("broadcaster_user_id", CHANNEL_ID),
          new TwEventSubReq(1, "channel.raid").Cond("to_broadcaster_user_id", CHANNEL_ID),
          new TwEventSubReq(1, "channel.charity_campaign.donate").Cond("broadcaster_user_id", CHANNEL_ID),
              };
                foreach (var evSub in evSubs)
                {
                    var res = await _twApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                evSub.type,
                evSub.version + "",
                evSub.conditions,
                EventSubTransportMethod.Websocket,
                twEventSub.SessionId
              ).ThrowMainThread().ConfigureAwait(true);
                    Debug.WriteLine("[twEventSub INIT] " + res.ToJson());
                }
                Log("[twEventSub] listening to all events successfuly");
            };
            int ccReconnectTime = 1000;
            twEventSub.WebsocketDisconnected += async (o, e) =>
            {
                while (!await twEventSub.ReconnectAsync())
                {
                    await Task.Delay(ccReconnectTime);
                    if (ccReconnectTime < 300000)
                        ccReconnectTime *= 2;
                }
            };
            twEventSub.ChannelPointsCustomRewardRedemptionAdd += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                //_redeemCounts.AddOrUpdate(_rewardsKey(ev.UserId, ev.Reward.Id), 1, (k, v) => v + 1);
                // handle redeems that have NO input
                Log("[Redeem] " + ev.ToJson());
                switch (ev.Reward.Title)
                {
                    case "FIRST":
                        TwSendMsg($"Nice. @{ev.UserName}");
                        break;
                    case "Japan":
                        TwSendMsg("Japan, baby!");
                        SneakyJapan.Buff(ChatterDataMgr.Get(ev.UserName.CanonicalUsername()), 3);
                        break;
                    case "Japan!!":
                        TwSendMsg("JAPAN, baby!!");
                        SneakyJapan.Buff(ChatterDataMgr.Get(ev.UserName.CanonicalUsername()), 7);
                        break;
                }
            };
            twEventSub.ChannelSubscribe += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                Log($"[sub] {ev.UserId} {ev.UserName} {ev.Tier} {ev.IsGift}");
                // TODO
            };
            twEventSub.ChannelSubscriptionGift += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                Log(ev.IsAnonymous ? $"[sub-gift] ANON ANON {ev.Tier} x{ev.Total}" : $"[sub-gift] {ev.UserId} {ev.UserName} {ev.Tier} x{ev.Total} (alltime:{ev.CumulativeTotal})");
                // TODO
            };
            twEventSub.ChannelSubscriptionMessage += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                Log($"[sub-msg] {ev.UserId} {ev.UserName} {ev.Tier} {ev.DurationMonths} {ev.StreakMonths ?? -1} (alltime:{ev.CumulativeTotal}) msg:{ev.Message.Text}");
                // TODO
            };
            twEventSub.ChannelCheer += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                Log(ev.IsAnonymous ? $"[cheer] ANON ANON {ev.Bits} x{ev.Message}" : $"[cheer] {ev.UserId} {ev.UserName} {ev.Bits} msg:{ev.Message}");
                // TODO
            };
            twEventSub.ChannelRaid += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                Log($"[raid] {ev.FromBroadcasterUserId} {ev.FromBroadcasterUserName} {ev.Viewers}");
                // TODO
            };
            twEventSub.ChannelCharityCampaignDonate += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                try
                {
                    Log($"[charity] {ev.UserId} {ev.UserName} {(ev.Amount.Value / (decimal)Math.Pow(10, ev.Amount.DecimalPlaces)).ToString("F" + ev.Amount.DecimalPlaces)} {ev.Currency} {ev.CampaignId}");
                }
                catch
                {
                    Log("[charity] json: " + ev.ToJson());
                }
                TwSendMsg("@" + ev.UserName + " Spreads love in the world <3 Clap");
            };
            // limit the number of times we react to follows in chat, to not spam
            DateTime followWindowExpiration = DateTime.MinValue;
            int followWindowCount = 0;
            int MAX_FOLLOW_GREETINGS_PER_WINDOW = Settings.Default.MaxFollowGreetingsPerEveryFewSeconds_negativeMeansNoLimit;
            twEventSub.ChannelFollow += (o, e) =>
            {
                var ev = e.Notification.Payload.Event;
                Log($"[follow] {ev.UserId} {ev.UserName} {ev.FollowedAt}");
                Follow?.Invoke(this, ev.UserName);
                if (MAX_FOLLOW_GREETINGS_PER_WINDOW == 0)
                    return;
                if (MAX_FOLLOW_GREETINGS_PER_WINDOW > 0)
                {
                    // don't send more than X msgs per T time
                    var now = DateTime.UtcNow;
                    // determine current "spam" levels (before we extend the window)
                    followWindowCount = now > followWindowExpiration ? 1 : followWindowCount + 1;
                    // always extend window, to make sure we have at least T time of silence
                    // - even follows that are just-nearly T time apart will still extend the window until considered spam (or until enough silence)
                    followWindowExpiration = now.AddSeconds(10);
                    if (followWindowCount > MAX_FOLLOW_GREETINGS_PER_WINDOW)
                        return;
                }
                TwSendMsg("Thanks for following @" + ev.UserName);
            };
            if (!(await twEventSub.ConnectAsync().ConfigureAwait(true)))
                Log("[twEventSub] failed to connect");

            #endregion
        }

        public static void Log(string msg)
        {
            msg = $"[{DateTime.Now.ToLongTimeString()}] {msg}";
            Debug.WriteLine(msg);
#if !DEBUG
            File.AppendAllText(_logFilePath, msg + '\n');
#endif
        }

        public void TwSendMsg(string msg, Chatter tagChatter = null)
        {
            if (tagChatter != null) msg = $"@{tagChatter.DisplayName} {msg}";
            if (msg.Length > 490)
                msg = msg[0..490] + " .....";
#if DEBUG
      if (msg.Length != 0 && !(msg[0] is '.' or '/'))
        msg = "* " + msg;
#endif
            _tw.SendMessage(_twJC, msg);
        }

        public static readonly ReadOnlyDictionary<BotCommandId, string[]> _builtinCommandsAliases =
          new(new Dictionary<BotCommandId, string[]>()
          {
              [BotCommandId.ListCommands] = new[] { "commands" },
              [BotCommandId.AddIgnoredBot] = new[] { "ignore", "addignore" },
              [BotCommandId.RemoveIgnoredBot] = new[] { "unignore", "remignore" },
              [BotCommandId.SlowModeOff] = new[] { "slowmodeoff" },
              [BotCommandId.SlowMode] = new[] { "slowmode" },
              [BotCommandId.SetTitle] = new[] { "title", "settitle" },
              [BotCommandId.SetGame] = new[] { "game", "setgame" },
              [BotCommandId.StartPoll] = new[] { "poll", "startpoll", "pollstart" },
              [BotCommandId.EndPoll] = new[] { "endpoll", "pollend" },
              [BotCommandId.DelPoll] = new[] { "delpoll", "deletepoll", "polldel", "polldelete" },
              [BotCommandId.AddCustomCommand] = new[] { "addcmd", "addcommand", "addcom" },
              [BotCommandId.DelCustomCommand] = new[] { "delcmd", "delcommand", "delcom" },
              [BotCommandId.EditCustomCommand] = new[] { "editcmd", "editcommand", "editcom" },
              [BotCommandId.ShowCustomCommand] = new[] { "showcmd", "showcommand", "showcom" },
              [BotCommandId.ShowBrb] = new[] { "brb" },
              [BotCommandId.SearchGame] = new[] { "searchgame" },
              [BotCommandId.GetCmdCounter] = new[] { "count" },
              [BotCommandId.GetRedeemCounter] = new[] { "redeems", "countredeem", "countredeems" },
              [BotCommandId.FollowAge] = new[] { "followage" },
              [BotCommandId.WatchTime] = new[] { "watchtime" },
              [BotCommandId.SongRequest_Request] = new[] { "sr" },
              [BotCommandId.SongRequest_Volume] = new[] { "volume", "vol" },
              [BotCommandId.SongRequest_SetVolumeMax] = new[] { "setmaxvolume" },
              [BotCommandId.SongRequest_Next] = new[] { "skip", "skipsong", "nextsong" },
              [BotCommandId.SongRequest_GetPrev] = new[] { "prevsong", "lastsong" },
              [BotCommandId.SongRequest_GetCurr] = new[] { "currsong", "currentsong", "songname", "cs", "song" },
              [BotCommandId.SongRequest_ShufflePlaylist] = new[] { "shuffle" },
              [BotCommandId.SongRequest_WrongSong] = new[] { "wrongsong", "oops" },
              [BotCommandId.Queue_Curr] = new[] { "curr", "current" },
              [BotCommandId.Queue_Next] = new[] { "next" },
              [BotCommandId.Queue_All] = new[] { "queue" },
              [BotCommandId.Queue_Clear] = new[] { "clear" },
              [BotCommandId.Queue_Join] = new[] { "join" },
              [BotCommandId.Queue_Leave] = new[] { "leave" },
              [BotCommandId.Queue_Close] = new[] { "close", "closequeue" },
              [BotCommandId.Queue_Open] = new[] { "open", "openqueue" },
              [BotCommandId.SneakyJapan] = new[] { "japan" },
              [BotCommandId.SneakyJapan_Stats] = new[] { "japanstats" },
              [BotCommandId.SneakyJapan_NewGamePlus] = new[] { "japanplus" },
              [BotCommandId.SneakyJapan_Leaderboard] = new[] { "japanlead", "japanleaderboard" },
              [BotCommandId.Celsius2Fahrenheit] = new[] { "c2f" },
              [BotCommandId.Fahrenheit2Celsius] = new[] { "f2c" },
              [BotCommandId.CoinFlip] = new[] { "coin", "coinflip" },
              [BotCommandId.DiceRoll] = new[] { "roll", "diceroll", "rolldice" },
              [BotCommandId.Quote_Get] = new[] { "quote", "wisdom" },
              [BotCommandId.Quote_Add] = new[] { "addquote", "addwisdom" },
              [BotCommandId.Quote_Del] = new[] { "delquote", "deletequote", "delwisdom", "deletewisdom", "removequote", "removewisdom" },
              [BotCommandId.LearnHiragana] = new[] { "hiragana" },
              [BotCommandId.GetChessRatings] = new[] { "elo", "rating" },
          });
        static readonly ReadOnlyDictionary<string, BotCommandId> _cmdStr2Cid =
          new(new Dictionary<string, BotCommandId>(_builtinCommandsAliases.SelectMany(x => x.Value.Select(alias => new KeyValuePair<string, BotCommandId>(alias, x.Key)))));
        static readonly string _allBuiltinCommands = string.Join(' ', _builtinCommandsAliases.Where(x => x.Key != BotCommandId.ListCommands).Select(x => x.Value[0]));

        static readonly ReadOnlyDictionary<string, string> _commandExpansions =
          new(new Dictionary<string, (BotCommandId cid, string extraText)>()
          {
              ["jc"] = (BotCommandId.SetGame, "jc"),
              ["gjc"] = (BotCommandId.SetGame, "jc"),
              ["gp"] = (BotCommandId.SetGame, "p"),
              ["gc"] = (BotCommandId.SetGame, "c"),
              ["gt"] = (BotCommandId.SetGame, "t"),
          }
          .ToDictionary(x =>
          {
              if (_cmdStr2Cid.ContainsKey(x.Key))
                  throw new ApplicationException("Expansion is shadowing a builtin alias");
              return x.Key;
          }, x => _builtinCommandsAliases[x.Value.cid][0] + ' ' + x.Value.extraText + ' '));

        public static BotCommandId ParseBuiltinCommandId(string cmd)
        {
            if (_cmdStr2Cid.TryGetValue(cmd, out var cid))
                return cid;
            return (BotCommandId)(-1);
        }

        public ChannelInformation GetChannelInfo(string channelName)
        {
            string id = GetUserIdOrNull(channelName);
            if (string.IsNullOrEmpty(id)) return null;
            return _twApi.Helix.Channels.GetChannelInformationAsync(id).GetAwaiter().GetResult()?.Data?.FirstOrDefault();
        }

        public string GetUserIdOrNull(string dirtyName)
        {
            var name = dirtyName?.CanonicalUsername();
            if (string.IsNullOrWhiteSpace(name))
                return null;
            return _twApi.GetUserId(name).GetAwaiter().GetResult();
        }

        private void twOnMessage(object sender, OnMessageReceivedArgs e)
        {
            try
            {
                _twOnMessage(sender, e);
            }
            catch (Exception ex)
            {
                Log("ERROR in twOnMessage: " + ex.ToString());
            }
        }

        private void _twOnMessage(object sender, OnMessageReceivedArgs e)
        {
            Chatter chatter = ChatActivity.OnMessage(e.ChatMessage);
            if (chatter == null) return; // ignored bot user

            // ignore commands when offline
            if (!IsOnline && chatter.userLevel != UserLevel.Streamer) return;

            // TODO use nicknames and mgr for display names and stuff

            var msg = e.ChatMessage.Message;
            // handle redeems that have input
            if (e.ChatMessage.CustomRewardId == "536092ba-85bb-4df8-bd15-286410fe96c6") // Change stream title
            {
                _ = SetGameOrTitle.SetTitle(this, chatter, msg).LogErr();
                return;
            }

            // 7tv or bttv are adding "\U000e0000" to duplicate messages, so remove some junk
            while (msg.Length != 0 && char.GetUnicodeCategory(msg, msg.Length - 1) is
                    UnicodeCategory.OtherNotAssigned or
                    UnicodeCategory.PrivateUse or
                    UnicodeCategory.Surrogate or
                    UnicodeCategory.Control or
                    UnicodeCategory.Format or
                    UnicodeCategory.EnclosingMark or
                    UnicodeCategory.NonSpacingMark or
                    UnicodeCategory.SpacingCombiningMark or
                    UnicodeCategory.LineSeparator or
                    UnicodeCategory.ParagraphSeparator)
                msg = msg[0..^1];
            msg = msg.Trim();
            if (msg.Length == 0)
                return;

            // general moderation
            if (msg.Contains("BANGER", StringComparison.InvariantCulture)) TwSendMsg("It's aaameee!! MARIO");
            else if (msg.Contains("wow", StringComparison.InvariantCultureIgnoreCase)) TwSendMsg("Mama mia");

            if (msg.Length == CMD_PREFIX.Length ||
                char.IsWhiteSpace(msg[CMD_PREFIX.Length]) ||
                !msg.StartsWith(CMD_PREFIX))
            {
                // can't be a command, bitch
                return;
            }
            msg = msg[CMD_PREFIX.Length..];

            // command expansions
            {
                var cmdLength = msg.IndexOf(' ');
                if (cmdLength == -1)
                    cmdLength = msg.Length;
                if (_commandExpansions.TryGetValue(msg[0..cmdLength].ToLowerInvariant(), out string expansion))
                    msg = expansion + msg[cmdLength..];
            }

            // commands
            var args = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var cmd = args[0].ToLowerInvariant();
            args.RemoveAt(0);
            var argsStr = string.Join(' ', args);
            BotCommandId cid = ParseBuiltinCommandId(cmd);

            switch (cid)
            {
                case BotCommandId.ListCommands:
                    TwSendMsg("builtin commands: " + _allBuiltinCommands, chatter);
                    TwSendMsg("editable commands: " + GetAllCustomCommands(), chatter);
                    return;
                // MOD
                case BotCommandId.AddIgnoredBot:
                case BotCommandId.RemoveIgnoredBot:
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    if (args.Count == 0) return;
                    bool isAdding = cid == BotCommandId.AddIgnoredBot;
                    ChatActivity.IncCommandCounter(chatter, isAdding ? BotCommandId.AddIgnoredBot : BotCommandId.RemoveIgnoredBot);
                    var botNames = args.Select(x => x.CleanUsername().CanonicalUsername()).ToArray();
                    int changed = isAdding ? ChatActivity.AddIgnoredBot(botNames) : ChatActivity.RemoveIgnoredBot(botNames);
                    int existed = botNames.Length - changed;
                    string response = changed + " names " + (isAdding ? "added to" : "removed from") + " ignore list";
                    if (isAdding && existed != 0)
                        response += ", " + existed + " were already ignored";
                    response += ". Total ignores: " + ChatActivity.GetIgnoredBotsCount();
                    TwSendMsg(response, chatter);
                    return;
                case BotCommandId.SlowModeOff:
                    bool slowmode;
                    args.Insert(0, "off");
                    goto case BotCommandId.SlowMode;
                case BotCommandId.SlowMode:
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    slowmode = args.FirstOrDefault()?.ToLowerInvariant() != "off";
                    ChatActivity.IncCommandCounter(chatter, slowmode ? BotCommandId.SlowMode : BotCommandId.SlowModeOff);
                    _ = _twApi.ChangeChatSettings(CHANNEL_ID, CHANNEL_ID, s =>
                    {
                        s.SlowModeWaitTime = 3;
                        s.SlowMode = slowmode;
                        s.EmoteMode = slowmode;
                        s.FollowerMode = slowmode;
                        s.SubscriberMode = slowmode;
                    }).LogErr();
                    return;
                case BotCommandId.SetTitle:
                    if (argsStr.Length == 0)
                    {
                        TwSendMsg("Title: " + GetChannelInfo(CHANNEL).Title, chatter);
                        return;
                    }
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SetTitle);
                    _ = SetGameOrTitle.SetTitle(this, chatter, argsStr).LogErr();
                    return;
                case BotCommandId.SetGame:
                    if (argsStr.Length == 0)
                    {
                        TwSendMsg("Game: " + GetChannelInfo(CHANNEL).GameName, chatter);
                        return;
                    }
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SetGame);
                    _ = SetGameOrTitle.SetGame(this, chatter, argsStr).LogErr();
                    return;
                case BotCommandId.StartPoll:
                    {
                        if (args.Count == 0)
                        {
                            _ = Task.Run(async () =>
                            {
                                var poll = await _twApi.GetLatestPoll(CHANNEL_ID);
                                var totVotes = poll.Choices.Sum(x => x.Votes);
                                var oneOverTotVotes = 1f / totVotes;
                                var res = string.Join(" | ", poll.Choices.OrderByDescending(x => x.Votes)
                    .Select(x => $"{(x.Votes == 0 ? "0%" : (x.Votes * oneOverTotVotes).ToString("P2"))} {x.Title}"));
                                TwSendMsg($"[{(poll.Status == "ACTIVE" ? "current" : "last")} poll] {poll.Title} | {res}");
                            }).LogErr();
                            return;
                        }
                        if (chatter.userLevel < UserLevel.Moderator) return;
                        // !poll title | A | B | C
                        // !poll --5m title | A | B | C
                        // !poll --30s title | A | B | C
                        int durationSec = 120;
                        char durUnit;
                        string durStr = args.FirstOrDefault() ?? "";
                        int restIdx = 0; // in characters, not arguments
                        if (durStr.Length > 3 && durStr[0] == '-' && durStr[1] == '-' && char.IsAsciiDigit(durStr[2]) && (durUnit = char.ToLowerInvariant(durStr[^1])) is 'm' or 's' && int.TryParse(durStr[2..^1], out durationSec))
                        {
                            restIdx = durStr.Length;
                            durationSec *= durUnit switch
                            {
                                'm' => 60,
                                's' => 1,
                                _ => throw new ApplicationException("Sanity Check failed - we're mad!")
                            };
                            durationSec = Math.Max(15, Math.Min(1800, durationSec));
                        }
                        var titleAndOpts = argsStr[restIdx..].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (titleAndOpts.Length == 0)
                        {
                            TwSendMsg("Missing title and options, example usage: !poll title | one | two", chatter);
                            return;
                        }
                        if (titleAndOpts.Length < 3 || titleAndOpts.Length > 6)
                        {
                            TwSendMsg("Poll must have 2-5 options, example usage: !poll title | one | two", chatter);
                            return;
                        }
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.StartPoll);
                        _ = Task.Run(async () =>
                        {
                            var success = await _twApi.StartPoll(CHANNEL_ID, titleAndOpts[0], durationSec, titleAndOpts.Skip(1));
                            if (!success)
                                TwSendMsg("Failed to start poll D:");
                        }).LogErr();
                        return;
                    }
                case BotCommandId.EndPoll:
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.EndPoll);
                    _ = _twApi.EndCurrentPoll(CHANNEL_ID, true).LogErr();
                    return;
                case BotCommandId.DelPoll:
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.DelPoll);
                    _ = _twApi.EndCurrentPoll(CHANNEL_ID, false).LogErr();
                    return;
                case BotCommandId.AddCustomCommand:
                    {
                        if (chatter.userLevel < UserLevel.Moderator) return;
                        var cmdData = new CustomCommandData();
                        // scan for flags
                        int cmdIdx = 0;
                        for (; cmdIdx < args.Count; cmdIdx++)
                        {
                            var arg = args[cmdIdx];
                            if (arg[0] != '-')
                                break;

                            if (arg.Length == 1)
                                continue; // forgiving a situtation like !addcmd - - -ul=mod !hi hi
                            int j = 1;
                            if (arg.Length >= 2 && arg[j] is 'u' or 'U' && arg[j + 1] is 'l' or 'L')
                            {
                                j += 2;
                                if (j + 1 >= arg.Length || arg[j] != '=')
                                {
                                    TwSendMsg("-ul expects a value without spaces, e.g: -ul=mod", chatter);
                                    return;
                                }
                                j++; // =
                                var ul = arg[j..].ToLowerInvariant();
                                switch (ul)
                                {
                                    case "all" or "everyone" or "normal" or "normals" or "viewer" or "viewers": cmdData.ReqLevel = UserLevel.Normal; break;
                                    case "sub" or "subs" or "subscriber" or "subscribers": cmdData.ReqLevel = UserLevel.Subscriber; break;
                                    case "vip" or "vips" or "twitch_vip" or "twitch_vips": cmdData.ReqLevel = UserLevel.VIP; break;
                                    case "mod" or "mods" or "moderator" or "moderators": cmdData.ReqLevel = UserLevel.Moderator; break;
                                    case "owner" or "streamer" or "broadcaster": cmdData.ReqLevel = UserLevel.Streamer; break;
                                    default:
                                        TwSendMsg($"Invalid UserLevel '{ul}', valid values: all/sub/vip/mod/owner", chatter);
                                        return;
                                }
                            }
                            else if (arg.Length >= 2 && arg[j] is 'c' or 'C' && arg[j + 1] is 'd' or 'D')
                            {
                                j += 2;
                                if (j + 1 >= arg.Length || arg[j] != '=')
                                {
                                    TwSendMsg("-cd expects a value without spaces, e.g: -cd=3s", chatter);
                                    return;
                                }
                                j++; // =
                                // TODO -cd cool down
                            }
                            else
                            {
                                TwSendMsg($"Invalid flag {arg}", chatter);
                                return;
                            }
                        }
                        string customCmd = null;
                        if (cmdIdx < args.Count)
                        {
                            customCmd = args[cmdIdx];
                            if (cmdIdx + 1 < args.Count)
                                cmdData.Response = string.Join(' ', args.Skip(cmdIdx + 1));
                        }
                        if (customCmd == null || cmdData.Response == null)
                        {
                            TwSendMsg($"{(cmdData.Response == null ? "Missing response, e" : "E")}xample usage: {CMD_PREFIX}{cmd} {CMD_PREFIX}hi Hello $(user)", chatter);
                            return;
                        }
                        if (customCmd.StartsWith(CMD_PREFIX))
                            customCmd = customCmd[CMD_PREFIX.Length..];
                        if (!char.IsAsciiLetterOrDigit(customCmd.FirstOrDefault()))
                        {
                            TwSendMsg("Custom command must begin with letter or digit", chatter);
                            return;
                        }
                        var success = AddCustomCommand(customCmd, cmdData, chatter);
                        if (success)
                            ChatActivity.IncCommandCounter(chatter, BotCommandId.AddCustomCommand);
                        TwSendMsg(success ? $"Added command {customCmd} SeemsGood" : $"The command {customCmd} already exists", chatter);
                        return;
                    }
                case BotCommandId.DelCustomCommand:
                    {
                        if (chatter.userLevel < UserLevel.Moderator) return;
                        if (args.Count < 1) return;
                        string customCmd = args[0];
                        if (customCmd.StartsWith(CMD_PREFIX))
                            customCmd = customCmd[CMD_PREFIX.Length..];
                        if (!char.IsAsciiLetterOrDigit(customCmd.FirstOrDefault()))
                        {
                            TwSendMsg("Custom command must begin with letter or digit", chatter);
                            return;
                        }
                        var success = DelCustomCommand(customCmd, chatter);
                        if (success)
                            ChatActivity.IncCommandCounter(chatter, BotCommandId.DelCustomCommand);
                        TwSendMsg(success ? $"Deleted command {customCmd} SeemsGood" : $"The command {customCmd} does not exist", chatter);
                        return;
                    }
                case BotCommandId.EditCustomCommand:
                    {
                        if (chatter.userLevel < UserLevel.Moderator) return;
                        if (args.Count < 1) return;
                        if (args.Count < 2)
                        {
                            TwSendMsg($"Missing response, example usage: {CMD_PREFIX}{cmd} {CMD_PREFIX}hi Hello $(user)", chatter);
                            return;
                        }
                        string customCmd = args[0];
                        if (customCmd.StartsWith(CMD_PREFIX))
                            customCmd = customCmd[CMD_PREFIX.Length..];
                        if (!char.IsAsciiLetterOrDigit(customCmd.FirstOrDefault()))
                        {
                            TwSendMsg("Custom command must begin with letter or digit", chatter);
                            return;
                        }
                        var success = EditCustomCommand(customCmd, argsStr[(args[0].Length + 1)..], chatter);
                        if (success)
                            ChatActivity.IncCommandCounter(chatter, BotCommandId.EditCustomCommand);
                        TwSendMsg(success ? $"Edited command {customCmd} SeemsGood" : $"The command {customCmd} does not exists", chatter);
                        return;
                    }
                case BotCommandId.ShowCustomCommand:
                    {
                        if (chatter.userLevel < UserLevel.Moderator) return;
                        if (args.Count < 1) return;
                        string customCmd = args[0];
                        if (customCmd.StartsWith(CMD_PREFIX))
                            customCmd = customCmd[CMD_PREFIX.Length..];
                        CustomCommandData? cc = null;
                        lock (_customCommandsLock)
                        {
                            if (_customCommands.TryGetValue(customCmd.ToLowerInvariant(), out var ccc))
                                cc = ccc;
                        }
                        TwSendMsg(cc == null ? $"command {customCmd} not found" : $"{CMD_PREFIX}{customCmd} :: {cc.Value.Response}");
                        return;
                    }
                case BotCommandId.ShowBrb:
                    if (_isBrbEnabled || chatter.userLevel != UserLevel.Streamer) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.ShowBrb);
                    DoShowBrb();
                    return;

                // COMMON
                case BotCommandId.SearchGame:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SearchGame);
                    _ = SetGameOrTitle.SearchGame(this, chatter, argsStr).LogErr();
                    return;
                case BotCommandId.GetCmdCounter:
                    {
                        if (args.Count == 0) return;
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.GetCmdCounter);
                        string targetCmdName = args[0];
                        if (targetCmdName.StartsWith(CMD_PREFIX))
                            targetCmdName = targetCmdName[CMD_PREFIX.Length..];
                        BotCommandId targetCid = ParseBuiltinCommandId(targetCmdName.ToLowerInvariant());
                        if (targetCid < 0)
                        {
                            TwSendMsg($"No counter for {targetCmdName}", chatter);
                            return;
                        }
                        Chatter targetChatter = chatter;
                        if (args.Count > 1)
                        {
                            var targetName = args[1].CleanUsername();
                            targetChatter = ChatterDataMgr.GetOrNull(targetName.CanonicalUsername());
                            if (targetChatter == null)
                            {
                                TwSendMsg("Who the fuck is " + targetName, chatter);
                                return;
                            }
                        }
                        var counter = targetChatter.GetCmdCounter(targetCid);
                        var whoStr = chatter == targetChatter ? "You" : "The user " + targetChatter.DisplayName;
                        TwSendMsg($"{whoStr} used the command '{targetCmdName}' {counter} time{(counter == 1 ? "" : "s")}", chatter);
                        return;
                    }
                case BotCommandId.FollowAge:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.FollowAge);
                    _ = Task.Run(async () =>
                    {
                        string tagUser = chatter.DisplayName;
                        string uid = chatter.uid;
                        if (args.Count > 0)
                        {
                            tagUser = args[0].CleanUsername();
                            uid = GetUserIdOrNull(tagUser);
                            if (uid == null)
                            {
                                TwSendMsg("User not found: " + tagUser, chatter);
                                return;
                            }
                        }
                        var res = await _twApi_More.GetAllFollowers(uid, CHANNEL_ID).ConfigureAwait(true);
                        if (res.Count == 0)
                            TwSendMsg(tagUser + " is not following D:");
                        else
                        {
                            var dur = DateTime.UtcNow.Subtract(DateTime.Parse(res[0].followed_at));
                            TwSendMsg(tagUser + " is following for " + dur.Humanize(4, true, maxUnit: Humanizer.Localisation.TimeUnit.Year, minUnit: Humanizer.Localisation.TimeUnit.Minute));
                        }
                    }).LogErr();
                    return;
                case BotCommandId.WatchTime:
                    {
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.WatchTime);
                        Chatter target = chatter;
                        if (args.Count > 0)
                        {
                            var name = args[0].CleanUsername();
                            target = ChatterDataMgr.GetOrNull(name.CanonicalUsername());
                            if (target == null)
                            {
                                TwSendMsg("Who the fuck is " + name, chatter);
                                return;
                            }
                        }
                        TwSendMsg(target.DisplayName + " has a watchtime of " + TimeSpan.FromMilliseconds(target.watchtime_ms).Humanize(4, true, maxUnit: Humanizer.Localisation.TimeUnit.Year, minUnit: Humanizer.Localisation.TimeUnit.Minute));
                        return;
                    }
                case BotCommandId.GetRedeemCounter:
                    TwSendMsg("Currently broken Sadge");
                    return; // TODO for !redeems TwitchApi broken? I'm dumb? maybe one day get back to it
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.GetRedeemCounter);
                    if (args.Count == 0)
                    {
                        TwSendMsg("Specify a reward title", chatter);
                        return;
                    }
                    _ = Task.Run(async () =>
                    {
                        var customRewards = (await _twApi.Helix.ChannelPoints.GetCustomRewardAsync(CHANNEL_ID).ConfigureAwait(true)).Data;
                        var reward =
                   customRewards.FirstOrDefault(x => x.Title.Equals(argsStr, StringComparison.InvariantCultureIgnoreCase))
                ?? customRewards.FirstOrDefault(x => x.Title.Contains(argsStr, StringComparison.InvariantCultureIgnoreCase));
                        if (reward == null)
                        {
                            TwSendMsg("No such reward", chatter);
                            return;
                        }
                        int count = _redeemCounts.TryGetValue(_rewardsKey(chatter.uid, reward.Id), out int v) ? v : 0;
                        TwSendMsg($"You have redeemed '{reward.Title}' a total of {count} time{(count == 1 ? "" : "s")}", chatter);
                    }).LogErr();
                    return;
                case BotCommandId.SongRequest_Request:
                    if (args.Count == 0)
                    {
                        TwSendMsg("Give me something to search on YouTube. I accept a youtube link or video id", chatter);
                        return;
                    }
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SongRequest_Request);
                    SongRequest.RequestSong(argsStr, chatter);
                    return;
                case BotCommandId.SongRequest_SetVolumeMax:
                    if (chatter.userLevel != UserLevel.Streamer) return;
                    if (args.Count == 0) return;
                    if (!int.TryParse(args.FirstOrDefault(), out int maxVol)) return;
                    _ = Task.Run(async () =>
                    {
                        maxVol = await SongRequest._SetMaxVolume(maxVol);
                        TwSendMsg("Max volume set to " + maxVol, chatter);
                    }).LogErr();
                    return;
                case BotCommandId.SongRequest_Next:
                    if (chatter.userLevel < UserLevel.VIP) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SongRequest_Next);
                    SongRequest.Next();
                    return;
                case BotCommandId.SongRequest_GetPrev:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SongRequest_GetPrev);
                    SongRequest.GetPrevSong(chatter);
                    return;
                case BotCommandId.SongRequest_GetCurr:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SongRequest_GetCurr);
                    SongRequest.GetCurrSong(chatter);
                    return;
                // TODO remove command and add to UI
                case BotCommandId.SongRequest_ShufflePlaylist:
                    if (chatter.userLevel != UserLevel.Streamer) return;
                    SongRequest.ShufflePlaylist();
                    return;
                case BotCommandId.SongRequest_WrongSong:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SongRequest_WrongSong);
                    SongRequest.WrongSong(chatter);
                    return;
                case BotCommandId.SongRequest_Volume:
                    if (chatter.userLevel < UserLevel.Moderator) return;
                    if (args.Count == 0 || !int.TryParse(args.FirstOrDefault(), out int volume))
                        SongRequest.GetVolume(chatter);
                    else
                    {
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.SongRequest_Volume);
                        SongRequest.SetVolume(volume, chatter);
                    }
                    return;
                case BotCommandId.Queue_Curr:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Queue_Curr);
                    ViewersQueue.Curr(this, chatter);
                    return;
                case BotCommandId.Queue_Next:
                    if (chatter.userLevel != UserLevel.Streamer) return;
                    ViewersQueue.Next(this);
                    _ = e.ChatMessage.Hide(this);
                    return;
                case BotCommandId.Queue_All:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Queue_All);
                    ViewersQueue.All(this, chatter);
                    return;
                case BotCommandId.Queue_Clear:
                    if (chatter.userLevel != UserLevel.Streamer) return;
                    ViewersQueue.Clear(this);
                    _ = e.ChatMessage.Hide(this);
                    return;
                case BotCommandId.Queue_Join:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Queue_Join);
                    ViewersQueue.Join(this, chatter, args.FirstOrDefault());
                    return;
                case BotCommandId.Queue_Leave:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Queue_Leave);
                    ViewersQueue.Leave(this, chatter);
                    return;
                case BotCommandId.Queue_Close:
                    if (chatter.userLevel != UserLevel.Streamer) return;
                    ViewersQueue.Close(this);
                    _ = e.ChatMessage.Hide(this);
                    return;
                case BotCommandId.Queue_Open:
                    if (chatter.userLevel != UserLevel.Streamer) return;
                    ViewersQueue.Open(this);
                    _ = e.ChatMessage.Hide(this);
                    return;
                case BotCommandId.SneakyJapan:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.SneakyJapan);
                    SneakyJapan.Japan(chatter);
                    return;
                case BotCommandId.SneakyJapan_Stats:
                    {
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.SneakyJapan_Stats);
                        var targetChatter = chatter;
                        if (args.Count > 0)
                        {
                            var targetName = args[0].CleanUsername();
                            targetChatter = ChatterDataMgr.GetOrNull(targetName.CanonicalUsername());
                            if (targetChatter == null)
                            {
                                TwSendMsg("Who the fuck is " + targetName, chatter);
                                return;
                            }
                        }
                        SneakyJapan.JapanStats(targetChatter);
                        return;
                    }
                case BotCommandId.SneakyJapan_NewGamePlus:
                    SneakyJapan.Do_NewGamePlus_Unchecked(chatter, args.FirstOrDefault());
                    return;
                case BotCommandId.SneakyJapan_Leaderboard:
                    SneakyJapan.Do_Leaderboard();
                    return;
                case BotCommandId.Celsius2Fahrenheit:
                    {
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.Celsius2Fahrenheit);
                        var deg = float.TryParse(args.Count > 0 ? args[0] : "", out float x) ? x : 0;
                        TwSendMsg($"{deg}°C = {deg * 9.0f / 5 + 32:F1}°F", chatter);
                        return;
                    }
                case BotCommandId.Fahrenheit2Celsius:
                    {
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.Fahrenheit2Celsius);
                        var deg = float.TryParse(args.Count > 0 ? args[0] : "", out float x) ? x : 0;
                        TwSendMsg($"{deg}°F = {(deg - 32) * 5.0f / 9:F1}°C", chatter);
                        return;
                    }
                case BotCommandId.CoinFlip:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.CoinFlip);
                    TwSendMsg($"Coin flip: {(Rand.R.Next(2) == 0 ? "heads" : "tails")}");
                    return;
                case BotCommandId.DiceRoll:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.DiceRoll);
                    int die;
                    if (!int.TryParse(args.FirstOrDefault(), out die) || die < 1) die = 20;
                    int roll = Rand.R.Next(die) + 1;
                    TwSendMsg($"Rolling d{die}... you get {roll}!{(roll == 20 ? " Kreygasm" : "")}");
                    return;
                case BotCommandId.Quote_Get:
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Quote_Get);
                    if (int.TryParse(args.FirstOrDefault(), out int wisIdx))
                        TwSendMsg(Quotes.GetQuote(wisIdx - 1));
                    else if (args.Count > 0)
                    {
                        var query = argsStr;
                        var q = Quotes.FindQuote(query);
                        TwSendMsg(q ?? "No quote found with the search term: " + query);
                    }
                    else
                        TwSendMsg(Quotes.GetRandom());
                    return;
                case BotCommandId.Quote_Add:
                    if (chatter.userLevel < UserLevel.VIP) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Quote_Add);
                    int newWisdomIdx = Quotes.AddQuote(argsStr);
                    Quotes.Save();
                    TwSendMsg("Quote " + newWisdomIdx + " added");
                    return;
                case BotCommandId.Quote_Del:
                    if (chatter.userLevel < UserLevel.VIP) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.Quote_Del);
                    if (!int.TryParse(args.FirstOrDefault(), out int delIdx))
                        return;
                    if (!Quotes.DelQuote(delIdx - 1))
                        TwSendMsg("There is no quote " + delIdx);
                    else
                    {
                        Quotes.Save();
                        TwSendMsg("Quote " + delIdx + " deleted");
                    }
                    return;
                case BotCommandId.LearnHiragana:
                    if (chatter.userLevel < UserLevel.VIP) return;
                    ChatActivity.IncCommandCounter(chatter, BotCommandId.LearnHiragana);
                    if (args.Count == 0 || !(args[0] is "on" or "off"))
                    {
                        TwSendMsg("Expected a parameter 'on' or 'off'", chatter);
                        return;
                    }
                    LearnHiragana._task.Enabled = args[0] == "on";
                    TwSendMsg("SeemsGood", chatter);
                    return;
                case BotCommandId.GetChessRatings:
                    {
                        ChatActivity.IncCommandCounter(chatter, BotCommandId.GetChessRatings);
                        var targetName = args.FirstOrDefault()?.CleanUsername() ?? chatter.DisplayName;
                        _ = Task.Run(async () =>
                        {
                            bool found = false;
                            try
                            {
                                using var http = new HttpClient();
                                var lichess = JToken.Parse(await http.GetStringAsync("https://lichess.org/api/user/" + targetName));
                                lichess = lichess["perfs"];
                                if (lichess != null)
                                {
                                    int bullet = lichess["bullet"]?["rating"]?.Value<int>() ?? 0;
                                    int blitz = lichess["blitz"]?["rating"]?.Value<int>() ?? 0;
                                    int rapid = lichess["rapid"]?["rating"]?.Value<int>() ?? 0;
                                    int classical = lichess["classical"]?["rating"]?.Value<int>() ?? 0;
                                    int daily = (lichess["daily"] ?? lichess["correspondence"])?["rating"]?.Value<int>() ?? 0;
                                    int puzzle = lichess["puzzle"]?["rating"]?.Value<int>() ?? 0;
                                    var sb = new StringBuilder();
                                    if (bullet != 0) sb.Append("Bullet ").Append(bullet).Append(" | ");
                                    if (blitz != 0) sb.Append("Blitz ").Append(blitz).Append(" | ");
                                    if (rapid != 0) sb.Append("Rapid ").Append(rapid).Append(" | ");
                                    if (classical != 0) sb.Append("Classical ").Append(classical).Append(" | ");
                                    if (daily != 0) sb.Append("Daily ").Append(daily).Append(" | ");
                                    if (puzzle != 0) sb.Append("Puzzle ").Append(puzzle).Append(" | ");
                                    if (sb.Length != 0)
                                    {
                                        found = true;
                                        TwSendMsg(targetName + " (lichess) " + sb.ToString()[..^3]);
                                    }
                                }
                            }
                            catch { }
                            try
                            {
                                using var http = new HttpClient();
                                // chesscum wants useragent
                                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246");
                                var chesscum = JToken.Parse(await http.GetStringAsync("https://api.chess.com/pub/player/" + targetName + "/stats"));
                                if (chesscum != null)
                                {
                                    int bullet = chesscum["chess_bullet"]?["last"]?["rating"]?.Value<int>() ?? 0;
                                    int blitz = chesscum["chess_blitz"]?["last"]?["rating"]?.Value<int>() ?? 0;
                                    int rapid = chesscum["chess_rapid"]?["last"]?["rating"]?.Value<int>() ?? 0;
                                    int daily = chesscum["chess_daily"]?["last"]?["rating"]?.Value<int>() ?? 0;
                                    int puzzle = (chesscum["tactics"]?["last"] ?? chesscum["tactics"]?["highest"])?["rating"]?.Value<int>() ?? 0;
                                    var sb = new StringBuilder();
                                    if (bullet != 0) sb.Append("Bullet ").Append(bullet).Append(" | ");
                                    if (blitz != 0) sb.Append("Blitz ").Append(blitz).Append(" | ");
                                    if (rapid != 0) sb.Append("Rapid ").Append(rapid).Append(" | ");
                                    if (daily != 0) sb.Append("Daily ").Append(daily).Append(" | ");
                                    if (puzzle != 0) sb.Append("Puzzle ").Append(puzzle).Append(" | ");
                                    if (sb.Length != 0)
                                    {
                                        found = true;
                                        TwSendMsg(targetName + " (chesscom) " + sb.ToString()[..^3]);
                                    }
                                }
                            }
                            catch { }
                            if (!found)
                                TwSendMsg(targetName + " was not found on lichess or chesscom");
                        }).LogErr();
                        return;
                    }
            } // switch (built-in commands)

            // custom commands
            {
                CustomCommandData cc;
                lock (_customCommandsLock)
                {
                    if (!_customCommands.TryGetValue(cmd, out cc))
                        return;
                    if (chatter.userLevel < cc.ReqLevel)
                        return;
                    cc.TotalTimesUsed++;
                    _customCommands[cmd] = cc;
                    _saveCustomCommands_noLock();
                }
                var formatted = formatResponseText(cc.Response, e.ChatMessage, chatter, args, argsStr, cc, out string error);
                TwSendMsg(formatted ?? ($"@{chatter.DisplayName} {error}"));
            }
        }

        public void DoShowBrb()
        {
            _isBrbEnabled = true;
            MainForm.Get.BeginInvoke(() => MainForm.Get.Icon = Resources.brb);

            string brbFile = null;
            if (!string.IsNullOrWhiteSpace(USER_DATA_FOLDER))
            {
                brbFile = Path.Combine(USER_DATA_FOLDER, "obs_labels\\brb.txt");
                File.WriteAllText(brbFile, "BRB");
            }
            _obs?.SetInputMute("Audio Input Capture", true);
            
            _ = Task.Run(async () =>
            {
                var p = Cursor.Position;
                do { await Task.Delay(1000); } while (Cursor.Position == p);
                
                MainForm.Get.BeginInvoke(() => MainForm.Get.Icon = Resources.s_logo);
                _isBrbEnabled = false;

                if (brbFile != null)
                    File.WriteAllText(brbFile, "");
                _obs?.SetInputMute("Audio Input Capture", false);
            });
        }

        #region Custom Commands

        string _customCommandsFile;
        Dictionary<string, CustomCommandData> _customCommands = new();
        readonly object _customCommandsLock = new();
        struct CustomCommandData
        {
            public string Response;
            public int TotalTimesUsed;
            public UserLevel ReqLevel;
            //public int CooldownSecs; // TODO
        }
        private string GetAllCustomCommands()
        {
            lock (_customCommandsLock)
            {
                return string.Join(' ', _customCommands.Select(x => x.Key));
            }
        }
        private void LoadCustomCommands(string filePath)
        {
            _customCommandsFile = filePath;
            if (string.IsNullOrWhiteSpace(_customCommandsFile))
                return;
            lock (_customCommandsLock)
            {
                try
                {
                    _customCommands = File.ReadAllText(_customCommandsFile).FromJson<Dictionary<string, CustomCommandData>>();
                }
                catch { } // TODO most empty catches all around should at least log the error probably
            }
        }
        private void _saveCustomCommands_noLock()
        {
#if DEBUG
      return;
#endif
            if (string.IsNullOrWhiteSpace(_customCommandsFile))
                return;
            try
            {
                File.WriteAllText(_customCommandsFile, _customCommands.ToJson());
            }
            catch { }
        }
        private bool DelCustomCommand(string command, Chatter mod)
        {
            command = command.ToLowerInvariant();
            CustomCommandData cc;
            lock (_customCommandsLock)
            {
                if (!_customCommands.TryGetValue(command, out cc))
                    return false;
                _customCommands.Remove(command);
                _saveCustomCommands_noLock();
            }
            Log($"[EVENT] {mod.DisplayName} deleted custom command {command}: {cc.ToJson()}");
            return true;
        }
        private bool AddCustomCommand(string command, CustomCommandData cc, Chatter mod)
        {
            command = command.ToLowerInvariant();
            lock (_customCommandsLock)
            {
                if (_customCommands.ContainsKey(command))
                    return false;
                _customCommands.Add(command, cc);
                _saveCustomCommands_noLock();
            }
            Log($"[EVENT] {mod.DisplayName} added custom command {command}: {cc.ToJson()}");
            return true;
        }
        private bool EditCustomCommand(string command, string response, Chatter mod)
        {
            command = command.ToLowerInvariant();
            string oldResponse;
            lock (_customCommandsLock)
            {
                if (!_customCommands.TryGetValue(command, out var cc))
                    return false;
                oldResponse = cc.Response;
                cc.Response = response;
                _customCommands[command] = cc;
                _saveCustomCommands_noLock();
            }
            Log($"[EVENT] {mod.DisplayName} edited custom command {command}\n  old: {oldResponse}\n  new: {response}");
            return true;
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////////

        // TODO make a proper DSL for this shit (subset of js for easy learning/copypastas)
        /// <summary>
        /// Supports:
        ///   - $(name) $(input) $(arg0) $(arg1) etc
        ///   - $(arg0?)
        ///   - $(arg0 ? fallback value)
        ///   - $(fetch URL) $(res) $(res.data.name)
        ///   - $(fetch:w URL) $(w) $(w.data.name)
        /// Notes:
        ///   - Fetches must preceed the actual message
        ///   - Fetches res don't currently support fallback values
        ///   - Any particle without '?' is considered a "required" input
        /// </summary>
        static readonly Regex rgxTwFormatParticle = new(@"\$\([^)]+\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly char[] newLineChars = { '\r', '\n' };
        string formatResponseText(string text, ChatMessage msgData, Chatter chatter, List<string> args, string argsStr, CustomCommandData? customCmd, out string error, Dictionary<string, JToken> fetchResults = null)
        {
            // fetches must come at the beginning, and they accumulate results into $(res) or a $(namedVar)
            // $(fetch:w www.example.com/q=$(input)) The weather at $(w.City) is $(w.WeatherText)
            // $(fetch:0 url) $(fetch url/q=$(0.Name)) $(0.Name) is $(res.Coolness)% cool
            // TODO test advanced chaining and naming ^
            if (fetchResults != null && text.StartsWith("$(fetch", StringComparison.InvariantCultureIgnoreCase))
                throw new ApplicationException("Unexpected format fetch within a fetch");
            fetchResults = new Dictionary<string, JToken>();
            string lastFetchResultName = null;
            while (text.StartsWith("$(fetch", StringComparison.InvariantCultureIgnoreCase))
            {
                const string BAD_SYNTAX_MSG = "[[ Bad fetch syntax ]] Example usage: $(fetch url) Results are $res.description";
                int i = "$(fetch".Length;
                void SkipSpace() { while (i < text.Length && text[i] == ' ') i++; }
                SkipSpace();
                string varName = "res";
                // read fetch NAME
                if (i < text.Length && text[i] == ':')
                {
                    int j = ++i;
                    while (j < text.Length && text[j] != ' ' && text[j] != ')')
                    {
                        var c = text[j++];
                        if (c != '_' && (c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9'))
                        {
                            error = "#1" + BAD_SYNTAX_MSG;
                            return null;
                        }
                    }
                    varName = text[i..j];
                    i = j;
                    SkipSpace();
                }
                // read fetch URL
                int urlStart = i;
                while (i < text.Length && text[i] != ')')
                {
                    if (text[i] == '$')
                    {
                        // skip over a basic $(x.y.z) particle
                        i++;
                        if (i < text.Length && text[i] != '(')
                        {
                            error = "[[ Bad fetch syntax ]] Character '$' in the url should be encoded as '%24'";
                            return null;
                        }
                        i++;
                        // wait for closing ) but make sure simple variable
                        while (i < text.Length && text[i] != ')')
                        {
                            if (text[i] == '$')
                            {
                                error = "[[ Bad fetch syntax ]] The placeholder $() in the URL must be a basic variable name";
                                return null;
                            }
                            i++;
                        }
                    }
                    i++;
                }
                if (text[i] != ')')
                {
                    error = "#2" + BAD_SYNTAX_MSG;
                    return null;
                }
                var url = text[urlStart..i].TrimEnd();
                if (string.IsNullOrWhiteSpace(url) || url[0] == '$') // makes sure url isn't $fetch itself, or smth stoopid
                {
                    error = "#3" + BAD_SYNTAX_MSG;
                    return null;
                }
                i++;
                SkipSpace();
                // finished parsing the fetch
                text = text[i..];

                // FETCH
                url = formatResponseText(url, msgData, chatter, args, argsStr, null, out string innerError, fetchResults);
                if (innerError != null)
                {
                    error = "[[ Failed fetch ]] " + innerError;
                    return null;
                }
                string json;
                using var http = new HttpClient();
                try
                {
                    json = http.GetStringAsync(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log("[[ Failed fetch ]] " + ex);
                    error = "[ Failed to reach url ]";
                    return null;
                }
                JToken jRes;
                if (string.IsNullOrWhiteSpace(json))
                    jRes = new JValue("<empty>");
                else
                {
                    try
                    {
                        jRes = JToken.Parse(json);
                    }
                    catch
                    {
                        jRes = new JValue(json);
                    }
                }
                fetchResults[varName] = jRes;
                lastFetchResultName = varName;
            }

            // after removing $(fetch url) we might have empty msg
            if (string.IsNullOrWhiteSpace(text))
            {
                error = null;
                if (lastFetchResultName != null)
                    return fetchResults[lastFetchResultName].ToString();
                return "";
            }

            // format particles
            string missingParticles = "";
            const string MISSING_PARTICLE_SEP = ", ";
            string formatted = rgxTwFormatParticle.Replace(text, m =>
            {
                var particle = m.Value[2..^1].Trim(); // $(name)
                                                      // first try find fetch result under that name (case sensitive)
                var particleMembers = particle.Split('.');
                if (fetchResults.TryGetValue(particleMembers[0], out var res))
                {
                    // member access $(res.Results.0.Name)
                    for (int j = 1; j < particleMembers.Length; j++)
                    {
                        var member = particleMembers[j];
                        // some builtin functions
                        switch (member)
                        {
                            case "__splitLines":
                                {
                                    if (res.Type != JTokenType.String)
                                        return $"<cant read {string.Join('.', particleMembers.Take(j + 1))} on a non-string>";
                                    JArray jarr = new();
                                    foreach (var line in res.ToString().Split(newLineChars, StringSplitOptions.RemoveEmptyEntries))
                                        jarr.Add(new JValue(line));
                                    res = jarr;
                                    continue;
                                }
                            case "__randItem":
                                {
                                    if (res.Type != JTokenType.Array)
                                        return $"<cant read {string.Join('.', particleMembers.Take(j + 1))} on a non-array>";
                                    var jarr = (JArray)res;
                                    res = jarr.HasValues ? jarr[Rand.R.Next(jarr.Count)] : new JValue("");
                                    continue;
                                }
                        }
                        if (!res.HasValues)
                            return $"<cant read {string.Join('.', particleMembers.Take(j + 1))}>";
                        try
                        {
                            res = member.Length != 0 && (member[0] is >= '0' and <= '9') && int.TryParse(member, out int k)
                        ? res[k] : res[member];
                        }
                        catch
                        {
                            res = null;
                        }
                        if (res == null)
                            break; // this makes $res.foo.bar null-coallescing
                    }
                    return res?.ToString() ?? "null";
                }

                // built-in particles (case insensitive)
                int qi = particle.IndexOf('?');
                string fallback = null;
                if (qi >= 0)
                {
                    // $(arg0 ? Some Fallback)
                    fallback = particle[(qi + 1)..].TrimStart();
                    particle = particle[..qi].TrimEnd();
                }
                particle = particle.ToLowerInvariant();
                string tmp;
                ChannelInformation _targetInfo = null;
                bool _targetInfoIsFetched = false;
                ChannelInformation getTargetInfo()
                {
                    if (_targetInfoIsFetched)
                        return _targetInfo;
                    _targetInfoIsFetched = true;
                    return (_targetInfo = GetChannelInfo(args.FirstOrDefault()));
                }
                ChannelInformation _chatterInfo = null;
                bool _chatterInfoIsFetched = false;
                ChannelInformation getChatterInfo()
                {
                    if (_chatterInfoIsFetched)
                        return _chatterInfo;
                    _chatterInfoIsFetched = true;
                    return (_chatterInfo = GetChannelInfo(chatter.name));
                }
                var rep = particle switch
                {
                    "input" or "args" => argsStr,
                    "query" or "querystring" => argsStr == null ? "" : HttpUtility.UrlEncode(argsStr),
                    "arg0" or "0" => args.Count > 0 ? args[0] : "",
                    "arg1" or "1" => args.Count > 1 ? args[1] : "",
                    "arg2" or "2" => args.Count > 2 ? args[2] : "",
                    "arg3" or "3" => args.Count > 3 ? args[3] : "",
                    "arg4" or "4" => args.Count > 4 ? args[4] : "",
                    "arg5" or "5" => args.Count > 5 ? args[5] : "",
                    "arg6" or "6" => args.Count > 6 ? args[6] : "",
                    "arg7" or "7" => args.Count > 7 ? args[7] : "",
                    "arg8" or "8" => args.Count > 8 ? args[8] : "",
                    "arg9" or "9" => args.Count > 9 ? args[9] : "",
                    "channel" or "streamer" => CHANNEL,
                    "channelid" or "channel_id" or "channel.id" or "streamerid" or "streamer_id" or "streamer.id" => CHANNEL_ID,

                    "targetorself_name" or "targetorself.name" => args.Count == 0 ? chatter.DisplayName : ChatterDataMgr.GetOrNull(args[0].CanonicalUsername())?.DisplayName ?? args[0].CleanUsername(),
                    "targetorself_id" or "targetorself.id" => args.Count == 0 ? chatter.uid : GetUserIdOrNull(args[0]),
                    "targetorself_game" or "targetorself.game" => (args.Count == 0 ? getChatterInfo() : getTargetInfo())?.GameName ?? "<not found>",
                    "targetorself_title" or "targetorself.title" => (args.Count == 0 ? getChatterInfo() : getTargetInfo())?.Title ?? "<not found>",
                    "targetorself_level" or "targetorself.level" => args.Count == 0 ? chatter.userLevel.ToString() : (ChatterDataMgr.GetOrNull(args[0].CanonicalUsername())?.userLevel ?? default).ToString(),

                    "username" or "user_name" or "user.name" or "name" or "user" => chatter.DisplayName,
                    "userid" or "user_id" or "user.id" => chatter.uid,
                    "usergame" or "user_game" or "user.game" => getChatterInfo()?.GameName ?? "<not found>",
                    "usertitle" or "user_title" or "user.title" => getChatterInfo()?.Title ?? "<not found>",
                    "userlevel" or "user_level" or "user.level" => chatter.userLevel.ToString(),

                    "target" or "targetname" or "target.name" => ChatterDataMgr.GetOrNull(args.FirstOrDefault()?.CanonicalUsername())?.DisplayName ?? args.FirstOrDefault()?.CleanUsername() ?? "",
                    "targetid" or "target_id" or "target.id" => GetUserIdOrNull(args.FirstOrDefault()) ?? "<not found>",
                    "targetlevel" or "target_level" or "target.level" => string.IsNullOrWhiteSpace(tmp = args.FirstOrDefault()?.CanonicalUsername()) ? "" : (ChatterDataMgr.GetOrNull(tmp)?.userLevel ?? default).ToString(),
                    "targetgame" or "target_game" or "target.game" => getTargetInfo()?.GameName ?? "<not found>",
                    "targettitle" or "target_title" or "target.title" => getTargetInfo()?.Title ?? "<not found>",

                    "randomchatter" => ChatActivity.RandomChatter(),
                    "time" => DateTime.Now.ToShortTimeString(),
                    "count" or "counter" => (customCmd?.TotalTimesUsed).ToString() ?? "",
                    "fetch" => "<invalid fetch usage>",
                    _ => m.Value, // original particle text e.g. '$(unreal.42)'
                };
                // shouldn't return an actual null here in regex replace
                if (string.IsNullOrWhiteSpace(rep))
                {
                    if (fallback == null)
                        missingParticles += particle + MISSING_PARTICLE_SEP;
                    rep = fallback ?? "null";
                }
                return rep;
            });

            if (missingParticles.Length > 0)
            {
                error = $"Missing {missingParticles[..^MISSING_PARTICLE_SEP.Length]}";
                return null;
            }
            error = null;
            return formatted;
        }

    }
}
