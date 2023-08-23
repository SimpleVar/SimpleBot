using System.Globalization;

namespace SimpleBot.Core
{
  static class SongRequest
  {
    public struct Req
    {
      public string ogRequesterDisplayName, ytVideoId, title, duration;
      public int _shuffleRandVal; // used in tandom with Array.Sort
    }
    enum ReqResult { OK, AlreadyExists, TooManyOngoingRequestsByUser, TooShort, TooLong, FailedToParseDuration };
    public class SRData
    {
      public List<Req> Queue;
      public List<Req> Playlist;
      public int CurrIndexToPlayInPlaylist;
      public int Volume = 10; // 0-100
      public int MaxVolume = 30;
      public Req PrevSong;
      public Req CurrSong;

      public SRData BeValid()
      {
        Playlist ??= new();
        Queue ??= new();
        MaxVolume = Math.Max(0, Math.Min(100, MaxVolume));
        Volume = Math.Max(0, Math.Min(MaxVolume, Volume));
        return this;
      }
    }

    /// <summary>
    /// Fired when the current played song is changed, or anything is changed in the queue, or a song is added to the playlist
    /// Callbacks are executed in LOCKED context, so just read the data and get out!
    /// </summary>
    public static event EventHandler<SRData> NeedUpdateUI_SongList = delegate { };

    public static event EventHandler<(int volume, int maxVolume)> NeedUpdateUI_Volume = delegate { };

    #region User Settings

    static int _SR_videoMinLength_inSeconds = Settings.Default.SR_videoMinLength_inSeconds;
    public static int SR_videoMinLength_inSeconds
    {
      get => _SR_videoMinLength_inSeconds;
      set
      {
        Settings.Default.SR_videoMinLength_inSeconds = _SR_videoMinLength_inSeconds = value;
        Settings.Default.Save();
      }
    }
    static int _SR_videoMaxLength_inSeconds = Settings.Default.SR_videoMaxLength_inSeconds;
    public static int SR_videoMaxLength_inSeconds
    {
      get => _SR_videoMaxLength_inSeconds;
      set
      {
        Settings.Default.SR_videoMaxLength_inSeconds = _SR_videoMaxLength_inSeconds = value;
        Settings.Default.Save();
      }
    }
    static int _SR_maxOngoingRequestsBySameUser = Settings.Default.SR_maxOngoingRequestsBySameUser;
    public static int SR_maxOngoingRequestsBySameUser
    {
      get => _SR_maxOngoingRequestsBySameUser;
      set
      {
        Settings.Default.SR_maxOngoingRequestsBySameUser = _SR_maxOngoingRequestsBySameUser = value;
        Settings.Default.Save();
      }
    }

    #endregion

    static string _filePath;
    static Bot _bot;
    static SRData _sr = new SRData().BeValid();
    static readonly object _lock = new();
    public static Youtube _yt;

    public static void Load(string filePath)
    {
      _filePath = filePath;
      if (string.IsNullOrEmpty(_filePath))
        return;
      try
      {
        var json = File.ReadAllText(_filePath);
        SRData sr = json.FromJson<SRData>().BeValid();
        lock (_lock)
        {
          _sr = sr;
        }
      }
      catch { }
    }

    static void _save_noLock()
    {
#if DEBUG
      return;
#endif
      if (!string.IsNullOrEmpty(_filePath))
        return;
      var json = _sr.ToJson();
      try
      {
        File.WriteAllText(_filePath, json);
      }
      catch { }
    }

    public static async Task Init(Bot bot)
    {
      Bot.Log("[init] _yt");
      _bot = bot;
      _yt = new Youtube();
      _yt.RegisterInitialized(async (o, e) =>
      {
        try
        {
          _yt.VideoEnded += _yt_VideoEnded;
          await _SetVolume(_sr.Volume);
          string videoId = _sr.CurrSong.ytVideoId;
          if (videoId != null)
            await _yt.PlayVideo(videoId);
          else
            Next();
        }
        catch (Exception ex)
        {
          Bot.Log(ex.ToString());
        }
      });
      await _yt.Init();
    }

    private static void _yt_VideoEnded(object sender, string videoId)
    {
      if (videoId == _sr.CurrSong.ytVideoId)
        Next();
    }

    static void _onSongListChange_noLock()
    {
      _save_noLock();
      NeedUpdateUI_SongList(null, _sr);
      // TODO save changes to github
    }

    static void _saveToPlaylist_noLock(Req req)
    {
      var id = req.ytVideoId;
      for (int i = 0; i < _sr.Playlist.Count; i++)
        if (_sr.Playlist[i].ytVideoId == id)
          return;

      _sr.Playlist.Insert(0, req);
      _sr.CurrIndexToPlayInPlaylist++;
      _onSongListChange_noLock();
    }

    #region API

    public static void GetCurrSong(Chatter chatter)
    {
      string songTitle;
      lock (_lock)
      {
        songTitle = _sr.CurrSong.title;
      }
      _bot.TwSendMsg("Current song is: " + songTitle, chatter);
    }

    public static void GetPrevSong(Chatter chatter)
    {
      string songTitle;
      lock (_lock)
      {
        songTitle = _sr.PrevSong.title;
      }
      _bot.TwSendMsg("Previous song was: " + songTitle, chatter);
    }

    public static void GetVolume(Chatter chatter)
    {
      int vol, maxVol;
      lock (_lock)
      {
        (vol, maxVol) = (_sr.Volume, _sr.MaxVolume);
      }
      _bot.TwSendMsg("Volume is " + vol + " out of " + maxVol, chatter);
    }

    public static void SetVolume(int volume, Chatter chatter)
    {
      _ = Task.Run(async () =>
      {
        volume = await _SetVolume(volume);
        _bot.TwSendMsg("SeemsGood Volume set to " + volume, chatter);
      }).LogErr();
    }

