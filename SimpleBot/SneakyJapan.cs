namespace SimpleBot
{
  /*
    JAPAN HEIST:
    everynow and then, JAPAN! the japan will have some random hidden sneakiness to it.
    chatters may roll a d20 perception to see if they notice the sneaky japan.
    After a set amount of time, all chatters that were able to notice the japan are rewarded exp.
    With enough exp you level up through the Japan expertise levels (Enthusiast, Expert, etc)
    and that level gives you a flat bonus to all perception checks (and street credit).
  */
  class SneakyJapanStats
  {
    public int Exp = 1;
    public int LastRoll;
    public long LastRollRoundId;
  }

  static class SneakyJapan
  {
    const int MS_BEFORE_FIRST_ROUND = 3000;
    const int MS_ROUND_DURATION = 60000;
    const int MS_AFTER_ROUND = 300000;
    
    static object _lock = new object();
    static Thread thread;
    static Bot bot;
    static long currentRoundId;
    static bool currentRoundOpen;

    public static void Init(Bot bot)
    {
#if DEBUG
      return;
#endif
      if (thread != null)
        throw new ApplicationException("Init should be called exactly once");

      SneakyJapan.bot = bot;
      currentRoundId = int.TryParse(Settings.Default.SneakyJapanRound, out var sjr) ? sjr : 1;

      thread = new Thread(async () =>
      {
        await Task.Delay(MS_BEFORE_FIRST_ROUND);
        while (true)
        {
          long rid;
          lock (_lock)
          {
            rid = ++currentRoundId;
            Settings.Default.SneakyJapanRound = rid + "";
            Settings.Default.Save();
            currentRoundOpen = true;
          }
          Bot.Log("Starting Sneaky Japan round: " + rid);
          bot.TwSendMsg($"/me Sneaky Japan is sneaking about! Try " + bot.CMD_PREFIX + "Japan and test your perception to spot it. Hurry! You only have one minute");
          await Task.Delay(MS_ROUND_DURATION);
          // ROUND
          var winners = new List<string>();
          int sneakRoll = Rand.R.Next(20) + 1 + 10;
          lock (_lock)
          {
            currentRoundOpen = false;
            foreach (var chatter in ChatterDataMgr.All())
            {
              var japan = chatter.sneakyJapanStats;
              if ((japan?.LastRollRoundId ?? 0) != rid)
                continue;
              var expGain = 1;
              if (japan.LastRoll >= sneakRoll)
              {
                expGain = 5;
                winners.Add(chatter.DisplayName);
              }
              japan.Exp += expGain;
              ChatterDataMgr.Update(chatter);
            }
          }
          if (winners.Count == 0)
            bot.TwSendMsg("/me This Japan was much too sneaky and could not be found by anyone D:");
          else if (winners.Count == 1)
            bot.TwSendMsg("/me This Japan wasn't sneaky enough and with brilliant observation was spotted by " + winners[0] + " Clap The sneak roll was " + sneakRoll);
          else
            bot.TwSendMsg("/me This Japan wasn't sneaky enough and with brilliant observation was spotted by " + winners.Count + " pro gamers! Clap The sneak roll was " + sneakRoll);
          await Task.Delay(MS_AFTER_ROUND);
        }
      });
      thread.IsBackground = true;
      thread.Start();
    }

    public static void Japan(string tagUser, string cmd, List<string> args, string argsStr)
    {
      var name = tagUser.CanonicalUsername();
      lock (_lock)
      {
        var chatter = ChatterDataMgr.Get(name);
        var japan = chatter.sneakyJapanStats ??= new SneakyJapanStats();
        var buff = CalcBuff(japan.Exp);
        tagUser += $" ({japan.Exp} exp - {GetMasteryTitle(japan.Exp)})";
        if (!currentRoundOpen)
        {
          bot.TwSendMsg("Your exp grants you a hidden bonus of " + buff + " for all future Sneaky Japans", tagUser);
          return;
        }
        if (japan.LastRollRoundId == currentRoundId)
        {
          bot.TwSendMsg("You've already rolled a " + japan.LastRoll + " for this Sneaky Japan", tagUser);
          return;
        }
        japan.LastRollRoundId = currentRoundId;
        japan.LastRoll = Rand.R.Next(20) + 1;
        bool crit = japan.LastRoll == 20;
        if (crit)
          japan.LastRoll <<= 1;
        japan.LastRoll += buff;
        ChatterDataMgr.Update(chatter);
        bot.TwSendMsg($"/me {tagUser} rolled a {(crit ? "CRIT " : "")}{japan.LastRoll}{(crit ? " Kreygasm" : "")}");
      }
    }

    private static int CalcBuff(int exp) => Math.Min(20, (20 * exp) / 1000);
    private static string GetMasteryTitle(int exp)
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
      return "Japan Deity (Kami-sama)";
    }
  }
}
