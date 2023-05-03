using TwitchLib.Api.Helix.Models.Common;
using TwitchLib.Client.Models;

namespace SimpleBot
{
  static class TwitchApiExtensions
  {
    public static string CleanUsername(this string name) => name.Length == 0 || (name[0] != '#' && name[0] != '@') ? name : name[1..];
    public static string CanonicalUsername(this string name) => CleanUsername(name).ToLowerInvariant();

    public static UserLevel GetUserLevel(this ChatMessage msg)
    {
      if (msg.IsBroadcaster) return UserLevel.Streamer;
      if (msg.IsModerator) return UserLevel.Mod;
      if (msg.IsVip) return UserLevel.Vip;
      if (msg.IsSubscriber) return UserLevel.Sub;
      return UserLevel.None;
    }

    public static async Task<List<U>> AggregatePages<T, U>(Func<string, Task<T>> req, Func<T, Pagination> pageFn, Func<T, U[]> dataFn)
    {
      var res = await req(null).ConfigureAwait(true);
      var pg = pageFn(res);
      var list = dataFn(res).ToList();
      while (pg.Cursor != null)
      {
        res = await req(pg.Cursor).ConfigureAwait(true);
        pg = pageFn(res);
        list.AddRange(dataFn(res));
      }
      return list;
    }
  }
}
