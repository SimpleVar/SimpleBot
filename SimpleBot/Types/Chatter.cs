namespace SimpleBot
{
  class Chatter
  {
    public string uid, name;
    public UserLevel userLevel;
    public long watchtime_ms;
    public Dictionary<BotCommandId, int> cmd_counters;
    public int groupColorRGB;
    
    private string displayName;
    private SneakyJapanStats sneakyJapanStats;

    public string DisplayName { get => displayName ?? name; set => displayName = value; }
    public SneakyJapanStats SneakyJapanStats => sneakyJapanStats ??= new();
    public int GetCmdCounter(BotCommandId cid) => (cmd_counters ??= new()).TryGetValue(cid, out int c) ? c : 0;
  }
}
