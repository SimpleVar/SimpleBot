using Timer = System.Windows.Forms.Timer;

namespace SimpleBot
{
  class ChatterData
  {
    public string name, displayName;
    public SneakyJapanStats sneakyJapanStats;

    public string DisplayName => displayName ?? name;
  }

  internal class ChatterDataMgr
  {
    static object _lock = new object();
    static Dictionary<string, ChatterData> _data = new();
    static string _chattersDataPath;
    static bool _dataChanged;
    static Timer _saveFileTimer;

    public static void Init()
    {
      if (_saveFileTimer != null)
        throw new ApplicationException("Init should be called exactly once");
      _saveFileTimer = new Timer { Interval = 30000, Enabled = true };
      _saveFileTimer.Tick += (o, e) =>
      {
        lock (_lock)
        {
          if (!_dataChanged || string.IsNullOrEmpty(_chattersDataPath))
            return;
          var json = All().ToArray().ToJson();
          try
          {
            File.WriteAllText(_chattersDataPath, json);
            _dataChanged = false;
          }
          catch { }
        }
      };
    }

    public static void Load(string chattersDataPath)
    {
      ChatterData[] data;
      try
      {
        data = File.ReadAllText(chattersDataPath).FromJson<ChatterData[]>();
      }
      catch
      {
        return;
      }
      lock (_lock)
      {
        _chattersDataPath = chattersDataPath;
        _data = data.ToDictionary(x => x.name);
      }
    }

    public static ChatterData[] All()
    {
      lock (_lock)
      {
        return _data.Values.ToArray();
      }
    }

    public static ChatterData Get(string name)
    {
      lock (_lock)
      {
        return _data.TryGetValue(name, out var x) ? x : new ChatterData { name = name, displayName = name };
      }
    }
    
    public static void Update(ChatterData chatter)
    {
      lock (_lock)
      {
        _data[chatter.name] = chatter;
        _dataChanged = true;
      }
    }

  }
}
