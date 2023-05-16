namespace SimpleBot
{
  static class LearnHiragana
  {
    public static LongRunningPeriodicTask _task;

    public static void Init(Bot bot)
    {
#if DEBUG
      return;
#endif
      _task = LongRunningPeriodicTask.Start(0, false, 60000, 3000, 10000, async rid =>
      {
        var q = _questions[Rand.R.Next(_questions.Length)];
        // TODO? play an alert, and read the answer?
        bot.TwSendMsg("▀▄▀▄▀▄ 𝐻𝒾𝓇𝒶𝑔𝒶𝓃𝒶 𝒫𝑜𝓅 𝒬𝓊𝒾𝓏 ▄▀▄▀▄▀ " + q.Q);
        await Task.Delay(15000);
        bot.TwSendMsg("▀▄▀▄▀▄ 𝐻𝒾𝓇𝒶𝑔𝒶𝓃𝒶 𝒜𝓃𝓈𝓌𝑒𝓇 ▄▀▄▀▄▀ " + q.A);
      });
    }

    struct QA
    {
      public string Q, A;
      public QA(string q, string a)
      {
        Q = q;
        A = a;
      }
    }
    static readonly QA[] _questions = new QA[]
    {
      new("ん", "n"),
      new("あ", "a"), new("い", "i"), new("う", "u"), new("え", "e"),new("お", "o"),
      new("か", "ka"), new("き", "ki"), new("く", "ku"), new("け", "ke"), new("こ", "ko"),
      new("さ", "sa"), new("し", "shi"), new("す", "su"), new("せ", "se"), new("そ", "so"),
      new("た", "ta"), new("ち", "chi"), new("つ", "tsu"), new("て", "te"), new("と", "to"),
      new("な", "na"), new("に", "ni"), new("ぬ", "nu"), new("ね", "ne"), new("の", "no"),
      new("は", "ha"), new("ひ", "hi"), new("ふ", "fu"), new("へ", "he"), new("ほ", "ho"),
      new("ま", "ma"), new("み", "mi"), new("む", "mu"), new("め", "me"), new("も", "mo"),
      new("や", "ya"), new("ゆ", "yu"), new("よ", "yo"),
      new("ら", "ra"), new("り", "ri"), new("る", "ru"), new("れ", "re"), new("ろ", "ro"),
      new("わ", "wa"), new("ゐ", "wi"), new("ゑ", "we"), new("を", "wo"),
      new("が", "ga"), new("ぎ", "gi"), new("ぐ", "gu"), new("げ", "ge"), new("ご", "go"),
      new("ざ", "za"), new("じ", "ji"), new("ず", "zu"), new("ぜ", "ze"), new("ぞ", "zo"),
      new("だ", "da"), new("ぢ", "ji"), new("づ", "zu"), new("で", "de"), new("ど", "do"),
      new("ば", "ba"), new("び", "bi"), new("ぶ", "bu"), new("べ", "be"), new("ぼ", "bo"),
      new("ぱ", "pa"), new("ぴ", "pi"), new("ぷ", "pu"), new("ぺ", "pe"), new("ぽ", "po"),
      new("じゃ", "ja"), new("じゅ", "ju"), new("じょ", "jo"),
      new("ぢゃ", "ja"), new("ぢゅ", "ju"), new("ぢょ", "jo"),
      new("きゃ", "kya"), new("きゅ", "kyu"), new("きょ", "kyo"),
      new("しゃ", "sha"), new("しゅ", "shu"), new("しょ", "sho"),
      new("ちゃ", "cha"), new("ちゅ", "chu"), new("ちょ", "cho"),
      new("にゃ", "nya"), new("にゅ", "nyu"),new("にょ", "nyo"),
      new("ひゃ", "hya"), new("ひゅ", "hyu"),new("ひょ", "hyo"),
      new("みゃ", "mya"), new("みゅ", "myu"),new("みょ", "myo"),
      new("りゃ", "rya"), new("りゅ", "ryu"),new("りょ", "ryo"),
      new("ぎゃ", "gya"), new("ぎゅ", "gyu"),new("ぎょ", "gyo"),
      new("びゃ", "bya"), new("びゅ", "byu"), new("びょ", "byo"),
      new("ぴゃ", "pya"), new("ぴゅ", "pyu"), new("ぴょ", "pyo"),
    };
  }
}
