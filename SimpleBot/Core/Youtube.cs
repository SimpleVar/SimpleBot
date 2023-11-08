using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using VideoLibrary;
using VideoLibrary.Exceptions;

namespace SimpleBot
{
  class Youtube
  {
    public struct YtVideo
    {
      public string id, title, duration;
    }

    const int BUFF_SIZE = 1024;

    public WebView2 webView;
    public event EventHandler<string> VideoEnded = delegate { };

    private readonly object _webViewInitLock = new();
    public bool IsWebViewInitialized { get; private set; }
    private event EventHandler WebViewInitialized = delegate { };

    public void RegisterInitialized(EventHandler action)
    {
      lock (_webViewInitLock)
      {
        if (IsWebViewInitialized)
          action.Invoke(this, null);
        else
          WebViewInitialized += action;
      }
    }

    readonly HttpClient _web;
    readonly Client<YouTubeVideo> _yt;
    readonly byte[] _buff = new byte[BUFF_SIZE];
    readonly List<YtVideo> _results = new();

    public Youtube()
    {
      var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
      _web = new HttpClient(handler);
      _yt = Client.For(YouTube.Default);
    }

    public Task Init()
    {
      //var env = await CoreWebView2Environment.CreateAsync(null, null, new("--allow-insecure-localhost --unsafely-treat-insecure-origin-as-secure=about:blank"));
      //await webView.EnsureCoreWebView2Async(env);
      return Extensions.RunThreadSTA(async () =>
      {
        webView = new WebView2 { Dock = DockStyle.Fill };
        webView.CoreWebView2InitializationCompleted += (o, e) =>
        {
          if (e.InitializationException != null)
          {
            Bot.Log("[yt] ERR: WebView2 core mega bullshit init error: " + e.InitializationException.Message);
            throw e.InitializationException;
          }
          Bot.Log("[yt] WebView2 core mega bullshit initialized");

          webView.CoreWebView2.DOMContentLoaded += (o, e) =>
          {
            // inject the html source into google so that youtube likes as and plays embedded videos
            _ = webView.ExecuteScriptAsync(@"document.body.innerHTML = '<div id=player></div>';
const scr = document.createElement('script');
scr.innerHTML = `function onYouTubeIframeAPIReady() {
  const player = new YT.Player('player', {
    height: '100%',
    width: '100%',
    playerVars: { 'autoplay': 1, 'controls': 1 },
    events: {
      'onReady': () => { document.ytPlayer = player; player.setVolume(0); window.chrome.webview.postMessage('loaded baby'); },
      'onStateChange': e => { if (e.data === 0) skipMe(); },
      'onError': e => { console.log('ytErr', e); if (e.data < 101) skipMe(); }
    }
  });
}
function pauseOrResume() {
  const p = document.ytPlayer;
  if (!p) return;
  const isPaused = p.getPlayerState() == 2;
  p[isPaused ? 'playVideo' : 'pauseVideo']();
  return +isPaused;
}
function skipMe() {
  window.chrome.webview.postMessage(document._loadedVideoId ?? '');
}
function playNow(id, start, end) {
  document._loadedVideoId = id
  document.ytPlayer?.loadVideoById(id, start, end)
}
// skip ads (TODO see if works?)
// https://github.com/0x48piraj/fadblock/blob/master/src/chrome/js/background.js
setInterval(() => {
  const videoContainer = document.querySelector("".html5-video-player"");
  const isAd = videoContainer?.classList.contains(""ad-interrupting"") || videoContainer?.classList.contains(""ad-showing"");
  const skipLock = document.querySelector("".ytp-ad-preview-text"")?.innerText;
  const surveyLock = document.querySelector("".ytp-ad-survey"")?.length > 0;

  if (isAd && skipLock) {
    console.log('skipping ad...');
    const videoPlayer = document.getElementsByClassName(""video-stream"")[0];
    videoPlayer.muted = true; // videoPlayer.volume = 0;
    videoPlayer.currentTime = videoPlayer.duration - 0.1;
    videoPlayer.paused && videoPlayer.play()
    // CLICK ON THE SKIP AD BTN
    document.querySelector("".ytp-ad-skip-button"")?.click();
  } else if (isAd && surveyLock) {
    console.log('skipping ad...');
    // CLICK ON THE SKIP SURVEY BTN
    document.querySelector("".ytp-ad-skip-button"")?.click();
  }
}, 100);
// setInterval(() => {
//   const videoContainer = document.querySelector("".html5-video-player"");
//   const isErr = videoContainer?.classList.contains(""ytp-embed-error"");
//   console.log(isErr)
//   if (isErr) {
//     console.log('trying to skip embed err...');
//     playNow(document._loadedVideoId)
//   }
// }, 100)`;
document.body.append(scr);
document.body.style.overflow = 'hidden';
const tag = document.createElement('script');
tag.src = 'https://www.youtube.com/iframe_api';
document.body.append(tag);");
          };

          webView.WebMessageReceived += (o, e) =>
          {
            switch (e.WebMessageAsJson)
            {
              case "\"loaded baby\"":
                if (!IsWebViewInitialized)
                {
                  lock (_webViewInitLock)
                  {
                    IsWebViewInitialized = true;
                    WebViewInitialized.Invoke(this, null);
                  }
                }
                break;
              default:
                VideoEnded.Invoke(this, e.WebMessageAsJson[1..^1]); // removes quotes from string values
                break;
            }
          };
          // now we are in an https secured url, Muahahahahahahahhahahahqahahahadhahssdhakjsawdg
          webView.CoreWebView2.Navigate("https://www.google.com");
        };
        await webView.EnsureCoreWebView2Async();
      });
    }

    public Task<string> PauseOrResume()
    {
      return webView?.Invoke(() => webView.ExecuteScriptAsync($"pauseOrResume()").LogErr());
    }

    Form _ytViewForm;
    public void ShowOrHide(IWin32Window parentWindow, Action<bool> onVisibilityChanged)
    {
      webView?.Invoke(() =>
      {
        if (_ytViewForm == null)
        {
          _ytViewForm = new Form
          {
            ClientSize = new Size(640, 390),
            ShowIcon = false,
#if false // dont set these properties if you want OBS to be able to capture the video player
            FormBorderStyle = FormBorderStyle.SizableToolWindow,
            ShowInTaskbar = false,
#endif
            Text = "SimpleBot - Youtube view"
          };
          _ytViewForm.FormClosing += (o, e) =>
          {
            if (e.CloseReason == CloseReason.UserClosing)
            {
              e.Cancel = true;
              _ytViewForm.Hide();
            }
          };
          _ytViewForm.VisibleChanged += (o, e) => onVisibilityChanged(_ytViewForm.Visible);
          _ytViewForm.Controls.Add(webView);
        }

        if (_ytViewForm.Visible)
          _ytViewForm.Hide();
        else
          _ytViewForm.Show();
      });
    }

    public Task<string> SetVolume(int volume)
    {
      return webView?.Invoke(() => webView.ExecuteScriptAsync($"document.ytPlayer?.setVolume({volume})").LogErr());
    }

    public Task<string> PlayVideo(string videoId, int startSeconds = 0, int endSeconds = 0)
    {
      return webView?.Invoke(() => webView.ExecuteScriptAsync($"playNow('{videoId}', {(startSeconds > 0 ? startSeconds : "undefined")}, {(endSeconds > 0 ? endSeconds : "undefined")})").LogErr());
    }

    private string GetIdFromUrl(string url)
    {
      try
      {
        return HttpUtility.ParseQueryString(new Uri(new StringBuilder(url)
          .Replace("youtu.be/", "youtube.com/watch?v=")
          .Replace("youtube.com/embed/", "youtube.com/watch?v=")
          .Replace("/v/", "/watch?v=")
          .Replace("/watch#", "/watch?")
          .Replace("youtube.com/shorts/", "youtube.com/watch?v=")
          .ToString()).Query)["v"];
      }
      catch
      {
        return null;
      }
    }

    public async Task Search(string query, List<YtVideo> results, int maxResults)
    {
      var couldBeId = true;
      for (int i = 0; couldBeId && i < query.Length; i++)
      {
        char c = query[i];
        couldBeId = c == '-' || c == '_' || char.IsLetterOrDigit(c);
      }

      YouTubeVideo video = null;
      string videoId = query;
      if (couldBeId)
        video = tryGetVideo("youtube.com/watch?v=" + query);
      else
      {
        videoId = GetIdFromUrl(query);
        if (videoId != null)
          video = tryGetVideo(query);
      }
      
      if (video != null)
      {
        results.Add(new()
        {
          id = videoId,
          title = video.Title,
          duration = TimeSpan.FromSeconds(video.Info.LengthSeconds ?? 0).ToShortDurationString()
        });
        return;
      }

      using Stream s = await _web.GetStreamAsync("https://www.youtube.com/results?sp=EgIQAQ%253D%253D&search_query=" + HttpUtility.UrlEncode(query));
      parseSearchResults(s, results, maxResults);
    }

    public async Task<YtVideo?> Search(string query)
    {
      _results.Clear();
      await Search(query, _results, 1);
      return _results.Count == 0 ? null : _results[0];
    }

    private YouTubeVideo tryGetVideo(string url)
    {
      try
      {
        return _yt.GetVideo(url);
      }
      catch (ArgumentException) { }
      catch (UnavailableStreamException) { }
      return null;
    }

    // JSON PARSING-NOT-PARSING:

    static int peekByte(ref ParseState s)
    {
      if (s.buffIdx < s.buffLen)
        return s.buff[s.buffIdx];

      int b = readByte(ref s);
      s.buffIdx = 0;
      return b;
    }

    static int readByte(ref ParseState s)
    {
      if (s.buffIdx < s.buffLen)
        return s.buff[s.buffIdx++];

      s.buffLen = s.sourceStream.Read(s.buff, 0, s.buff.Length);
      if (s.buffLen == 0)
        return 0;
      s.buffIdx = 1;
      return s.buff[0];
    }

    static bool nextMatch(string searchToken, ref ParseState s)
    {
      if (searchToken.IndexOf(searchToken[0], 1) != -1)
        throw new ApplicationException("Assumption doesn't hold"); // the token's first char should not appear in it later, to have a simple search
      bool foundToken = false;
      while (!foundToken)
      {
        foundToken = true;
        for (int i = 0; i < searchToken.Length; i++)
        {
          int b = readByte(ref s);
          if (b == 0) return false;
          if (b != searchToken[i])
          {
            foundToken = false;
            break;
          }
        }
      }
      return foundToken;
    }

    static void readAndExpect(ref ParseState s, char c)
    {
      if (readByte(ref s) != c)
        throw new ApplicationException("oh no");
    }

    static void skipSpace(ref ParseState s)
    {
      while (peekByte(ref s) is ' ' or '\t')
        readByte(ref s);
    }

    static void skipSpaceOrComma(ref ParseState s)
    {
      while (peekByte(ref s) is ' ' or '\t' or ',')
        readByte(ref s);
    }

    // ; TOP-LEVEL
    void parseSearchResults(Stream stream, List<YtVideo> results, int maxResults)
    {
      ParseState s = new()
      {
        sourceStream = stream,
        buff = _buff,
        results = results,
        buffLen = 0,
        buffIdx = 0,
        currVideo = new(),
        phase = Phase.TheDoNothingState, // wait for itemSectionRenderer
        depth = 0,
        videoRendererDepth = 0,
        maxResults = maxResults,
      };

      if (!nextMatch("ytInitialData = ", ref s))
        return;

      if (readByte(ref s) != '{')
        throw new ApplicationException("Huh?");

      parse_jobj(ref s);
    }

    unsafe void parse_quoted(ref ParseState s)
    {
      s.quotedBuffLen = 0;
      int c = 0;
      while (c != '"')
      {
        switch (c = readByte(ref s))
        {
          case '\\': _ = readByte(ref s); break; // The quoted buff will contain zero instead of escaped chars
          case '"': break;
          default:
            if (s.quotedBuffLen < ParseState.QUOTED_BUFF_SIZE)
              s.quotedBuff[s.quotedBuffLen++] = (byte)c;
            break;
        }
      }
      s.quotedBuff[s.quotedBuffLen] = 0;
    }

    void parse_jval(ref ParseState s)
    {
      // ; <quoted>
      // ; <jobj>
      // ; <jarr>
      // ; <literal>

      skipSpace(ref s);
      switch (readByte(ref s))
      {
        case '"': parse_quoted(ref s); return;
        case '{': parse_jobj(ref s); return;
        case '[': parse_jarr(ref s); return;
        default:
          // literal - anything until ]},
          while (!(peekByte(ref s) is ']' or '}' or ','))
            _ = readByte(ref s);
          return;
      }
    }

    void parse_jarr(ref ParseState s)
    {
      // ; [<jval> ,]+  ']'

      while (s.results.Count < s.maxResults)
      {
        if (peekByte(ref s) == 0)
        {
          Bot.Log("[ytSearch] Unexpected EOF in " + nameof(parse_jarr));
          return;
        }

        skipSpaceOrComma(ref s);
        if (peekByte(ref s) == ']')
        {
          _ = readByte(ref s);
          return;
        }
        parse_jval(ref s);
      }
    }

    void parse_jobj(ref ParseState s)
    {
      // ; <quoted> : <jval> [, <quoted> : <jval>]+ }

      s.depth++;
      while (s.results.Count < s.maxResults)
      {
        if (peekByte(ref s) == 0)
        {
          Bot.Log("[ytSearch] Unexpected EOF in " + nameof(parse_jobj));
          return;
        }
        skipSpace(ref s);
        switch (readByte(ref s))
        {
          case '}': s.depth--; return;
          case ',': break;
          case '"':
            parse_quoted(ref s);
            skipSpace(ref s);
            readAndExpect(ref s, ':');
            var outPhase = s.phase; // phase coming in, is the phase coming out (mostly, there is Finished_attr state)
            switch (s.phase)
            {
              case Phase.TheDoNothingState:
                // we only care for videoRenderes that appear under itemSectionRenderer
                if (s.IsQuotedEqualTo("itemSectionRenderer"))
                  s.phase = Phase.LookingFor_videoRenderer;
                break;
              case Phase.LookingFor_videoRenderer:
                if (s.IsQuotedEqualTo("videoRenderer"))
                {
                  s.phase = Phase.LookingFor_videoRenderer_attr;
                  s.videoRendererDepth = s.depth;
                }
                else if (s.IsQuotedEqualTo("ads") || s.IsQuotedEqualTo("shelfRenderer"))
                {
                  // these lil shits contain videoRenderes that must be ignored
                  s.phase = Phase.TheDoNothingState;
                }
                break;
              case Phase.LookingFor_videoRenderer_attr:
                if (s.depth == s.videoRendererDepth + 1)
                {
                  if (s.IsQuotedEqualTo("videoId"))
                    s.phase = Phase.In_videoRenderer_id;
                  else if (s.IsQuotedEqualTo("title"))
                    s.phase = Phase.In_videoRenderer_title;
                  else if (s.IsQuotedEqualTo("lengthText"))
                    s.phase = Phase.In_videoRenderer_duration;
                }
                break;
            }
            bool isAttrGood =
              (s.phase == Phase.In_videoRenderer_title && s.IsQuotedEqualTo("text")) ||
              (s.phase == Phase.In_videoRenderer_duration && s.IsQuotedEqualTo("simpleText"));
            parse_jval(ref s);
            switch (s.phase)
            {
              case Phase.In_videoRenderer_id:
                s.currVideo.id = s.BuildQuotedStr();
                break;
              case Phase.In_videoRenderer_title:
                if (isAttrGood)
                {
                  s.currVideo.title = s.BuildQuotedStr();
                  outPhase = Phase.Finished_videoRenderer_attr;
                }
                break;
              case Phase.In_videoRenderer_duration:
                if (isAttrGood)
                {
                  s.currVideo.duration = s.BuildQuotedStr();
                  outPhase = Phase.Finished_videoRenderer_attr;
                }
                break;
            }
            s.phase = outPhase;
            if (s.currVideo.id != null && s.currVideo.title != null && s.currVideo.duration != null)
            {
              s.results.Add(s.currVideo);
              s.currVideo.id = null;
              s.currVideo.title = null;
              s.currVideo.duration = null;
            }
            break;
        }
      }
    }

    enum Phase
    {
      TheDoNothingState,
      LookingFor_videoRenderer,
      LookingFor_videoRenderer_attr,
      Finished_videoRenderer_attr,
      In_videoRenderer_id,
      In_videoRenderer_title,
      In_videoRenderer_duration,
    }

    unsafe struct ParseState
    {
      public const int QUOTED_BUFF_SIZE = 127;
      public Stream sourceStream;
      public byte[] buff;
      public fixed byte quotedBuff[QUOTED_BUFF_SIZE + 1];
      public int quotedBuffLen;
      public int buffLen, buffIdx;
      public List<YtVideo> results;
      public YtVideo currVideo;
      public Phase phase;
      public int depth;
      public int videoRendererDepth;
      public int maxResults;

      public readonly bool IsQuotedEqualTo(string value)
      {
        if (value.Length > QUOTED_BUFF_SIZE)
          throw new ApplicationException("We need a bigger quoted buff size for that operation");
        for (int i = 0; i < value.Length; i++)
        {
          if (quotedBuff[i] != value[i])
            return false;
        }
        return true;
      }

      public string BuildQuotedStr()
      {
        quotedBuff[QUOTED_BUFF_SIZE] = 0; // safe-gaurd
        fixed (byte* s = quotedBuff)
        {
          return Encoding.Default.GetString(s, quotedBuffLen);
        }
      }
    }
  }
}
