using System.Text.Json.Serialization;

namespace SimpleBot
{
    class Chatter
    {
        public string uid, name;
        public UserLevel userLevel;
        public long watchtime_ms;
        public Dictionary<BotCommandId, int> cmd_counters;
        public int groupColorRGB;

        public string displayName;
        public SneakyJapanStats sneakyJapanStats;

        [JsonIgnore]
        public Stack<string> msgIds = [];

        [JsonIgnore]
        public string DisplayName { get => displayName ?? name; set => displayName = value; }
        [JsonIgnore]
        public SneakyJapanStats SneakyJapanStats => sneakyJapanStats ??= new();
        
        public int GetCmdCounter(BotCommandId cid) => (cmd_counters ??= new()).TryGetValue(cid, out int c) ? c : 0;

        [JsonIgnore]
        public bool _isDirty { get; private set; }
        public void SetDirty() => _isDirty = true;
    }
}
