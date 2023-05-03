using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using Octokit;
using SimpleBot.Commands;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.Handler;
using TwitchLib.EventSub.Websockets.Extensions;
using TwitchLib.PubSub.Models.Responses;
using Websocket.Client.Logging;

namespace SimpleBot
{
  class CCC : INotificationHandler
  {
    public string SubscriptionType => "channel.moderator.add";

    public void Handle(EventSubWebsocketClient client, string jsonString, JsonSerializerOptions serializerOptions)
    {
    }
  }

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

    public event EventHandler UpdatedUsersInChat;
    public event EventHandler UpdatedTwitchConnected;
    public event EventHandler UpdatedOBSConnected;

    #endregion

    #region user data paths

    public string QuotesFile { get; private set; }

    #endregion

    #region user data

    public Dictionary<string, UserWatchtime> _watchtimes;
    public HashSet<string> _ignoredBotUsernames;

    #endregion

    public TwitchClient _tw;
    public TwitchAPI _twApi;
    public TwitchApi_More _twApi_More;
    // TODO lock against race cond exception probably
    public HashSet<string> _usersInChat = new HashSet<string>();
    public OBSWebsocket _obs;

    public string CHANNEL_ID { get; private set; }
    public string BOT_ID { get; private set; }
    readonly JoinedChannel _twJC; // fake object with no data for quick TwSendMessage
    
