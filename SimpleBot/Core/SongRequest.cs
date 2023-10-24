using System.Configuration;
using System.Globalization;
using System.Text.Json.Serialization;
using VideoLibrary;

namespace SimpleBot.Core
{
  static class SongRequest
  {
    public struct Req
    {
      public string ogRequesterDisplayName, ytVideoId, title, duration;
      [JsonIgnore]
      public int _shuffleRandVal; // used in tandom with Array.Sort
    }
    enum ReqResult { OK, AlreadyExists, TooManyOngoingRequestsByUser, TooShort, TooLong, FailedToParseDuration };
    public class SRData
    {
      public List<Req> Queue;
      public List<Req> Playlist;
      public int CurrIndexToPlayInPlaylist;
      public Req PrevSong;
      public Req CurrSong;

      public SRData BeValid()
      {
        Playlist ??= new();
        Queue ??= new();
        return this;
      }
    }

    /// <summary>
    /// Fired when the current played song is changed, or anything is changed in the queue, or a song is added to the playlist
    /// Callbacks are executed in LOCKED context, so just read the data and get out!
    /// </summary>
    public static event EventHandler<SRData> NeedUpdateUI_SongList;

    public static event EventHandler<(int volume, int maxVolume)> NeedUpdateUI_Volume;

    #region User Settings

    public const int VOL_MIN = 0, VOL_MAX = 100;
#if DEBUG
    static int _SR_volume = 0;
#else
    static int _SR_volume = Settings.Default.SR_volume; // 0-100
#endif
    static int _SR_maxVolume = Settings.Default.SR_maxVolume; // 0-100
    static int _SR_minDuration_inSeconds = Settings.Default.SR_minDuration_inSeconds;
    static int _SR_maxDuration_inSeconds = Settings.Default.SR_maxDuration_inSeconds;
    static int _SR_maxSongsInQueuePerUser = Settings.Default.SR_maxSongsInQueuePerUser;
    public static int SR_volume
    {
      get => _SR_volume;
      set
      {
        if (_SR_volume == value) return;
        Settings.Default.SR_volume = _SR_volume = value;
        Settings.Default.Save();
      }
    }
    public static int SR_maxVolume
    {
      get => _SR_maxVolume;
      set
      {
        if (_SR_maxVolume == value) return;
        Settings.Default.SR_maxVolume = _SR_maxVolume = value;
        Settings.Default.Save();
      }
    }
    public static int SR_minDuration_inSeconds
    {
      get => _SR_minDuration_inSeconds;
      set
      {
        if (_SR_minDuration_inSeconds == value) return;
        Settings.Default.SR_minDuration_inSeconds = _SR_minDuration_inSeconds = value;
        Settings.Default.Save();
      }
    }
    public static int SR_maxDuration_inSeconds
    {
      get => _SR_maxDuration_inSeconds;
      set
      {
        if (_SR_maxDuration_inSeconds == value) return;
        Settings.Default.SR_maxDuration_inSeconds = _SR_maxDuration_inSeconds = value;
        Settings.Default.Save();
      }
    }
    public static int SR_maxSongsInQueuePerUser
    {
      get => _SR_maxSongsInQueuePerUser;
      set
      {
        if (_SR_maxSongsInQueuePerUser == value) return;
        Settings.Default.SR_maxSongsInQueuePerUser = _SR_maxSongsInQueuePerUser = value;
        Settings.Default.Save();
      }
    }

#endregion

    static string _filePath;
    static Bot _bot;
    static SRData _sr = new SRData().BeValid();
    static readonly object _lock = new();
    public static Youtube _yt;

    static void _beValid_noLock()
    {
      SR_maxVolume = Math.Max(0, Math.Min(100, _SR_maxVolume));
      SR_volume = Math.Max(0, Math.Min(_SR_maxVolume, _SR_volume));
      _sr.BeValid();
    }

    public static void Load(string filePath)
    {
      _filePath = filePath;
      if (string.IsNullOrEmpty(_filePath))
        return;
      try
      {
        var json = File.ReadAllText(_filePath);
        SRData sr = json.FromJson<SRData>();
        lock (_lock)
        {
          _sr = sr;
          _beValid_noLock();
        }
      }
      catch { }
    }

    static void _save_noLock()
    {
#if DEBUG
      return;
#endif
      if (string.IsNullOrEmpty(_filePath))
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
          await _SetVolume(_SR_volume, true);
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
      NeedUpdateUI_SongList?.Invoke(null, _sr);
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

    public static Req[] GetPlaylist()
    {
      lock (_lock)
      {
        return _sr.Playlist.ToArray();
      }
    }

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
      var (vol, maxVol) = _GetVolume();
      _bot.TwSendMsg("Volume is " + vol + " out of " + maxVol, chatter);
    }

