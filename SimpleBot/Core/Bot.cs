using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Chat;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
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
#if DEBUG
    public readonly string CMD_PREFIX = "!" + Settings.Default.CommandsPrefix;
#else
    public readonly string CMD_PREFIX = Settings.Default.CommandsPrefix;
#endif

    #region UI events

    public event EventHandler UpdatedTwitchConnected;
    public event EventHandler UpdatedOBSConnected;

    #endregion

    public TwitchClient _tw;
    public TwitchAPI _twApi;
    public TwitchApi_More _twApi_More;
    public OBSWebsocket _obs;

    public string CHANNEL_ID { get; private set; }
    public string BOT_ID { get; private set; }
    readonly JoinedChannel _twJC; // fake object with no data for quick TwSendMessage
    readonly ConcurrentDictionary<string, int> _redeemCounts = new(); // value = count, key = "user_id;reward_id"
    static string _rewardsKey(string userId, string rewardId) => userId + ";" + rewardId;
    
    public Bot()
    {
      _twJC = new JoinedChannel(CHANNEL);

      // Load persistent data
      if (string.IsNullOrWhiteSpace(USER_DATA_FOLDER))
        return;
      
      Directory.CreateDirectory(Path.Combine(USER_DATA_FOLDER, "data"));
      ChatterDataMgr.Load(Path.Combine(USER_DATA_FOLDER, "data\\chatters_data.txt"));
      ChatActivity.Load(Path.Combine(USER_DATA_FOLDER, "data\\chat_activity.txt")); // has IgnoredBotNames
      Quotes.Load(Path.Combine(USER_DATA_FOLDER, "data\\quotes.txt"));
    }

    bool _init = false;
    public async Task Init()
    {
      if (_init)
        throw new ApplicationException("Init should be called exactly once");
      _init = true;
      ChatterDataMgr.Init();

      _obs = new OBSWebsocket();
      _obs.Connected += (o, e) => { Log("obs connected"); UpdatedOBSConnected?.Invoke(this, EventArgs.Empty); };
      _obs.Disconnected += (o, e) => { Log("obs disconnected: " + e.ToJson()); UpdatedOBSConnected?.Invoke(this, EventArgs.Empty); };
      _obs.StreamStateChanged += (o, e) => { Log("obs state change: " + e.OutputState.StateStr); }; // TODO
      _obs.ConnectAsync(Settings.Default.ObsWebsocketUrl, Settings.Default.ObsWebsocketPassword);
      //_obs.SetInputSettings("VS", new JObject { { "text", "LETS FUCKING GO" } });
      // TODO test around obs disconnecting or not existing on init

      _tw = new TwitchClient(new WebSocketClient(new ClientOptions { DisconnectWait = 5000 }));
      _tw.Initialize(new ConnectionCredentials(BOT_NAME, File.ReadAllText(Settings.Default.TwitchOAuthBot)), CHANNEL);

      _tw.OnLog += (o, e) => Log("twitch log: " + e.Data);
      _tw.OnConnected += (o, e) => { Log("twitch connected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
      _tw.OnReconnected += (o, e) => { Log("twitch reconnected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
      _tw.OnDisconnected += (o, e) => { Log("twitch disconnected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
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
      
      _twApi = new TwitchAPI(settings: new ApiSettings { ClientId = Settings.Default.TwitchClientId, AccessToken = File.ReadAllText(Settings.Default.TwitchOAuth) });
      _twApi_More = new TwitchApi_More(_twApi.Settings);
      CHANNEL_ID = await GetUserId(CHANNEL).ConfigureAwait(true);
      BOT_ID = await GetUserId(BOT_NAME).ConfigureAwait(true);
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
      ChatActivity.Init(this);
      SneakyJapan.Init(this);
      LearnHiragana.Init(this);
      LongRunningPeriodicTask.Start(0, true, 1200123, 600000, 0, _ =>
      {
        return _twApi_More.Announce(CHANNEL_ID, CHANNEL_ID,
          "Simple Tree House https://discord.gg/48dDcAPwvD is where I chill and hang out :)",
          AnnouncementColors.Blue);
      });
      //var yuli = GetUserId("zulu_gula7").ThrowMainThread().Result;
      //var res = _twApi_More.Shoutout(CHANNEL_ID, CHANNEL_ID, yuli).Result;

      bool isVsVisible = false;
      int vsItemId = -1;
      int browserItemId = -1;
      ForegroundWinUtil.Init();
      ForegroundWinUtil.ForgroundWindowChanged += (o, e) =>
      {
        var TWITCH_CHAT_TITLE = CHANNEL + " - Chat - Twitch";
        if ((e.procName != "msedge" || e.title.StartsWith(TWITCH_CHAT_TITLE)) && e.procName != "devenv")
          return;
        var shouldShowVS = e.procName != "msedge";
        if (isVsVisible == shouldShowVS)
          return;
        isVsVisible = shouldShowVS;

        if (!_obs.IsConnected)
          return;
        if (vsItemId == -1) vsItemId = _obs.GetSceneItemId("CODE", "VS", 0);
        if (browserItemId == -1) browserItemId = _obs.GetSceneItemId("CODE", "Browser", 0);
        var vsIdx = _obs.GetSceneItemIndex("CODE", vsItemId);
        var browserIdx = _obs.GetSceneItemIndex("CODE", browserItemId);
        // looks stupid but obs is stupid
        var newVsIdx = browserIdx + (shouldShowVS ? (vsIdx > browserIdx ? 1 : 0) : (vsIdx > browserIdx ? 0 : -1));
        _obs.SetSceneItemIndex("CODE", vsItemId, newVsIdx);
      };
      
      _tw.Connect();

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
      var serviceProvider = new ServiceCollection().AddLogging().AddTwitchLibEventSubWebsockets().BuildServiceProvider();
      var cc = serviceProvider.GetService<EventSubWebsocketClient>();
      cc.WebsocketConnected += async (o, e) =>
      {
        if (e.IsRequestedReconnect)
          return;
        // subscribe to events https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
        var res = await _twApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
          "channel.channel_points_custom_reward_redemption.add",
          "1",
          new Dictionary<string, string> { { "broadcaster_user_id", CHANNEL_ID } },
          EventSubTransportMethod.Websocket,
          cc.SessionId
        ).ThrowMainThread().ConfigureAwait(true);
        //Debug.WriteLine(res.ToJson());
      };
      cc.WebsocketDisconnected += async (o , e)=>
      {
        // TODO Don't do this in production. You should implement a better reconnect strategy with exponential backoff
        while (!await cc.ReconnectAsync())
          await Task.Delay(1000);
      };
      cc.ChannelPointsCustomRewardRedemptionAdd += (o, e) =>
      {
        var ev = e.Notification.Payload.Event;
        _redeemCounts.AddOrUpdate(_rewardsKey(ev.UserId, ev.Reward.Id), 1, (k, v) => v + 1);
        // handle redeems that have NO input
        switch (ev.Reward.Title)
        {
          case "FIRST":
            TwSendMsg($"Nice. @{ev.UserName}");
            break;
          case "Japan":
          case "Japan!!":
            TwSendMsg("Japan, baby!!");
            break;
        }
      };
      _ = await cc.ConnectAsync().ConfigureAwait(true);
    }

    public static void Log(string msg)
    {
      Debug.WriteLine($"[{DateTime.Now}] {msg}");
    }

    public void TwSendMsg(string msg, Chatter tagChatter = null)
    {
      if (tagChatter != null) msg = $"@{tagChatter.DisplayName} {msg}";
      if (msg.Length > 490)
        msg = msg[0..490] + " .....";
#if DEBUG
      if (msg.Length != 0 && msg[0] != '.')
        msg = "* " + msg;
#endif
      _tw.SendMessage(_twJC, msg);
    }

    /// <summary>
    /// Searches cache before hitting the api
    /// </summary>
    public async Task<string> GetUserId(string canonicalName)
    {
      var uid = ChatterDataMgr.GetOrNull(canonicalName)?.uid;
      if (string.IsNullOrEmpty(uid))
      {
        var res = await _twApi.Helix.Users.GetUsersAsync(logins: new List<string> { canonicalName }).ConfigureAwait(true);
        uid = res.Users.FirstOrDefault()?.Id;
      }
      return uid;
    }

    public async Task<ChatSettings> GetChatSettings()
    {
      var res = (await _twApi.Helix.Chat.GetChatSettingsAsync(CHANNEL_ID, CHANNEL_ID).ConfigureAwait(true)).Data[0];
      return new ChatSettings
      {
        EmoteMode = res.EmoteMode,
        FollowerMode = res.FollowerMode,
        FollowerModeDuration = res.FollowerModeDuration,
        NonModeratorChatDelay = res.NonModeratorChatDelay,
        NonModeratorChatDelayDuration = res.NonModeratorChatDelayDuration,
        SlowMode = res.SlowMode,
        SlowModeWaitTime = res.SlowModeWaitDuration,
        SubscriberMode = res.SubscriberMode,
        UniqueChatMode = res.UniqueChatMode
      };
    }

    public async Task ChangeChatSettings(Action<ChatSettings> doChanges)
    {
      var settings = await GetChatSettings().ConfigureAwait(true);
      doChanges(settings);
      await _twApi.Helix.Chat.UpdateChatSettingsAsync(CHANNEL_ID, CHANNEL_ID, settings).ConfigureAwait(true);
    }

    public static BotCommandId ParseCommandId(string cmd)
    {
      var cid = cmd switch
      {
        "ignore" or "addignore" => BotCommandId.AddIgnoredBot,
        "unignore" or "remignore" => BotCommandId.RemoveIgnoredBot,
        "slowmodeoff" => BotCommandId.SlowModeOff,
        "slowmode" => BotCommandId.SlowMode,
        "title" or "settitle" => BotCommandId.SetTitle,
        "game" or "setgame" => BotCommandId.SetGame,
        "searchgame" => BotCommandId.SearchGame,
        "count" => BotCommandId.GetCmdCounter,
        "redeems" or "countredeem" or "countredeems" => BotCommandId.GetRedeemCounter,
        "followage" => BotCommandId.FollowAge,
        "japan" => BotCommandId.SneakyJapan,
        "japanstats" => BotCommandId.SneakyJapan_Stats,
        "coin" or "coinflip" => BotCommandId.CoinFlip,
        "roll" or "diceroll" or "rolldice" => BotCommandId.DiceRoll,
        "quote" or "wisdom" => BotCommandId.Quote_Get,
        "addquote" or "addwisdom" => BotCommandId.Quote_Add,
        "delquote" or "delwisdom" => BotCommandId.Quote_Del,
        "hiragana" => BotCommandId.LearnHiragana,
        "elo" or "rating" => BotCommandId.GetChessRatings,
        _ => (BotCommandId)(-1),
      };
      if (cid < 0)
      {
        // TODO customs
      }
      return cid;
    }

    private void twOnMessage(object sender, OnMessageReceivedArgs e)
    {
      Chatter chatter = ChatActivity.OnMessage(e.ChatMessage);
      if (chatter == null)
        return; // ignored bot user
      // TODO use nicknames and mgr for display names and stuff

      var msg = e.ChatMessage.Message;
      // handle redeems that have input
      if (e.ChatMessage.CustomRewardId == "536092ba-85bb-4df8-bd15-286410fe96c6") // Change stream title
      {
        _ = SetGameOrTitle.SetTitle(this, chatter, msg).ThrowMainThread();
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
      if (msg.Length == 0)
        return;

      // general moderation
      if (msg.Contains("wow")) TwSendMsg("Mama mia");

      // commands
      var args = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
      if (args.Count > 0 && args[0].StartsWith(CMD_PREFIX))
      {
        var cmd = args[0][CMD_PREFIX.Length..].ToLowerInvariant();
        args.RemoveAt(0);
        var argsStr = string.Join(' ', args);

        if (cmd == "dbgsave")
        {
          ChatterDataMgr.ForceSave();
          TwSendMsg("hmmmmkay");
          return;
        }

        BotCommandId cid = ParseCommandId(cmd);

        // TODO make req. user levels a user pref
        // TODO make cmd names/aliases a user pref
        // TODO song requests
        // TODO !worship
        switch (cid)
        {
          // MOD
          case BotCommandId.AddIgnoredBot:
          case BotCommandId.RemoveIgnoredBot:
            if (chatter.userLevel < UserLevel.Mod) return;
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
            if (chatter.userLevel < UserLevel.Mod) return;
            slowmode = args.FirstOrDefault()?.ToLowerInvariant() != "off";
            ChatActivity.IncCommandCounter(chatter, slowmode ? BotCommandId.SlowMode : BotCommandId.SlowModeOff);
            _ = ChangeChatSettings(s =>
            {
              s.SlowModeWaitTime = 3;
              s.SlowMode = slowmode;
              s.EmoteMode = slowmode;
              s.FollowerMode = slowmode;
              s.SubscriberMode = slowmode;
            }).ThrowMainThread();
            return;
          case BotCommandId.SetTitle:
            if (chatter.userLevel < UserLevel.Mod) return;
            ChatActivity.IncCommandCounter(chatter, BotCommandId.SetTitle);
            _ = SetGameOrTitle.SetTitle(this, chatter, argsStr).ThrowMainThread();
            return;
          case BotCommandId.SetGame:
            if (chatter.userLevel < UserLevel.Mod) return;
            ChatActivity.IncCommandCounter(chatter, BotCommandId.SetGame);
            _ = SetGameOrTitle.SetGame(this, chatter, argsStr).ThrowMainThread();
            return;

          // COMMON
          case BotCommandId.SearchGame:
            ChatActivity.IncCommandCounter(chatter, BotCommandId.SearchGame);
            _ = SetGameOrTitle.SearchGame(this, chatter, argsStr).ThrowMainThread();
            return;
          case BotCommandId.GetCmdCounter:
            {
              if (args.Count == 0) return;
              ChatActivity.IncCommandCounter(chatter, BotCommandId.GetCmdCounter);
              string targetCmdName = args[0];
              if (targetCmdName.StartsWith(CMD_PREFIX))
                targetCmdName = targetCmdName[CMD_PREFIX.Length..];
              BotCommandId targetCid = ParseCommandId(targetCmdName.ToLowerInvariant());
              if (targetCid < 0)
              {
                TwSendMsg($"No such command '{targetCmdName}'", chatter);
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
            Task.Run(async () =>
            {
              string tagUser = chatter.DisplayName;
              string uid = chatter.uid;
              if (args.Count > 0)
              {
                tagUser = args[0].CleanUsername();
                uid = await GetUserId(tagUser.CanonicalUsername()).ConfigureAwait(true);
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
            }).ThrowMainThread();
            return;
          case BotCommandId.GetRedeemCounter:
            return; // TODO for !redeems TwitchApi broken? I'm dumb? maybe one day get back to it
            if (args.Count == 0)
            {
              TwSendMsg("Specify a reward title", chatter);
              return;
            }
            Task.Run(async () =>
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
            }).ThrowMainThread();
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
          case BotCommandId.CoinFlip:
            ChatActivity.IncCommandCounter(chatter, BotCommandId.CoinFlip);
            TwSendMsg($"Coin flip: {(Rand.R.Next(2) == 0 ? "heads" : "tails")}");
            return;
          case BotCommandId.DiceRoll:
            ChatActivity.IncCommandCounter(chatter, BotCommandId.DiceRoll);
            int die;
            if (!int.TryParse(args.FirstOrDefault(), out die)) die = 20;
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
            if (chatter.userLevel < UserLevel.Vip) return;
            ChatActivity.IncCommandCounter(chatter, BotCommandId.Quote_Add);
            int newWisdomIdx = Quotes.AddQuote(argsStr);
            Quotes.Save();
            TwSendMsg("Quote " + newWisdomIdx + " added");
            return;
          case BotCommandId.Quote_Del:
            if (chatter.userLevel < UserLevel.Vip) return;
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
            if (chatter.userLevel < UserLevel.Vip) return;
            if (args.Count == 0 || !(args[0] is "on" or "off"))
            {
              TwSendMsg("Expected a parameter 'on' or 'off'", chatter);
              return;
            }
            LearnHiragana._task.Enabled = args[0] == "off";
            TwSendMsg("SeemsGood", chatter);
            return;
          case BotCommandId.GetChessRatings:
            {
              var targetName = args.FirstOrDefault()?.CleanUsername() ?? chatter.DisplayName;
              bool found = false;
              try
              {
                using var http = new HttpClient();
                var lichess = JToken.Parse(http.GetStringAsync("https://lichess.org/api/user/" + targetName).Result);
                lichess = lichess["perfs"];
                if (lichess != null)
                {
                  int bullet = lichess["bullet"]?["rating"]?.Value<int>() ?? 0;
                  int blitz = lichess["blitz"]?["rating"]?.Value<int>() ?? 0;
                  int rapid = lichess["rapid"]?["rating"]?.Value<int>() ?? 0;
                  int classical = lichess["classical"]?["rating"]?.Value<int>() ?? 0;
                  int daily = lichess["daily"]?["rating"]?.Value<int>() ?? 0;
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
                var chesscum = JToken.Parse(http.GetStringAsync("https://api.chess.com/pub/player/" + targetName + "/stats").Result);
                if (chesscum != null)
                {
                  int bullet = chesscum["chess_bullet"]?["last"]?["rating"]?.Value<int>() ?? 0;
                  int blitz = chesscum["chess_blitz"]?["last"]?["rating"]?.Value<int>() ?? 0;
                  int rapid = chesscum["chess_rapid"]?["last"]?["rating"]?.Value<int>() ?? 0;
                  int daily = chesscum["chess_daily"]?["last"]?["rating"]?.Value<int>() ?? 0;
                  var sb = new StringBuilder();
                  if (bullet != 0) sb.Append("Bullet ").Append(bullet).Append(" | ");
                  if (blitz != 0) sb.Append("Blitz ").Append(blitz).Append(" | ");
                  if (rapid != 0) sb.Append("Rapid ").Append(rapid).Append(" | ");
                  if (daily != 0) sb.Append("Daily ").Append(daily).Append(" | ");
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
              return;
            }
        }

        // TODO change to work with BotCommandId.FIRST_CUSTOM_COMMAND
        tryModCommand(cmd, e.ChatMessage, chatter, args, argsStr);
      }
    }

    private bool tryModCommand(string cmd, ChatMessage msgData, Chatter chatter, List<string> args, string argsStr)
    {
      // TODO persist commands
      bool shouldTagChatter = false;
      string twFormat;
      switch (cmd)
      {
        case "weather":
          if (string.IsNullOrWhiteSpace(argsStr))
          {
            TwSendMsg("Missing input, try " + CMD_PREFIX + "weather Osaka Japan");
            return true;
          }
          shouldTagChatter = true;
          twFormat = "$(fetch https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/$(input)/today?unitGroup=metric&include=current&key=RK7YNMRSS664ZJZXWJHZZTF8W&contentType=json)"
                   + "Weather at $(res.resolvedAddress) - $(res.currentConditions.conditions) | humidity $(res.currentConditions.humidity)% | temp $(res.currentConditions.temp)°C | feels $(res.currentConditions.feelslike)°C";
          break;
        case "hug": twFormat = "$(user) is hugging $(target) and its very wholesome..."; break;
        case "time": twFormat = CHANNEL + "'s time is $(time)"; break;
        case "winner": twFormat = "$(randomChatter) won, horrah!"; break;
        case "kate": twFormat = "$(fetch https://twitch.center/customapi/quote?token=767931e8&data=$(input))"; break;
        // TODO $(calc stuff)
        //case "f2c": twFormat = "$(arg0)°F = $(calc ($arg0-32)*5/9)°C"; break;
        case "c2f":
          {
            var deg = float.TryParse(args[0], out float x) ? x : 0;
            twFormat = $"{deg}°C = {deg * 9.0f / 5 + 32:F1}°F";
            break;
          }
        case "f2c":
          {
            var deg = float.TryParse(args[0], out float x) ? x : 0;
            twFormat = $"{deg}°C = {(deg - 32) * 5.0f / 9:F1}°F";
            break;
          }
        default: return false;
      }

      TwSendMsg(formatResponseText(twFormat, msgData, chatter, args, argsStr), shouldTagChatter ? chatter : null);
      return true;
    }

    ////////////////////////////////////////////////////////////////////////////////////

    static readonly Regex rgxTwFormatParticle = new(@"\$\([^)]+\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    string formatResponseText(string text, ChatMessage msgData, Chatter chatter, List<string> args, string argsStr, Dictionary<string, JToken> fetchResults = null)
    {
      // fetches must come at the beginning, and they accumulate results into $(res) or a $(namedVar)
      // $(fetch:w www.example.com/q=$input) The weather at $(w.City) is $(w.WeatherText)
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
          int j = i;
          while (j < text.Length && text[j] != ' ' && text[j] != ')')
          {
            var c = text[j++];
            if (c != '_' && (c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9'))
              return BAD_SYNTAX_MSG;
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
              return "[[ Bad fetch syntax ]] Character '$' in the url should be encoded as '%24'";
            i++;
            // wait for closing ) but make sure simple variable
            while (i < text.Length && text[i] != ')')
            {
              if (text[i] == '$')
                return "[[ Bad fetch syntax ]] The placeholder $() in the URL must be a basic variable name";
              i++;
            }
          }
          i++;
        }
        if (text[i] != ')')
          return BAD_SYNTAX_MSG;
        var url = text[urlStart..i].TrimEnd();
        if (string.IsNullOrWhiteSpace(url) || url[0] == '$')
          return BAD_SYNTAX_MSG;
        i++;
        SkipSpace();
        // finished parsing the fetch
        text = text[i..];

        // FETCH
        url = formatResponseText(url, msgData, chatter, args, argsStr, fetchResults);
        string json;
        using var http = new HttpClient();
        try
        {
          json = http.GetStringAsync(url).Result;
        }
        catch (Exception ex)
        {
          Log("[[ Failed fetch ]] " + ex);
          return "[[ Failed fetch ]] couldn't reach the url, see logs";
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
        if (lastFetchResultName != null)
          return fetchResults[lastFetchResultName].ToString();
        return "";
      }

      // format particles
      return rgxTwFormatParticle.Replace(text, m =>
      {
        var particle = m.Value[2..^1]; // $(name)
        // first try find fetch result under that name (case sensitive)
        var particleMembers = particle.Split('.');
        if (fetchResults.TryGetValue(particleMembers[0], out var res))
        {
          // member access $(res.Results.0.Name)
          for (int j = 1; j < particleMembers.Length; j++)
          {
            if (!res.HasValues)
              return $"<cant read {string.Join('.', particleMembers.Take(j + 1))}>";
            var member = particleMembers[j];
            try
            {
              res = member.Length != 0 && member[0] >= '0' && member[0] <= '9' && int.TryParse(member, out int k)
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
        particle = particle.ToLowerInvariant();
        return particle switch
        {
          "input" or "args" => argsStr,
          "name" or "user" => chatter.DisplayName,
          "user_id" => GetUserId(chatter.name).Result,
          "target" => args.FirstOrDefault()?.CleanUsername() ?? "<no target>",
          "randomchatter" => ChatActivity.RandomChatter(),
          "time" => DateTime.Now.ToShortTimeString(),
          "fetch" => "<invalid fetch usage>",
          _ => m.Value, // original particle text e.g. '$unreal.42'
        };
      });
    }

  }
}
