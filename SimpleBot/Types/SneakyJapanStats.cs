﻿using System.Text.Json.Serialization;

namespace SimpleBot
{
  class SneakyJapanStats
  {
    public int Exp = 1;
    public int NewGamePlus;
    public int RoundsPlayed;
    public int RoundsWon;
    public int TotalCrits;
    public int WinStreak_Curr;
    public int WinStreak_Longest;
    public int TemporaryBuff;

    [JsonIgnore]
    public int LastRoll;
    [JsonIgnore]
    public long LastRollRoundId;

    public int GetRankingScore() => NewGamePlus * 10000 + Exp;
  }
}
