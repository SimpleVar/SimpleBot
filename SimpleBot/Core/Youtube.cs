using System.Net;
using System.Web;

namespace SimpleBot
{
  class Youtube
  {
    public struct YtVideo
    {
      public string id, title, duration;
    }

    const int BUFF_SIZE = 1024;

    readonly HttpClient web;
    readonly byte[] _buff = new byte[BUFF_SIZE];
    readonly List<YtVideo> _results = new();

    public Youtube()
    {
      var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
      web = new HttpClient(handler);
    }

    public void Search(string query, List<YtVideo> results, int maxResults)
    {
      // TODO handle youtube urls
      // TODO how to get the DATA from just the id? hit the actual api at the cost of 1 UNITTT?!?!
      using Stream s = web.GetStreamAsync("https://www.youtube.com/results?sp=EgIQAQ%253D%253D&search_query=" + HttpUtility.UrlEncode(query)).Result;
      parseSearchResults(s, results, maxResults);
    }

    public YtVideo? Search(string query)
    {
      _results.Clear();
      Search(query, _results, 1);
      return _results.Count == 0 ? null : _results[0];
    }

    // JSON PARSING-NOT-PARSING:

    int peekByte(ref ParseState s)
    {
      if (s.buffIdx < s.buffLen)
        return s.buff[s.buffIdx];

      int b = readByte(ref s);
      s.buffIdx = 0;
      return b;
    }

    int readByte(ref ParseState s)
    {
      if (s.buffIdx < s.buffLen)
        return s.buff[s.buffIdx++];

      s.buffLen = s.sourceStream.Read(s.buff, 0, s.buff.Length);
      if (s.buffLen == 0)
        return 0;
      s.buffIdx = 1;
      return s.buff[0];
    }

    bool nextMatch(string searchToken, ref ParseState s)
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

    void readAndExpect(ref ParseState s, char c)
    {
      if (readByte(ref s) != c)
        throw new ApplicationException("oh no");
    }

    void skipSpace(ref ParseState s)
    {
      while (peekByte(ref s) is ' ' or '\t')
        readByte(ref s);
    }

    void skipSpaceOrComma(ref ParseState s)
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
      int qLen = 0;
      int c = 0;
      while (c != '"')
      {
        switch (c = readByte(ref s))
        {
          case '\\': _ = readByte(ref s); break; // The quoted buff will contain zero instead of escaped chars
          case '"': break;
          default:
            if (qLen < ParseState.QUOTED_BUFF_SIZE)
              s.quotedBuff[qLen++] = (sbyte)c;
            break;
        }
      }
      s.quotedBuff[qLen] = 0;
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
      public fixed sbyte quotedBuff[QUOTED_BUFF_SIZE + 1];
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
        fixed (sbyte* s = quotedBuff)
        {
          return new string(s);
        }
      }
    }
  }
}
