using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Media.Animation;
using VideoLibrary;
using VideoLibrary.Exceptions;

namespace SimpleBot
{
    class Youtube
    {
        public struct YtVideo
        {
            public string id, title, author, duration;
        }

        const int BUFF_SIZE = 1024;

        public WebView2 webView;
        public event EventHandler<string> VideoEnded = delegate { };
        public event EventHandler<(string vidId, string syncUTC)> VideoStarted = delegate { };

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
        private string _lastPlayedVideoId;

        public Youtube()
        {
            var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            _web = new HttpClient(handler);
            _yt = Client.For(YouTube.Default);
        }

        public Task Init(WebView2 existingWebView)
        {
            webView = existingWebView;
            webView.WebMessageReceived += (o, e) =>
            {
                var msg = e.WebMessageAsJson[1..^1]; // removes quotes from string value
                if (msg == "loaded baby")
                {
                    if (IsWebViewInitialized)
                        return;
                    lock (_webViewInitLock)
                    {
                        IsWebViewInitialized = true;
                        WebViewInitialized.Invoke(this, null);
                    }
                }
                else if (msg.StartsWith("[sync] "))
                {
                    var parts = msg.Split(' '); // sync <vidId> <syncUTC>
                    VideoStarted.Invoke(this, (parts[1], parts[2]));
                }
                else
                {
                    VideoEnded.Invoke(this, msg);
                }
            };
            webView.CoreWebView2.DOMContentLoaded += async (o, e) =>
            {
                // inject the html source into google so that youtube likes as and plays embedded videos
                _ = await webView.ExecuteScriptAsync(@"
const sanitizer = trustedTypes.createPolicy('foo', {createHTML:x=>x, createScriptURL:x=>x});
document.body.innerHTML = sanitizer.createHTML('<div id=player></div>')
document.documentElement.style.overflow = 'hidden'
window.onYouTubeIframeAPIReady = onYouTubeIframeAPIReady
let syncId = undefined
function onYouTubeIframeAPIReady() {
  const player = new YT.Player('player', {
    height: '100%',
    width: '100%',
    playerVars: { 'autoplay': 1, 'controls': 1 },
    events: {
      'onReady': () => { debugger; setInterval(checkAds, 50); document.ytPlayer = player; player.setVolume(0); window.chrome.webview.postMessage('loaded baby'); },
      'onStateChange': e => {
        if (e.data === 0) skipMe();
        if (e.data !== 1) return
        const currId = e.target?.getVideoData()?.video_id
        if (currId !== syncId) {
          syncId = currId
          window.chrome.webview.postMessage('[sync] ' + currId + ' ' + Date.now());
        }
      },
      'onError': e => { console.log('ytErr', e); if (e.data !== 2 && e.data < 101) skipMe(); }
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
let beQuite = false
function skipMe() {
  if (beQuite) return
  window.chrome.webview.postMessage(document._loadedVideoId ?? '');
}
function waitMs(ms) { return new Promise(res => setTimeout(res, ms)); }
function playNow(id, start, end) {
  start = start ? +start : 0
  beQuite = true
  document._loadedVideoId = id
  ;(async () => {
    //document.ytPlayer?.loadVideoById(id, 1, 2); await waitMs(50); document.ytPlayer?.loadVideoById(id, 2, 3); await waitMs(50)
    beQuite = false
    document.ytPlayer?.loadVideoById(id, start, end)
  })();
}
let currVolume = 0
function doSetVolume(vol) {
  vol = +vol
  currVolume = vol
  document.ytPlayer?.setVolume(vol)
}
function checkAds() {
  const p = document.ytPlayer
  if (!p) return
  const skipAd = player.contentWindow.document.body.querySelector('.ytp-ad-skip-button-text')
  const hasAds = skipAd || player.contentWindow.document.body.querySelector('.ytp-ad-player-overlay')
  if (!hasAds) {
    p.setVolume(currVolume)
    return
  }
  p.setVolume(0)
  skipAd?.click()
}
while (document.styleSheets.length) document.styleSheets[0].ownerNode.remove()
document.body.style.margin = '0';
document.body.style.width = '100vw';
document.body.style.height = '100vh';
document.body.style.overflow = 'hidden';
document.body.style.backgroundColor = '#111';
const tag = document.createElement('script');
tag.src = sanitizer.createScriptURL('https://www.youtube.com/iframe_api')
document.body.append(tag);
");
            };
            // now we are in an https secured url, Muahahahahahahahhahahahqahahahadhahssdhakjsawdg
            // specifically in youtube domain, to be able to read into the iframe bullshit, and look for elements (mini-ad-block)
            webView.Invoke(() => webView.CoreWebView2.Navigate("https://www.youtube.com"));
            return Task.CompletedTask;
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
            return webView?.Invoke(() => webView.ExecuteScriptAsync($"doSetVolume({volume})").LogErr());
        }

        public Task<string> PlayVideo(string videoId, int startSeconds = 0, int endSeconds = 0)
        {
            this._lastPlayedVideoId = videoId;
            return webView?.Invoke(() => webView.ExecuteScriptAsync($"playNow('{videoId}', {(startSeconds > 0 ? startSeconds : "undefined")}, {(endSeconds > 0 ? endSeconds : "undefined")})").LogErr());
        }

        public async Task<bool> PauseOrResume()
        {
            var paused = (await webView?.Invoke(() => webView.ExecuteScriptAsync($"pauseOrResume()").LogErr())) != "1";
            return paused;
        }

        private string GetIdFromUrl(string url)
        {
            try
            {
                url = new StringBuilder(url)
                  .Replace("http://", "")
                  .Replace("https://", "")
                  .Replace("youtu.be/", "youtube.com/watch?v=")
                  .Replace("youtube.com/embed/", "youtube.com/watch?v=")
                  .Replace("/v/", "/watch?v=")
                  .Replace("/watch#", "/watch?")
                  .Replace("youtube.com/shorts/", "youtube.com/watch?v=")
                  .ToString();
                return HttpUtility.ParseQueryString(new Uri("https://" + url).Query)["v"];
            }
            catch
            {
                return null;
            }
        }

        public async Task Search(string query, List<YtVideo> results, int maxResults)
        {
            query = query.Trim();
            var couldBeId = query.Length == 11; // YT video id is length 11
            for (int i = 0; couldBeId && i < query.Length; i++)
            {
                char c = query[i];
                couldBeId = c == '-' || c == '_' || char.IsLetterOrDigit(c);
            }

            {
                var i = query.IndexOf('&');
                if (i != -1)
                    query = query[..i];
            }

            YouTubeVideo video = null;
            string videoId = query;
            if (couldBeId)
                video = tryGetVideo("https://youtube.com/watch?v=" + query);
            if (video == null)
            {
                videoId = GetIdFromUrl(query);
                if (videoId != null)
                    video = tryGetVideo("https://youtube.com/watch?v=" + videoId);
            }

            if (video != null)
            {
                var author = video.Info.Author;
                var req = new YtVideo()
                {
                    id = videoId,
                    title = video.Title.ReduceWhitespace().Trim(),
                    duration = TimeSpan.FromSeconds(video.Info.LengthSeconds ?? 0).ToShortDurationString(),
                    author = author.Trim()
                };
                // hack
                if (req.author.EndsWith(" - topic", StringComparison.InvariantCultureIgnoreCase))
                    req.author = req.author[..^8];
                results.Add(req);
                return;
            }

            using Stream s = await _web.GetStreamAsync("https://www.youtube.com/results?sp=EgIQAQ%253D%253D&search_query=" + HttpUtility.UrlPathEncode(videoId ?? query));
            try
            {
                parseSearchResults(s, results, maxResults);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }
        }

        public async Task<YtVideo?> Search(string query)
        {
            List<YtVideo> res = [];
            await Search(query, res, 1);
            return res.Count == 0 ? null : res[0];
        }

        private YouTubeVideo tryGetVideo(string url)
        {
            try
            {
                return null; // YT is broken atm
                //return _yt.GetVideo(url);
            }
            catch (ArgumentException) { }
            catch (UnavailableStreamException) { }
            catch (InvalidOperationException ex)
            {
                // seems to happen when video is made private
                Debugger.Break();
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
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
            Span<byte> bytes = stackalloc byte[4];
            s.quotedBuffLen = 0;
            int c = 0;
            while (c != '"')
            {
                switch (c = readByte(ref s))
                {
                    case '\\':
                        switch (c = readByte(ref s))
                        {
                            case 'u':
                            case 'U':
                                for (int d = 0; d < 4; d++)
                                {
                                    if ((c = readByte(ref s)) == 0)
                                        goto end;
                                    bytes[d] = (byte)c;
                                }
                                if (!int.TryParse(bytes, System.Globalization.NumberStyles.HexNumber, null, out int utf32))
                                    goto end;
                                var ss = char.ConvertFromUtf32(utf32);
                                for (int i = 0; i < ss.Length; i++)
                                {
                                    if (s.quotedBuffLen < ParseState.QUOTED_BUFF_SIZE)
                                        s.quotedBuff[s.quotedBuffLen++] = (byte)ss[i];
                                }
                                c = ' ';
                                continue;
                            case 'n':
                            case 'v':
                            case 'h':
                            case 'f':
                            case 't':
                                c = ' ';
                                goto write;
                            case 'r':
                            case 'b':
                                c = ' ';
                                continue;
                            default:
                                goto write;
                        }
                        break;
                    case '"': break;
                    default:
                    write:
                        if (s.quotedBuffLen < ParseState.QUOTED_BUFF_SIZE)
                            s.quotedBuff[s.quotedBuffLen++] = (byte)c;
                        c = ' '; // we surely did not end the string, but may have written a literal "
                        break;
                }
            }
            end:
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
                                    else if (s.IsQuotedEqualTo("ownerText"))
                                        s.phase = Phase.In_videoRenderer_author;
                                    else if (s.IsQuotedEqualTo("lengthText"))
                                        s.phase = Phase.In_videoRenderer_duration;
                                }
                                break;
                        }
                        bool isAttrGood =
                          (s.phase == Phase.In_videoRenderer_title && s.IsQuotedEqualTo("text")) ||
                          (s.phase == Phase.In_videoRenderer_duration && s.IsQuotedEqualTo("simpleText")) ||
                          (s.phase == Phase.In_videoRenderer_author && s.IsQuotedEqualTo("text"));
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
                            case Phase.In_videoRenderer_author:
                                if (isAttrGood)
                                {
                                    s.currVideo.author = s.BuildQuotedStr();
                                    outPhase = Phase.Finished_videoRenderer_attr;
                                }
                                break;
                            case Phase.In_videoRenderer_duration:
                                if (isAttrGood)
                                {
                                    string dur = s.BuildQuotedStr();
                                    if (dur.Length > 2 && dur[0] == '0' && dur[1] != ':')
                                        dur = dur[1..];
                                    s.currVideo.duration = dur;
                                    outPhase = Phase.Finished_videoRenderer_attr;
                                }
                                break;
                        }
                        if (s.phase == Phase.LookingFor_videoRenderer_attr && outPhase == Phase.LookingFor_videoRenderer)
                        {
                            // id, title, dur are required for a song request. author is not required.
                            if (s.currVideo.id == null || s.currVideo.title == null || s.currVideo.duration == null)
                            {
                                Debugger.Break();
                                throw new ApplicationException("Missing video data after parsing, maybe a json attr name mismatch?");
                            }

                            s.results.Add(s.currVideo);
                            s.currVideo.id = null;
                            s.currVideo.title = null;
                            s.currVideo.author = null;
                            s.currVideo.duration = null;
                        }
                        s.phase = outPhase;
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
            In_videoRenderer_author,
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

            public bool IsQuotedEqualTo(string value)
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
                    return Encoding.UTF8.GetString(s, quotedBuffLen);
                }
            }
        }
    }
}