    public static (int vol, int maxVol) _GetVolume()
    {
      lock (_lock)
      {
        return (_SR_volume, _SR_maxVolume);
      }
    }

    public static void SetVolume(int volume, Chatter chatter)
    {
      _ = Task.Run(async () =>
      {
        volume = await _SetVolume(volume);
        _bot.TwSendMsg("SeemsGood Volume set to " + volume, chatter);
      }).LogErr();
    }

    public static async Task<int> _SetVolume(int volume, bool forceUpdate = false)
    {
      volume = Math.Max(VOL_MIN, Math.Min(VOL_MAX, volume));
      int ogVol = _SR_volume;
      if (ogVol == volume && !forceUpdate)
        return volume;
      int vol, maxVol;
      lock (_lock)
      {
        SR_volume = volume;
        _beValid_noLock();
        //_save_noLock(); // avoid this save, its useless. The volume will be saved when a song changes
        (vol, maxVol) = (_SR_volume, _SR_maxVolume);
      }
      if (vol != ogVol || forceUpdate)
      {
        await _yt.SetVolume(vol);
        NeedUpdateUI_Volume?.Invoke(null, (vol, maxVol));
      }
      return vol;
    }

    public static async Task<int> _SetMaxVolume(int maxVolume)
    {
      maxVolume = Math.Max(VOL_MIN, Math.Min(VOL_MAX, maxVolume));
      if (_SR_maxVolume == maxVolume)
        return maxVolume;
      int ogVol = _SR_volume;
      int vol, maxVol;
      lock (_lock)
      {
        SR_maxVolume = maxVolume;
        _beValid_noLock();
        _save_noLock();
        (vol, maxVol) = (_SR_volume, _SR_maxVolume);
      }
      if (vol != ogVol)
      {
        await _yt.SetVolume(vol);
      }
      NeedUpdateUI_Volume?.Invoke(null, (vol, maxVol));
      return maxVol;
    }

    public static void RemoveManySongsFromPlaylist(HashSet<string> videoIds)
    {
      lock (_lock)
      {
        var removesBeforeNextToPlay = 0;
        var N = Math.Min(_sr.Playlist.Count, _sr.CurrIndexToPlayInPlaylist);
        for (int i = 0; i < N; i++)
        {
          if (videoIds.Contains(_sr.Playlist[i].ytVideoId))
            removesBeforeNextToPlay++;
        }
        _sr.CurrIndexToPlayInPlaylist -= removesBeforeNextToPlay;
        _sr.Playlist.RemoveAll(x => videoIds.Contains(x.ytVideoId));
      }
    }

    public static void RemoveSongFromPlaylist(string videoId)
    {
      lock (_lock)
      {
        for (int i = 0; i < _sr.Playlist.Count; i++)
        {
          if (_sr.Playlist[i].ytVideoId == videoId)
          {
            _sr.Playlist.RemoveAt(i);
            if (i < _sr.CurrIndexToPlayInPlaylist)
              _sr.CurrIndexToPlayInPlaylist--;
            return;
          }
        }
      }
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

    public static void ImportToPlaylist_nochecks(Req[] reqs)
    {
      /*
        // NightBot export:
        JSON.stringify( [...document.querySelectorAll('.ibox-content tbody tr')].map(r => {
          const tds = [...r.querySelectorAll('td')];
          return {ytVideoId: tds[0].querySelector('a').href.replace('https://youtu.be/', ''), title: tds[0].innerText, duration: tds[2].innerText};
        }) )
      */

      int importCount = 0;
      lock (_lock)
      {
        var s = _sr.Playlist.Select(r => r.ytVideoId).ToHashSet();
        var newReqs = reqs.Where(r => !s.Contains(r.ytVideoId)).ToArray();
        _sr.Playlist.AddRange(newReqs);
        importCount = newReqs.Length;
      }
      MessageBox.Show("Added " + importCount + " new songs to the playlist");
    }
    
    static ReqResult _addToQueue(Req r, bool isStreamerRequest)
    {
      int maxReqsByUser = int.MaxValue;
      if (!isStreamerRequest)
      {
        if (!TimeSpan.TryParse("0:" + r.duration, CultureInfo.InvariantCulture, out TimeSpan dur))
          return ReqResult.FailedToParseDuration;
        if (dur.TotalSeconds < _SR_minDuration_inSeconds)
          return ReqResult.TooShort;
        if (dur.TotalSeconds > _SR_maxDuration_inSeconds)
          return ReqResult.TooLong;
        maxReqsByUser = _SR_maxSongsInQueuePerUser;
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
      var res = _addToQueue(new Req
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
