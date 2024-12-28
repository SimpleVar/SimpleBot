using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Chat.ChatSettings;
using TwitchLib.Api.Helix.Models.Common;
using TwitchLib.Api.Helix.Models.Polls;
using TwitchLib.Client.Models;

namespace SimpleBot
{
    static class TwitchApiExtensions
    {
        public static string CleanUsername(this string name) => name.Length == 0 || (name[0] != '#' && name[0] != '@') ? name : name[1..];
        public static string CanonicalUsername(this string name) => CleanUsername(name).ToLowerInvariant();

        /// This feels more annoying than helpful since with 7tv/bttv you still see deleted msgs,
        /// so for now this is a NOOP
        public static Task Hide(this ChatMessage msg, Bot bot) => Task.CompletedTask;//bot._twApi.Helix.Moderation.DeleteChatMessagesAsync(bot.CHANNEL_ID, bot.CHANNEL_ID, msg.Id).NoThrow();

        public static UserLevel GetUserLevel(this ChatMessage msg)
        {
            if (msg.IsBroadcaster) return UserLevel.Streamer;
            if (msg.IsModerator) return UserLevel.Moderator;
            if (msg.IsVip) return UserLevel.VIP;
            if (msg.IsSubscriber) return UserLevel.Subscriber;
            return UserLevel.Normal;
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

        public static async Task<Poll> GetLatestPoll(this TwitchAPI api, string broadcasterId) => (await api.Helix.Polls.GetPollsAsync(broadcasterId, first: 1).ConfigureAwait(true)).Data.FirstOrDefault();

        public static async Task EndCurrentPoll(this TwitchAPI api, string broadcasterId, bool keepPublic)
        {
            var currPoll = await api.GetLatestPoll(broadcasterId);
            if (currPoll.Status == "ACTIVE" || (!keepPublic && currPoll.Status == "COMPLETED"))
            {
                var status = keepPublic ? PollStatusEnum.TERMINATED : PollStatusEnum.ARCHIVED;
                _ = await api.Helix.Polls.EndPollAsync(broadcasterId, currPoll.Id, status).ConfigureAwait(true);
            }
        }

        public static async Task<bool> StartPoll(this TwitchAPI api, string broadcasterId, string title, int durationSecs, IEnumerable<string> choices)
        {
            try
            {
                var poll = (await api.Helix.Polls.CreatePollAsync(new()
                {
                    BroadcasterId = broadcasterId,
                    Title = title,
                    DurationSeconds = durationSecs,
                    Choices = choices.Select(x => new TwitchLib.Api.Helix.Models.Polls.CreatePoll.Choice { Title = x }).ToArray()
                }).ConfigureAwait(true)).Data[0];
                return poll.Status == "ACTIVE";
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Searches cache (ChatterDataMgr) before hitting the api
        /// </summary>
        public static async Task<string> GetUserIdAsync(this TwitchAPI api, string canonicalName)
        {
            // TODO skip cache BotV2.ONE.chatters if null
            var uid = ChatterDataMgr.GetOrNull(canonicalName)?.uid;
            if (string.IsNullOrEmpty(uid))
            {
                var res = await api.Helix.Users.GetUsersAsync(logins: new List<string> { canonicalName }).ConfigureAwait(true);
                uid = res.Users.FirstOrDefault()?.Id;
            }
            return uid;
        }
        /// <summary>
        /// Searches cache (ChatterDataMgr) before hitting the api
        /// </summary>
        public static string GetUserId(this TwitchAPI api, string canonicalName) => GetUserIdAsync(api, canonicalName).Result;

        public static async Task<ChatSettings> GetChatSettings(this TwitchAPI api, string broadcasterId, string moderatorId)
        {
            var res = (await api.Helix.Chat.GetChatSettingsAsync(broadcasterId, moderatorId).ConfigureAwait(true)).Data[0];
            return new ChatSettings
            {
                EmoteMode = res.EmoteMode,
                FollowerMode = res.FollowerMode,
                FollowerModeDuration = res.FollowerModeDuration,
                NonModeratorChatDelay = res.NonModeratorChatDelay,
                NonModeratorChatDelayDuration = res.NonModeratorChatDelayDuration,
                SlowMode = res.SlowMode,
                SlowModeWaitTime = res.SlowModeWaitDuration,
                SubscriberMode = res.SubscriberMode,
                UniqueChatMode = res.UniqueChatMode
            };
        }

        public static async Task ChangeChatSettings(this TwitchAPI api, string broadcasterId, string moderatorId, Action<ChatSettings> doChanges)
        {
            var settings = await api.GetChatSettings(broadcasterId, moderatorId).ConfigureAwait(true);
            doChanges(settings);
            await api.Helix.Chat.UpdateChatSettingsAsync(broadcasterId, moderatorId, settings).ConfigureAwait(true);
        }

        public static ChannelInformation GetChannelInfo(this TwitchAPI api, string userId)
        {
            return string.IsNullOrWhiteSpace(userId)
                ? null
                : api.Helix.Channels.GetChannelInformationAsync(userId).Result?.Data?.FirstOrDefault();
        }
    }
}
