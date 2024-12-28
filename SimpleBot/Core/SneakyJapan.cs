using System.Text;

namespace SimpleBot
{
  static class SneakyJapan
  {
    const int MS_BEFORE_FIRST_ROUND = 32000;
    const int MS_ROUND_DURATION = 60000;
    const int MS_AFTER_ROUND = 300000;
    const int MS_QUICKNESS_THRESHOLD = 3200;
    const int QUICKNESS_BUFF = 5;

    public static LongRunningPeriodicTask _task;
    static readonly object _lock = new();
    static Bot _bot;
    static long _currentRoundId;
    static bool _currentRoundOpen;
    static DateTime _currentRoundOpenTime;
    static readonly List<Chatter> _currentPlayers = new();

    public static void Init(Bot bot)
    {
      if (_bot != null)
        throw new ApplicationException("Init should be called exactly once");
      _bot = bot;
#if DEBUG
      return;
#endif
      _currentRoundId = long.TryParse(Settings.Default.SneakyJapanRound, out var sjr) ? sjr : 1;
      _task = LongRunningPeriodicTask.Start(_currentRoundId, true, MS_AFTER_ROUND, MS_BEFORE_FIRST_ROUND, MS_BEFORE_FIRST_ROUND,
        async rid =>
      {
        if (!_bot.IsOnline) return MS_BEFORE_FIRST_ROUND;
        if (!ChatActivity.GetActiveChatters(TimeSpan.FromMilliseconds(MS_AFTER_ROUND)).Any(x => x.name == "milesplayzchess" || x.name == "itsqueenliv") &&
            ChatActivity.GetActiveChatters(TimeSpan.FromMilliseconds(MS_AFTER_ROUND), maxChattersNeeded: 3).Count < 3)
        {
          Bot.Log("Sneaky Japan delayed due to inactive chat");
          return MS_ROUND_DURATION;
        }
        lock (_lock)
        {
          _currentRoundId = rid;
          _currentRoundOpen = true;
          _currentRoundOpenTime = DateTime.UtcNow;
          Settings.Default.SneakyJapanRound = rid + "";
          Settings.Default.Save();
        }
        // ROUND
        Bot.Log("Starting Sneaky Japan round: " + rid);
        bool extraSneak = ChatActivity.IsUserInChat("soultego");
        bot.TwSendMsg($"/me peepoJapan {(extraSneak?"VERY ":"")}Sneaky Japan is sneaking about! Try {bot.CMD_PREFIX}Japan and test your perception to spot it. Hurry! You only have one minute peepoJapan");
        await Task.Delay(MS_ROUND_DURATION);
        int d20s = extraSneak ? 2 : 1;
        int japanBuff = 10;
        int sneakRoll = japanBuff;
        for (int i = 0; i < d20s; i++)
          sneakRoll += Rand.R.Next(20) + 1;
        if (Rand.R.Next(100) == 0)
          sneakRoll = 0;
        var winners = new List<string>();
        lock (_lock)
        {
          _currentRoundOpen = false;
          if (_currentPlayers.Count > 0)
          {
            foreach (var chatter in _currentPlayers)
            {
              var japan = chatter.SneakyJapanStats;
              if ((japan?.LastRollRoundId ?? 0) != rid)
                continue;
              var expGain = 1;
              japan.RoundsPlayed++;
              if (japan.LastRoll + japan.TemporaryBuff < sneakRoll)
              {
                japan.WinStreak_Curr = 0;
              }
              else
              {
                japan.RoundsWon++;
                japan.WinStreak_Curr++;
                japan.WinStreak_Longest = Math.Max(japan.WinStreak_Curr, japan.WinStreak_Longest);
                expGain = 5;
                winners.Add(chatter.DisplayName);
              }
              japan.Exp += expGain;
              japan.TemporaryBuff = 0;
            }
            ChatterDataMgr.Update();
            _currentPlayers.Clear();
          }
        }
        if (winners.Count == 0)
          bot.TwSendMsg("/me peepoJapan This Japan was much too sneaky and could not be found by anyone D:");
        else
        {
          var winnersText = winners.Count == 1 ? winners[0] : winners.Count + " pro gamers!";
          bot.TwSendMsg($"/me peepoJapan This Japan wasn't sneaky enough and with brilliant observation was spotted by {winnersText} Clap The sneak roll was {sneakRoll}{(sneakRoll == 0 ? " LUL" : "")}");
        }
        return MS_AFTER_ROUND;
      });
    }

    public static void JapanStats(Chatter targetChatter)
    {
      var j = targetChatter.SneakyJapanStats;
      _bot.TwSendMsg($"{FullJapanName(targetChatter)} rounds played: {j.RoundsPlayed}, rounds won: {j.RoundsWon} ({(float)j.RoundsWon / j.RoundsPlayed:P1}), total crits: {j.TotalCrits}. Win streak: {j.WinStreak_Curr} (longest {j.WinStreak_Longest})");
    }

