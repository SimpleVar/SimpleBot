namespace SimpleBot
{
  static class ViewersQueue
  {
    const string EMPTY_QUEUE_MSG = "Queue is empty D:";

    struct Entry { public string DisplayName, ExtraText; }
    struct Q { public List<Entry> list; public bool isOpen; };

    static Q _q = new Q { list = new(), isOpen = true };
    static readonly object _lock = new();
    static string _filePath;

    public static void Load(string filePath)
    {
      lock (_lock)
      {
        _filePath = filePath;
        try
        {
          _q = File.ReadAllText(_filePath).FromJson<Q>();
          _q.list ??= new();
        }
        catch { }
      }
    }

    static void _save()
    {
#if DEBUG
      return;
#endif
      if (string.IsNullOrWhiteSpace(_filePath))
        return;
      try
      {
        File.WriteAllText(_filePath, _q.ToJson());
      }
      catch { }
    }

    public static void All(Bot bot, Chatter tagChatter)
    {
      string qStr;
      lock (_lock)
      {
        if (_q.list.Count == 0)
          qStr = EMPTY_QUEUE_MSG;
        else
          qStr = (_q.isOpen ? "Queue: " : "Queue (closed): ") + string.Join(", ", _q.list.Select(x => x.DisplayName));
      }
      bot.TwSendMsg(qStr, tagChatter);
    }

    public static void Clear(Bot bot)
    {
      lock (_lock)
      {
        _q.list.Clear();
        _save();
      }
      bot.TwSendMsg("Queue cleared");
    }

    public static void Close(Bot bot)
    {
      lock (_lock)
      {
        _q.isOpen = false;
        _save();
      }
      bot.TwSendMsg("Queue is closed D:");
    }

    public static void Open(Bot bot)
    {
      lock (_lock)
      {
        _q.isOpen = true;
        _save();
      }
      bot.TwSendMsg("Queue is OPEN! :D");
    }

    public static void Curr(Bot bot, Chatter tagChatter)
    {
      string response;
      lock (_lock)
      {
        if (_q.list.Count == 0)
        {
          response = EMPTY_QUEUE_MSG;
        }
        else
        {
          var e = _q.list[0];
          response = "Current in queue: " + e.DisplayName;
          if (e.ExtraText != null)
            response += " - " + e.ExtraText;
          if (_q.list.Count > 1)
            response += " | coming up: " + _q.list[1].DisplayName;
        }
      }
      bot.TwSendMsg(response, tagChatter);
    }

    public static void Join(Bot bot, Chatter chatter, string extraText)
    {
      string msg = null;
      lock (_lock)
      {
        for (int i = 0; i < _q.list.Count; i++)
        {
          if (_q.list[i].DisplayName == chatter.DisplayName)
          {
            msg = "You are already in the queue at #" + (i + 1);
            break;
          }
        }
        if (msg == null)
        {
          if (!_q.isOpen)
          {
            msg = "Can't join D: queue is closed";
          }
          else
          {
            _q.list.Add(new Entry { DisplayName = chatter.DisplayName, ExtraText = extraText });
            msg = "You joined the queue at #" + _q.list.Count;
            _save();
          }
        }
      }
      bot.TwSendMsg(msg, chatter);
    }

    public static void Leave(Bot bot, Chatter chatter)
    {
      string msg = null;
      lock (_lock)
      {
        for (int i = 0; i < _q.list.Count; i++)
        {
          if (_q.list[i].DisplayName == chatter.DisplayName)
          {
            msg = "You left the queue";
            _q.list.RemoveAt(i);
            _save();
            break;
          }
        }
        msg ??= "You are not in the queue";
      }
      bot.TwSendMsg(msg, chatter);
    }

    public static void Next(Bot bot)
    {
      string msg = null;
      lock (_lock)
      {
        if (_q.list.Count == 0)
          msg = EMPTY_QUEUE_MSG;
        else
        {
          _q.list.RemoveAt(0);
          _save();
          if (_q.list.Count == 0)
            msg = "Queue has emptied out";
          else
          {
            var e = _q.list[0];
            msg = "Next in queue: " + e.DisplayName;
            if (e.ExtraText != null)
              msg += " - " + e.ExtraText;
          }
        }
      }
      bot.TwSendMsg(msg);
    }
  }
}
