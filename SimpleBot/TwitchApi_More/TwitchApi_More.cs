using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Helix.Models.Chat;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Helix.Models.Polls;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace SimpleBot
{
  public class TwitchApi_MoreEdges : TwitchLib.Api.Core.ApiBase
  {
    public TwitchApi_MoreEdges(IApiSettings settings) : base(settings, BypassLimiter.CreateLimiterBypassInstance(), new TwitchHttpClient())
    {
      Settings.ClientId = settings.ClientId;
      Settings.AccessToken = settings.AccessToken;
      Settings.Secret = settings.Secret;
      Settings.SkipDynamicScopeValidation = settings.SkipDynamicScopeValidation;
      Settings.SkipAutoServerTokenGeneration = settings.SkipAutoServerTokenGeneration;
      Settings.Scopes = settings.Scopes;
    }

    public async Task<bool> Shoutout(string broadcasterId, string modId, string shoutedUserId, string accessToken = null)
    {
      var list = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("from_broadcaster_id", broadcasterId),
        new KeyValuePair<string, string>("to_broadcaster_id", shoutedUserId),
        new KeyValuePair<string, string>("moderator_id", modId)
      };
      try
      {
        await TwitchPostAsync("/chat/shoutouts", ApiVersion.Helix, null, list, accessToken).ConfigureAwait(true);
        return true;
      }
      catch (TooManyRequestsException)
      {
        return false;
      }
    }

    public async Task Announce(string broadcasterId, string modId, string announcement, AnnouncementColors color, string accessToken = null)
    {
      var list = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
        new KeyValuePair<string, string>("moderator_id", modId)
      };
      var payload = new { message = announcement, color = color.Value }.ToJson();
      await TwitchPostAsync("/chat/announcements", ApiVersion.Helix, payload, list, accessToken).ConfigureAwait(true);
    }

    public Task<TwitchGetFollowsResponse> GetFollowedChannelsAsync(string userId, int first = 100, string after = null, string accessToken = null)
    {
      if (first < 1 || first > 100)
        throw new BadParameterException("first cannot be less than 1 or greater than 100");

      var list = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("user_id", userId),
        new KeyValuePair<string, string>("first", first.ToString())
      };
      if (!string.IsNullOrWhiteSpace(after))
        list.Add(new KeyValuePair<string, string>("after", after));

      return TwitchGetGenericAsync<TwitchGetFollowsResponse>("/channels/followed", ApiVersion.Helix, list, accessToken);
    }

    public Task<TwitchGetFollowersResponse> GetFollowersAsync(string userId, string broadcasterId, int first = 100, string after = null, string accessToken = null)
    {
      if (first < 1 || first > 100)
        throw new BadParameterException("first cannot be less than 1 or greater than 100");

      var list = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
        new KeyValuePair<string, string>("first", first.ToString())
      };
      if (!string.IsNullOrWhiteSpace(userId))
        list.Add(new KeyValuePair<string, string>("user_id", userId));
      if (!string.IsNullOrWhiteSpace(after))
        list.Add(new KeyValuePair<string, string>("after", after));

      return TwitchGetGenericAsync<TwitchGetFollowersResponse>("/channels/followers", ApiVersion.Helix, list, accessToken);
    }

    public Task<List<TwitchFollowData>> GetAllFollows(string userId, string accessToken = null)
    {
      return TwitchApiExtensions.AggregatePages(after => GetFollowedChannelsAsync(userId, 100, after, accessToken), x => x.pagination, x => x.data);
    }

    public Task<List<TwitchFollowerData>> GetAllFollowers(string userId, string broadcasterId, string accessToken = null)
    {
      return TwitchApiExtensions.AggregatePages(after => GetFollowersAsync(userId, broadcasterId, 100, after, accessToken), x => x.pagination, x => x.data);
    }
  }
}
