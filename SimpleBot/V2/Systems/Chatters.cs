using Timer = System.Threading.Timer;

namespace SimpleBot.v2
{
    class Chatters
    {
        const int SAVE_INTERVAL_MS = 600000;

        readonly object _lock = new();
        readonly Bot _bot;
        readonly Dictionary<string, Chatter> _chatters = [];
        readonly string _chattersDataPath;
        readonly Timer _saveFileTimer;

        public Chatters(Bot bot)
        {
            _bot = bot;
            _chattersDataPath = bot.UserPath("chatters") + "\\";
            Directory.CreateDirectory(_chattersDataPath);
            _saveFileTimer = new Timer(_ => ForceSave(), null, SAVE_INTERVAL_MS, SAVE_INTERVAL_MS);
        }

        public void ForceSave()
        {
            lock (_lock)
            {
                _save_noLock();
            }
        }

        public void _save_noLock()
        {
#if DEBUG
      return;
#endif
            // TODO
        }

        public Chatter GetOrNull(string canonicalName)
        {
            lock (_lock)
            {
                return _chatters.TryGetValue(canonicalName, out var x) ? x : null;
            }
        }

        public Chatter Get(string canonicalName)
        {
            lock (_lock)
            {
                if (_chatters.TryGetValue(canonicalName, out var x))
                    return x;
                x = new Chatter { name = canonicalName };
                _chatters.Add(canonicalName, x);
                return x;
            }
        }

        public void ForEach(IEnumerable<string> canonicalNames, Action<Chatter> action)
        {
            lock (_lock)
            {
                foreach (var name in canonicalNames)
                {
                    if (!_chatters.TryGetValue(name, out Chatter chatter))
                        _chatters.Add(name, chatter = new() { name = name });
                    action(chatter);
                }
            }
        }

        public Chatter RandomChatter()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