    public static void Japan(Chatter chatter)
    {
      lock (_lock)
      {
        var quickly = DateTime.UtcNow.Subtract(_currentRoundOpenTime).TotalMilliseconds < MS_QUICKNESS_THRESHOLD;
        var japan = chatter.SneakyJapanStats;
        var buff = CalcBuff(japan.Exp) + japan.NewGamePlus;
        var tagUser = FullJapanName(chatter);
        if (!_currentRoundOpen)
        {
          _bot.TwSendMsg(tagUser + " Your exp grants you a hidden bonus of " + buff + " for all future Sneaky Japans");
          return;
        }
        if (japan.LastRollRoundId == _currentRoundId)
        {
          _bot.TwSendMsg(tagUser + " You've already rolled a " + japan.LastRoll + " for this Sneaky Japan");
          return;
        }
        japan.LastRollRoundId = _currentRoundId;
        japan.LastRoll = japan.TemporaryBuff == 420 ? 20 : Rand.R.Next(20) + 1;
        bool crit = japan.LastRoll == 20;
        if (crit)
        {
          japan.LastRoll <<= 1;
          japan.TotalCrits++;
        }
        japan.LastRoll += buff;
        japan.LastRoll += quickly ? QUICKNESS_BUFF : 0;
        _currentPlayers.Add(chatter);
        _bot.TwSendMsg($"/me {tagUser} {(quickly ? "QUICKLY " : "")}rolled a {(crit ? "CRIT " : "")}{japan.LastRoll}{(crit ? " Kreygasm" : "")}{(japan.TemporaryBuff == 0 ? "" : " (and +" + japan.TemporaryBuff + " buff)")}");
      }
    }

    public static void Buff(Chatter chatter, int buff)
    {
      lock (_lock)
      {
        var newBuff = chatter.SneakyJapanStats.TemporaryBuff + buff;
        string accumulationStr = chatter.SneakyJapanStats.TemporaryBuff == 0 ? "" : $" (accumulated total: {newBuff})";
        chatter.SneakyJapanStats.TemporaryBuff = newBuff;
        _bot.TwSendMsg($"{FullJapanName(chatter)} gets a temporary +{buff} buff to your next perception check!" + accumulationStr);
      }
    }

    public static void MinusOneExp(Chatter chatter)
    {
        if (chatter.SneakyJapanStats.Exp == 1)
            return;
        ChatActivity.IncCommandCounter(chatter, BotCommandId.SneakyJapan_MinusOneExp);
        chatter.SneakyJapanStats.Exp--;
        ChatterDataMgr.Update();
        _bot.TwSendMsg($"{FullJapanName(chatter)} lost one exp point to the void");
    }

    public static void Do_NewGamePlus_Unchecked(Chatter chatter, string confirmationStr)
    {
      lock (_lock)
      {
        if (chatter.SneakyJapanStats.Exp < 10000)
        {
          _bot.TwSendMsg("Can't. Skill issue. Try when you have 10,000 exp 4Head", chatter);
          return;
        }
        const string CONFIRM = "doit";
        if (confirmationStr != CONFIRM)
        {
          _bot.TwSendMsg($"You may start fresh as a Weeb on NewGame+ ({chatter.SneakyJapanStats.NewGamePlus + 1}), if you are sure send '{_bot.CMD_PREFIX}{Bot._builtinCommandsAliases[BotCommandId.SneakyJapan_NewGamePlus][0]} {CONFIRM}'", chatter);
          return;
        }
        ChatActivity.IncCommandCounter(chatter, BotCommandId.SneakyJapan_Stats);
        chatter.SneakyJapanStats.Exp -= 10000;
        chatter.SneakyJapanStats.NewGamePlus++;
        ChatterDataMgr.Update();
        _bot.TwSendMsg($"svBEST peepoJapan MercyWing1 {FullJapanName(chatter)} MercyWing2 Congratulations on achieving NewGame+ ({chatter.SneakyJapanStats.NewGamePlus})!! peepoJapan SeemsGood peepoJapan PartyHat Kreygasm");
      }
    }

    public static void Do_Leaderboard()
    {
      var chatters = ChatterDataMgr.All();
      Array.Sort(chatters, (a, b) => b.SneakyJapanStats.GetRankingScore() - a.SneakyJapanStats.GetRankingScore());
      var sb = new StringBuilder("peepoJapan Japan top ten: ");
      for (int i = 0; i < 10 && i < chatters.Length; i++)
      {
        var j = chatters[i].SneakyJapanStats;
        sb.Append(chatters[i].DisplayName);
        if (j.NewGamePlus != 0)
          sb.Append(' ').Append('(').Append('+').Append(j.NewGamePlus).Append(')');
        sb.Append(' ').Append(j.Exp);
        sb.Append(" | ");
      }

      Chatter mostBuffedIndividual = null;
      int mostBuffValue = 0;
      foreach (var c in chatters)
      {
        if (c.SneakyJapanStats.TemporaryBuff > mostBuffValue)
        {
          mostBuffValue = c.SneakyJapanStats.TemporaryBuff;
          mostBuffedIndividual = c;
        }
      }
      if (mostBuffedIndividual != null)
        sb.Append("current biggest buff: ").Append(mostBuffedIndividual.DisplayName).Append(' ').Append('+').Append(mostBuffValue);

      _bot.TwSendMsg(sb.ToString());
    }

    static string FullJapanName(Chatter chatter) => $"{chatter.DisplayName} ({chatter.SneakyJapanStats.Exp} exp - {GetMasteryTitle(chatter.SneakyJapanStats.Exp)}{(chatter.SneakyJapanStats.NewGamePlus == 0 ? "" : " +" + chatter.SneakyJapanStats.NewGamePlus)})";
    static int CalcBuff(int exp) => Math.Min(20, (20 * exp) / 1000);
    static string GetMasteryTitle(int exp)
    {
      if (exp < 10)
        return "Weeb";
      if (exp < 25)
        return "Japan Guesser";
      if (exp < 50)
        return "Japan Tourist";
      if (exp < 100)
        return "Japan Explorer";
      if (exp < 200)
        return "Japan Pro Traveler";
      if (exp < 300)
        return "Sneaky Detective";
      if (exp < 500)
        return "Sneaky Samurai";
      if (exp < 750)
        return "Sneaky Shogun (Commander in Chief)";
      if (exp < 1000)
        return "Japan Fox Spirit";
      if (exp < 10000)
        return "Japan Dragon";
      return "Sneaky Japan Deity";
    }
  }
}
