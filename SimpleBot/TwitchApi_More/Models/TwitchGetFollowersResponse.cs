using TwitchLib.Api.Helix.Models.Common;

namespace SimpleBot
{
  public class TwitchGetFollowersResponse
  {
    public int total;
    public Pagination pagination;
    public TwitchFollowerData[] data;
  }

  public class TwitchFollowerData
  {
    public string followed_at, user_id, user_login, user_name;
  }
}
