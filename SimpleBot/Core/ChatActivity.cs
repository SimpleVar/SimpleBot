using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace SimpleBot
{
  static class ChatActivity
  {
    static readonly object _lock = new();
    static Bot _bot;
    static string _settingsFile;
    static ChatActivitySettings _settings = new() { IgnoredBotUsernames = new[] { "nightbot", "streamlabs" } };
    static HashSet<string> _ignoredBotNames;
    static readonly HashSet<string> _usersInChat = new();
    static readonly List<(DateTime ts, Chatter chatter)> _activityHistory = new();

    public static event EventHandler UpdatedUsersInChat;

    public static void Load(string settingsFile)
    {
      lock (_lock)
      {
        _settingsFile = settingsFile;
        try
        {
          _settings = File.ReadAllText(_settingsFile).FromJson<ChatActivitySettings>();
          _settings.IgnoredBotUsernames ??= Array.Empty<string>();
        }
        catch { }
      }
    }

    static void _save()
    {
      if (string.IsNullOrWhiteSpace(_settingsFile))
        return;
      try
      {
        File.WriteAllText(_settingsFile, _settings.ToJson());
      }
      catch { }
    }

    public static void Init(Bot bot)
    {
      _bot = bot;
      _ignoredBotNames = _settings.IgnoredBotUsernames.ToHashSet();
      _bot._tw.OnExistingUsersDetected += twOnExistingUsersDetected;
      _bot._tw.OnUserJoined += twOnUserJoined;
      _bot._tw.OnUserLeft += twOnUserLeft;
      // TODO timer to accumulate watchtime by _usersInChat
    }

    /// <summary>
    /// Called when message is recieved, returning null for ignored users, or the associated ChatterData.
    /// Used for stats like watchtime, activity monitoring, etc
    /// </summary>
    public static Chatter OnMessage(ChatMessage msg)
    {
      var name = msg.Username.ToLowerInvariant();
      if (IsIgnoredBot(name))
        return null;

      var chatter = ChatterDataMgr.Get(name);
      lock (_lock)
      {
        _activityHistory.Add((DateTime.UtcNow, chatter));
      }

      var msgUserLevel = msg.GetUserLevel();
      if (chatter.displayName != msg.DisplayName ||
          chatter.userLevel != msgUserLevel ||
          chatter.uid != msg.UserId)
      {
        chatter.displayName = msg.DisplayName;
        chatter.userLevel = msgUserLevel;
        chatter.uid = msg.UserId;
        ChatterDataMgr.Update();
      }
      return chatter;
    }

    public static void IncCommandCounter(Chatter chatter, BotCommandId cid)
    {
      (chatter.cmd_counters ??= new())[cid] = chatter.GetCmdCounter(cid) + 1;
      ChatterDataMgr.Update();
    }

    public static int AddIgnoredBot(string[] names)
    {
      lock (_lock)
      {
        int added = 0;
        foreach (var name in names)
        {
          if (_ignoredBotNames.Add(name))
            added++;
        }
        if (added != 0) _save();
        return added;
      }
    }

    public static int RemoveIgnoredBot(string[] names)
    {
      lock (_lock)
      {
        int removed = 0;
        foreach (var name in names)
        {
          if (_ignoredBotNames.Remove(name))
            removed++;
        }
        if (removed != 0) _save();
        return removed;
      }
    }

    public static int GetIgnoredBotsCount()
    {
      lock (_lock)
      {
        return _ignoredBotNames.Count;
      }
    }

    public static bool IsIgnoredBot(string canonicalName)
    {
      lock (_lock)
      {
        return _ignoredBotNames.Contains(canonicalName);
      }
    }

    public static bool IsUserInChat(string canonicalName)
    {
      lock (_lock)
      {
        return _usersInChat.Contains(canonicalName);
      }
    }

    public static string[] UsersInChat()
    {
      lock (_lock)
      {
        return _usersInChat.ToArray();
      }
    }

    public static string RandomChatter()
    {
      lock (_lock)
      {
        return _usersInChat.ToArray().AtRand() ?? "";
      }
    }

    public static HashSet<Chatter> GetActiveChatters(TimeSpan span, int maxChattersNeeded = 0)
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
          if (res.Count > maxChattersNeeded)
            break;
        }
      }
      return res;
    }

    private static void twOnUserLeft(object sender, OnUserLeftArgs e)
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

    private static void twOnUserJoined(object sender, OnUserJoinedArgs e)
    {
      _twOnUserJoined(e.Username);
    }

    private static void twOnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e)
    {
      foreach (var user in e.Users)
        _twOnUserJoined(user);
    }

    private static void _twOnUserJoined(string name)
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