    public static async Task<int> _SetVolume(int volume)
    {
      int ogVol = _sr.Volume;
      int vol, maxVol;
      lock (_lock)
      {
        _sr.Volume = volume;
        _sr.BeValid();
        //_save_noLock(); // avoid this save, its useless. The volume will be saved when a song changes
        (vol, maxVol) = (_sr.Volume, _sr.MaxVolume);
      }
      if (vol != ogVol)
      {
        await _yt.SetVolume(vol);
        NeedUpdateUI_Volume(null, (vol, maxVol));
      }
      return vol;
    }

    public static async Task _SetMaxVolume(int maxVolume)
    {
      int ogVol = _sr.Volume;
      int vol, maxVol;
      lock (_lock)
      {
        _sr.MaxVolume = maxVolume;
        _sr.BeValid();
        //_save_noLock(); // avoid this save, its useless. The volume will be saved when a song changes
        (vol, maxVol) = (_sr.Volume, _sr.MaxVolume);
      }
      if (vol != ogVol)
      {
        await _yt.SetVolume(vol);
      }
      NeedUpdateUI_Volume(null, (vol, maxVol));
    }

    public static void SaveCurrSongToPlaylist()
    {
      lock (_lock)
      {
        _saveToPlaylist_noLock(_sr.CurrSong);
      }
    }

    public static void SavePrevSongToPlaylist()
    {
      lock (_lock)
      {
        _saveToPlaylist_noLock(_sr.PrevSong);
      }
    }

    public static void ShufflePlaylist()
    {
      lock (_lock)
      {
        var N = _sr.Playlist.Count;
        for (int i = 0; i < N; i++)
        {
          var req = _sr.Playlist[i];
          req._shuffleRandVal = Rand.R.Next(N);
          _sr.Playlist[i] = req;
        }
        _sr.Playlist.Sort((a, b) => a._shuffleRandVal - b._shuffleRandVal);
        _onSongListChange_noLock();
      }
    }

    public static void Next()
    {
      string videoId = null;
      lock (_lock)
      {
        _sr.PrevSong = _sr.CurrSong;
        if (_sr.Queue.Count > 0)
        {
          _sr.CurrSong = _sr.Queue[0];
          _sr.Queue.RemoveAt(0);
        }
        else if (_sr.Playlist.Count > 0)
        {
          _sr.CurrIndexToPlayInPlaylist++;
          _sr.CurrIndexToPlayInPlaylist %= Math.Max(1, _sr.Playlist.Count);
          _sr.CurrSong = _sr.Playlist[_sr.CurrIndexToPlayInPlaylist];
        }
        else
          _sr.CurrSong = new();

        _onSongListChange_noLock();
        videoId = _sr.CurrSong.ytVideoId;
      }
      if (videoId != null)
        _ = Task.Run(async () => await _yt.PlayVideo(videoId)).LogErr();
    }

    static ReqResult AddToQueue(Req r, bool isStreamerRequest)
    {
      int maxReqsByUser = int.MaxValue;
      if (!isStreamerRequest)
      {
        if (!TimeSpan.TryParse(r.duration, CultureInfo.InvariantCulture, out TimeSpan dur))
          return ReqResult.FailedToParseDuration;
        if (dur.TotalSeconds < _SR_videoMinLength_inSeconds)
          return ReqResult.TooShort;
        if (dur.TotalSeconds > _SR_videoMaxLength_inSeconds)
          return ReqResult.TooLong;
        maxReqsByUser = _SR_maxOngoingRequestsBySameUser;
      }
      lock (_lock)
      {
        int reqsByUser = 0;
        for (int i = 0; i < _sr.Queue.Count; i++)
        {
          if (_sr.Queue[i].ytVideoId == r.ytVideoId)
            return ReqResult.AlreadyExists;
          if (_sr.Queue[i].ogRequesterDisplayName == r.ogRequesterDisplayName && ++reqsByUser > maxReqsByUser)
            return ReqResult.TooManyOngoingRequestsByUser;
        }

        _sr.Queue.Add(r);
        _onSongListChange_noLock();
      }
      return ReqResult.OK;
    }

    public static void RequestSong(string query, Chatter chatter)
    {
      _ = Task.Run(async () =>
      {
        var response = await _RequestSong(query, chatter.DisplayName);
        _bot.TwSendMsg(response, chatter);
      }).LogErr();
    }

    /// <summary>
    /// A null requestedBy means the request is by the streamer, and ignore limitations
    /// </summary>
    /// <param name="query"></param>
    /// <param name="requestedBy"></param>
    /// <returns></returns>
    public static async Task<string> _RequestSong(string query, string requestedBy)
    {
      if (!_yt.IsWebViewInitialized)
        return "SongRequest is not initialized";

      if (await _yt.Search(query) is not Youtube.YtVideo video)
        return "No video found for: " + query;

      var isStreamerRequest = string.IsNullOrWhiteSpace(requestedBy);
      var res = AddToQueue(new Req
      {
        ytVideoId = video.id,
        title = video.title,
        duration = video.duration,
        ogRequesterDisplayName = isStreamerRequest ? _bot.CHANNEL : requestedBy
      }, isStreamerRequest);

      return res switch
      {
        ReqResult.OK => $"Added #{_sr.Queue.Count} ({video.duration}): {video.title}",
        ReqResult.AlreadyExists => $"\"{video.title}\" is already in the queue",
        ReqResult.TooManyOngoingRequestsByUser => "You have enough requests already in the queue",
        ReqResult.TooShort => "The video is too short D:",
        ReqResult.TooLong => "The video is too long D:",
        _ => "Error: " + res,
      };
    }

    #endregion
  }
}
