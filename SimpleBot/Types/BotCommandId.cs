﻿namespace SimpleBot
{
  // NOTE: THESE VALUES MUST BE SET IN STONE
  enum BotCommandId : int
  {
    AddIgnoredBot = 100,
    RemoveIgnoredBot = 110,
    SlowMode = 120,
    SlowModeOff = 130,
    SetTitle = 200,
    SetGame = 210,
    ShowBrb = 750,
    SearchGame = 1000,
    GetCmdCounter = 1005,
    GetRedeemCounter = 1007,
    FollowAge = 1010,
    Queue_Curr = 1500,
    Queue_Next = 1501,
    Queue_All = 1502,
    Queue_Clear = 1503,
    Queue_Join = 1504,
    Queue_Leave = 1505,
    Queue_Close = 1506,
    Queue_Open = 1507,
    SneakyJapan = 2000,
    SneakyJapan_Stats = 2001,
    CoinFlip = 3010,
    DiceRoll = 3020,
    Quote_Get = 4000,
    Quote_Add = 4010,
    Quote_Del = 4020,
    LearnHiragana = 5010,
    GetChessRatings = 6010,
    FIRST_CUSTOM_COMMAND = 100000,
  }
}