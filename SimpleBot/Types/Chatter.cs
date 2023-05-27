namespace SimpleBot
{
  class Chatter
  {
    public string uid, name, displayName;
    public UserLevel userLevel;
    public long watchtime_ms;
    public Dictionary<BotCommandId, int> cmd_counters;
    public SneakyJapanStats sneakyJapanStats;

    public string DisplayName => displayName ?? name;
    public SneakyJapanStats SneakyJapanStats => sneakyJapanStats ??= new();
    public int GetCmdCounter(BotCommandId cid) => (cmd_counters ??= new()).TryGetValue(cid, out int c) ? c : 0;
  }
}
