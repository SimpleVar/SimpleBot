using Timer = System.Windows.Forms.Timer;

namespace SimpleBot
{
  internal static class ChatterDataMgr
  {
    const int SAVE_INTERVAL_MS = 600000;

    static readonly object _lock = new();
    static Dictionary<string, Chatter> _data = new();
    static string _chattersDataPath;
    static bool _dataChanged;
    static Timer _saveFileTimer;

    public static void Init()
    {
      if (_saveFileTimer != null)
        throw new ApplicationException("Init should be called exactly once");

      _saveFileTimer = new Timer { Interval = SAVE_INTERVAL_MS, Enabled = true };
      _saveFileTimer.Tick += (o, e) => { ForceSave(); };
    }

    public static void ForceSave()
    {
      lock (_lock)
      {
        _save_noLock();
      }
    }

    public static void _save_noLock()
    {
#if DEBUG
      return;
#endif
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

    public static void Load(string chattersDataPath)
    {
      Chatter[] data;
      try
      {
        var json = File.ReadAllText(chattersDataPath);
        data = json.FromJson<Chatter[]>();
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

    public static Chatter[] All()
    {
      lock (_lock)
      {
        return _data.Values.ToArray();
      }
    }

    public static Chatter GetOrNull(string canonicalName)
    {
      lock (_lock)
      {
        return _data.TryGetValue(canonicalName, out var x) ? x : null;
      }
    }

    public static Chatter Get(string canonicalName)
    {
      lock (_lock)
      {
        if (_data.TryGetValue(canonicalName, out var x))
          return x;
        x = new Chatter { name = canonicalName };
        _data.Add(canonicalName, x);
        return x;
      }
    }
    
    public static void Update()
    {
      lock (_lock)
      {
        _dataChanged = true;
      }
    }

  }
}
