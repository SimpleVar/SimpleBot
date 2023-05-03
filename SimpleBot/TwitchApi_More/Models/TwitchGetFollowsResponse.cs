using TwitchLib.Api.Helix.Models.Common;

namespace SimpleBot
{
  public class TwitchGetFollowsResponse
  {
    public int total;
    public Pagination pagination;
    public TwitchFollowData[] data;
  }

  public class TwitchFollowData
  {
    public string broadcaster_id, broadcaster_login, broadcaster_name, followed_at;
  }
}
