﻿namespace SimpleBot
{
  static class SneakyJapan
  {
    // TODO configurable via command loaded settings:
    const int MS_BEFORE_FIRST_ROUND = 3000;
    const int MS_ROUND_DURATION = 60000;
    const int MS_AFTER_ROUND = 300000;
    
    public static LongRunningPeriodicTask _task;
    static readonly object _lock = new();
    static Bot _bot;
    static long _currentRoundId;
    static bool _currentRoundOpen;

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
        if (ChatActivity.GetActiveChatters(TimeSpan.FromMinutes(10), maxChattersNeeded: 1).Count == 0)
        {
          Bot.Log("Sneaky Japan delayed due to inactive chat");
          await Task.Delay(60000);
          return;
        }
        lock (_lock)
        {
          _currentRoundId = rid;
          _currentRoundOpen = true;
          Settings.Default.SneakyJapanRound = rid + "";
          Settings.Default.Save();
        }
        // ROUND
        Bot.Log("Starting Sneaky Japan round: " + rid);
        bot.TwSendMsg($"/me peepoJapan Sneaky Japan is sneaking about! Try " + bot.CMD_PREFIX + "Japan and test your perception to spot it. Hurry! You only have one minute peepoJapan");
        await Task.Delay(MS_ROUND_DURATION);
        var winners = new List<string>();
        int sneakRoll = Rand.R.Next(20) + 1 + 10;
        if (Rand.R.Next(100) == 0)
          sneakRoll = 0;
        lock (_lock)
        {
          _currentRoundOpen = false;
          // TODO only loop over chatters that played this round, so collect them, and stop persisting LastRoll data
          foreach (var chatter in ChatterDataMgr.All())
          {
            var japan = chatter.SneakyJapanStats;
            if ((japan?.LastRollRoundId ?? 0) != rid)
              continue;
            var expGain = 1;
            japan.RoundsPlayed++;
            if (japan.LastRoll < sneakRoll)
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
            ChatterDataMgr.Update();
          }
        }
        if (winners.Count == 0)
          bot.TwSendMsg("/me This Japan was much too sneaky and could not be found by anyone D:");
        else
        {
          var winnersText = winners.Count == 1 ? winners[0] : winners.Count + " pro gamers!";
          bot.TwSendMsg($"/me This Japan wasn't sneaky enough and with brilliant observation was spotted by {winnersText} Clap The sneak roll was {sneakRoll}{(sneakRoll == 0 ? " LUL" : "")}");
        }
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
        var japan = chatter.SneakyJapanStats;
        var buff = CalcBuff(japan.Exp);
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
        japan.LastRoll = Rand.R.Next(20) + 1;
        bool crit = japan.LastRoll == 20;
        if (crit)
        {
          japan.LastRoll <<= 1;
          japan.TotalCrits++;
        }
        japan.LastRoll += buff;
        ChatterDataMgr.Update();
        _bot.TwSendMsg($"/me {tagUser} rolled a {(crit ? "CRIT " : "")}{japan.LastRoll}{(crit ? " Kreygasm" : "")}");
      }
    }

    static string FullJapanName(Chatter chatter) => $"{chatter.DisplayName} ({chatter.SneakyJapanStats.Exp} exp - {GetMasteryTitle(chatter.SneakyJapanStats.Exp)})";
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
