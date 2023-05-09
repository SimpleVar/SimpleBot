using System.Web;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Games;

namespace SimpleBot
{
  static class SetGameOrTitle
  {
    enum KnownGame { Chess, Tetrio, GeoGuessr, Coding, JustChatting };
    static KnownGame? TryGetKnownGame(string name) => name.ToLowerInvariant() switch
    {
      "chess" or "ch" or "c" => (KnownGame?)KnownGame.Chess,
      "tetr.io" or "tetrio" or "tetris" or "tet" or "t" => (KnownGame?)KnownGame.Tetrio,
      "geoguessr" or "geoguesser" or "gg" or "g" => (KnownGame?)KnownGame.GeoGuessr,
      "coding" or "code" or "software" or "dev" => (KnownGame?)KnownGame.Coding,
      "just chatting" or "justchatting" or "chatting" or "jc" => (KnownGame?)KnownGame.JustChatting,
      _ => null,
    };

    public static async Task SetTitle(Bot bot, Chatter chatter, string title)
    {
      if (title.Length == 0)
        return;
      await bot._twApi.Helix.Channels.ModifyChannelInformationAsync(bot.CHANNEL_ID, new ModifyChannelInformationRequest
      {
        Title = title
      }).ConfigureAwait(true);
      bot.TwSendMsg("Title changed: " + title, chatter);
    }

    public static async Task SearchGame(Bot bot, Chatter tagUser, string query)
    {
      var (gameId, gameName) = await _searchGame(bot, tagUser, query).ConfigureAwait(true);
      if (gameId == null) return;
      bot.TwSendMsg($"Game id {gameId} | {gameName}", tagUser);
    }

    public static async Task SetGame(Bot bot, Chatter tagUser, string query)
    {
      var (gameId, gameName) = await _searchGame(bot, tagUser, query).ConfigureAwait(true);
      if (gameId == null) return;
      await bot._twApi.Helix.Channels.ModifyChannelInformationAsync(bot.CHANNEL_ID, new ModifyChannelInformationRequest
      {
        GameId = gameId
      }).ConfigureAwait(true);
      bot.TwSendMsg("Game changed: " + gameName, tagUser);
    }

    static async Task<(string id, string name)> _searchGame(Bot bot, Chatter tagUser, string query)
    {
      KnownGame? knownGame = TryGetKnownGame(query);
      if (knownGame != null)
      {
        string id, name;
        switch (knownGame)
        {
          case KnownGame.Chess: id = "743"; name = "Chess"; break;
          case KnownGame.Tetrio: id = "517447"; name = "TETR.IO"; break;
          case KnownGame.GeoGuessr: id = "369418"; name = "GeoGuessr"; break;
          case KnownGame.Coding: id = "1469308723"; name = "Software and Game Development"; break;
          case KnownGame.JustChatting: id = "509658"; name = "Just Chatting"; break;
          default: throw new ApplicationException();
        }
        return (id, name);
      }

      var games = (await bot._twApi.Helix.Search.SearchCategoriesAsync(HttpUtility.UrlEncode(query)).ConfigureAwait(true)).Games;
      if (games.Length == 0)
      {
        bot.TwSendMsg("No games found for query: " + query, tagUser);
        return (null, null);
      }
      Game g = games[0];
      if (games.Length > 1)
      {
        Array.Sort(games, (a, b) => int.Parse(a.Id).CompareTo(int.Parse(b.Id)));
        g = games.FirstOrDefault(x => x.Name.Equals(query, StringComparison.InvariantCultureIgnoreCase));
        if (g == null)
        {
          var gamesStr = string.Join(" | ", games.Select(x => x.Name).Distinct().Take(5));
          bot.TwSendMsg("Be more specific, several results: " + gamesStr, tagUser);
          return (null, null);
        }
      }
      return (g.Id, g.Name);
    }
  }
}