    public Bot()
    {
      _twJC = new JoinedChannel(CHANNEL);

      // Load persistent data
      if (string.IsNullOrWhiteSpace(USER_DATA_FOLDER))
        return;
      
      Directory.CreateDirectory(Path.Combine(USER_DATA_FOLDER, "data"));
      ChatterDataMgr.Load(Path.Combine(USER_DATA_FOLDER, "data\\chatters_data.txt"));
      QuotesFile = Path.Combine(USER_DATA_FOLDER, "data\\quotes.txt");
      Quotes.Load(QuotesFile);

      // TODO persist these
      _watchtimes = new Dictionary<string, UserWatchtime>();
      _ignoredBotUsernames = new HashSet<string> { "nightbot", "streamlabs" };//, "simpiebot" };
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

      _tw = new TwitchClient(new WebSocketClient(new ClientOptions { DisconnectWait = 5000 }));
      _tw.Initialize(new ConnectionCredentials(BOT_NAME, File.ReadAllText(Settings.Default.TwitchOAuthBot)), CHANNEL);

      _tw.OnLog += (o, e) => Log("twitch log: " + e.Data);
      _tw.OnConnected += (o, e) => { Log("twitch connected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
      _tw.OnReconnected += (o, e) => { Log("twitch reconnected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
      _tw.OnDisconnected += (o, e) => { Log("twitch disconnected"); UpdatedTwitchConnected?.Invoke(this, EventArgs.Empty); };
      _tw.OnExistingUsersDetected += twOnExistingUsersDetected;
      _tw.OnUserJoined += twOnUserJoined;
      _tw.OnUserLeft += twOnUserLeft;
      _tw.OnMessageReceived += twOnMessage;

      _tw.Connect();
      _twApi = new TwitchAPI(settings: new ApiSettings { ClientId = Settings.Default.TwitchClientId, AccessToken = File.ReadAllText(Settings.Default.TwitchOAuth) });
      _twApi_More = new TwitchApi_More(_twApi.Settings);
      CHANNEL_ID = (await _twApi.Helix.Users.GetUsersAsync(logins: new List<string> { CHANNEL }).ConfigureAwait(true)).Users[0].Id;
      BOT_ID = (await _twApi.Helix.Users.GetUsersAsync(logins: new List<string> { BOT_NAME }).ConfigureAwait(true)).Users[0].Id;

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
        // handle redeems that have NO input
        switch (ev.Reward.Title)
        {
          case "FIRST":
            TwSendMsg("Nice.", ev.UserName);
            break;
          case "Japan":
          case "Japan!!":
            TwSendMsg("Japan, baby!!");
            break;
        }
      };
      _ = await cc.ConnectAsync().ConfigureAwait(true);

      // Init custom thingies
      SneakyJapan.Init(this);

      bool isVsVisible = false;
      int vsItemId = -1;
      int browserItemId = -1;
      ForegroundWinUtil.Init(this);
      ForegroundWinUtil.ForgroundWindowChanged += (o, e) =>
      {
        //Debug.WriteLine(e.procName + " | " + e.title);
        if (e.procName != "msedge" && e.procName != "devenv")
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
    }

    public static void Log(string msg)
    {
      Debug.WriteLine($"[{DateTime.Now}] {msg}");
    }

    public void TwSendMsg(string msg, string tagUser = null)
    {
      if (tagUser != null) msg = $"@{tagUser} {msg}";
#if DEBUG
      _tw.SendMessage(_twJC, "* " + msg);
#else
      _tw.SendMessage(_twJC, msg);
#endif
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

    public string RandomChatter() => _usersInChat.ToArray().AtRand() ?? "";

    #region Join/Leave

    // TODO call that on all users in chat before we closing
    private void twOnUserLeft(object sender, OnUserLeftArgs e)
    {
      var name = e.Username.CanonicalUsername();
      if (_ignoredBotUsernames.Contains(name))
        return;
      _usersInChat.Remove(name);
      UpdatedUsersInChat?.Invoke(this, EventArgs.Empty);
      if (_watchtimes.TryGetValue(name, out var w))
        w.prevSeenInChat = false;
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
      if (_ignoredBotUsernames.Contains(name))
        return;
      _usersInChat.Add(name);
      UpdatedUsersInChat?.Invoke(this, EventArgs.Empty);
      var now = DateTime.UtcNow;
      if (!_watchtimes.TryGetValue(name, out var w))
      {
        _watchtimes.Add(name, new UserWatchtime { totalSeconds = 0, prevSeenInChat = true, prevSeenInChatTime = now });
        return;
      }

      if (w.prevSeenInChat)
        w.totalSeconds += Math.Max(0, (int)now.Subtract(w.prevSeenInChatTime).TotalSeconds); // cap at 0 to semi-handle messed up clocks
      w.prevSeenInChat = true;
      w.prevSeenInChatTime = now;
    }

    #endregion

    private void twOnMessage(object sender, OnMessageReceivedArgs e)
    {
      var name = e.ChatMessage.Username.CanonicalUsername();
      // TODO use nicknames and mgr for display names and stuff
      if (_ignoredBotUsernames.Contains(name))
        return;

      var userLevel = e.ChatMessage.GetUserLevel();
      var msg = e.ChatMessage.Message;
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

      // handle redeems that have input
      if (e.ChatMessage.CustomRewardId == "536092ba-85bb-4df8-bd15-286410fe96c6") // Change stream title
        _ = SetGameOrTitle.SetTitle(this, e.ChatMessage.Username, msg).ThrowMainThread();

      // commands
      var args = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
      if (args.Count > 0 && args[0].StartsWith(CMD_PREFIX))
      {
        var cmd = args[0][CMD_PREFIX.Length..].ToLowerInvariant();
        args.RemoveAt(0);
        var argsStr = string.Join(' ', args);

        // TODO make req. user levels a user pref
        // TODO make cmd names/aliases a user pref
        // TODO song requests
        // TODO !worship
        switch (cmd)
        {
          /*
          case "test":
            if (userLevel < UserLevel.Mod) return;
            var itemId = _obs.GetSceneItemId("CODE", "task", 0);
            var enabled = _obs.GetSceneItemEnabled("CODE", itemId);
            var aaa = _obs.GetSourceFilterList("task");
            _obs.SetSceneItemEnabled("CODE", itemId, !enabled);
            return;
          */
          case "followage":
            Task.Run(async () =>
            {
              string tagUser = e.ChatMessage.Username;
              string uid = e.ChatMessage.UserId;
              if (args.Count > 0)
              {
                tagUser = args[0].CleanUsername();
                uid = (await _twApi.Helix.Users.GetUsersAsync(logins: new List<string> { tagUser }).ConfigureAwait(true)).Users[0].Id;
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
          case "japan":
            SneakyJapan.Japan(e.ChatMessage.Username, cmd, args, argsStr);
            return;
          case "coin":
          case "coinflip":
            TwSendMsg($"Coin flip: {(Rand.R.Next(2) == 0 ? "heads" : "tails")}");
            return;
          case "roll":
          case "diceroll":
          case "rolldice":
            int die;
            if (!int.TryParse(args.FirstOrDefault(), out die)) die = 20;
            int roll = Rand.R.Next(die) + 1;
            TwSendMsg($"Rolling d{die}... you get {roll}!{(roll == 20 ? " Kreygasm" : "")}");
            return;
          case "slowmodeoff":
            bool slowmode;
            args.Insert(0, "off");
            goto case "slowmode";
          case "slowmode":
            if (userLevel < UserLevel.Mod) return;
            slowmode = args.FirstOrDefault()?.ToLowerInvariant() != "off";
            _ = ChangeChatSettings(s =>
            {
              s.SlowModeWaitTime = 3;
              s.SlowMode = slowmode;
              s.EmoteMode = slowmode;
              s.FollowerMode = slowmode;
              s.SubscriberMode = slowmode;
            }).ThrowMainThread();
            return;
          case "game":
          case "setgame":
            if (userLevel < UserLevel.Mod) return;
            _ = SetGameOrTitle.SetGame(this, e.ChatMessage.Username, argsStr).ThrowMainThread();
            return;
          case "searchgame":
            _ = SetGameOrTitle.SearchGame(this, e.ChatMessage.Username, argsStr).ThrowMainThread();
            return;
          case "title":
          case "settitle":
            if (userLevel < UserLevel.Mod) return;
            _ = SetGameOrTitle.SetTitle(this, e.ChatMessage.Username, argsStr).ThrowMainThread();
            return;
          case "quote":
          case "wisdom":
            if (userLevel < UserLevel.Vip) return;
            if (int.TryParse(args.FirstOrDefault(), out int wisIdx))
              TwSendMsg(Quotes.GetQuote(wisIdx - 1));
            else if (args.Count > 0)
            {
              var query = argsStr;
              var q = Quotes.FindQuote(query);
              TwSendMsg(q != null ? q : "No quote found with the search term: " + query);
            }
            else
              TwSendMsg(Quotes.GetRandom());
            return;
          case "addquote":
          case "addwisdom":
            if (userLevel < UserLevel.Vip) return;
            int newWisdomIdx = Quotes.AddQuote(argsStr);
            Quotes.Save(QuotesFile);
            TwSendMsg("Quote " + newWisdomIdx + " added");
            return;
          case "delquote":
          case "delwisdom":
            if (userLevel < UserLevel.Vip) return;
            if (!int.TryParse(args.FirstOrDefault(), out int delIdx))
              return;
            if (!Quotes.DelQuote(delIdx - 1))
              TwSendMsg("There is no quote " + delIdx);
            else
            {
              Quotes.Save(QuotesFile);
              TwSendMsg("Quote " + delIdx + " deleted");
            }
            return;
        }

        tryModCommand(cmd, e.ChatMessage, args, argsStr);
      }
    }

    private bool tryModCommand(string cmd, ChatMessage data, List<string> args, string argsStr)
    {
      // TODO persist commands
      string twFormat;
      switch (cmd)
      {
        case "hug": twFormat = "$user is hugging $target and its very wholesome..."; break;
        case "time": twFormat = CHANNEL + "'s time is $time"; break;
        case "winner": twFormat = "$randomChatter won, horrah!"; break;
        default: return false;
      }

      TwSendMsg(formatResponseText(twFormat, data, args, argsStr));
      return true;
    }

    ////////////////////////////////////////////////////////////////////////////////////

    static readonly Regex rgxTwFormatParticle = new Regex(@"\$\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    string formatResponseText(string text, ChatMessage data, List<string> args, string argsStr)
    {
      return rgxTwFormatParticle.Replace(text, m =>
        m.Value.Substring(1).ToLowerInvariant() switch
        {
          "user" => data.DisplayName,
          "target" => args.FirstOrDefault()?.CleanUsername() ?? "<no target>",
          "randomchatter" => RandomChatter(),
          "time" => DateTime.Now.ToShortTimeString(),
          _ => m.Value,
        }
      );
    }

  }
}
