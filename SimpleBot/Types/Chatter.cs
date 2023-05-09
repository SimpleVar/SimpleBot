using System.Security.Cryptography;

namespace SimpleBot
{
  class Chatter
  {
    public string uid, name, displayName;
    public UserLevel userLevel;
    public long watchtime_sec;
    public Dictionary<BotCommandId, int> cmd_counters;
    public SneakyJapanStats sneakyJapanStats;

    public string DisplayName => displayName ?? name;
    public SneakyJapanStats SneakyJapanStats => sneakyJapanStats ??= new();
    public int GetCmdCounter(BotCommandId cid) => cmd_counters == null ? 0 : cmd_counters.TryGetValue(cid, out int c) ? c : 0;
  }
}
