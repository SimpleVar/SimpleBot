﻿using TwitchLib.EventSub.Core.SubscriptionTypes.User;
using TwitchLib.EventSub.Websockets.Core.Models;

namespace TwitchLib.EventSub.Websockets.Core.EventArgs.User
{
    public class UserUpdateArgs : TwitchLibEventSubEventArgs<EventSubNotification<UserUpdate>>
    { }
}