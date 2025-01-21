using OBSWebsocketDotNet;
using System.Collections.Concurrent;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace SimpleBot.v2
{
    class Bot
    {
        public static Bot ONE {  get; private set; }

        public readonly string Channel = Settings.Default.Channel;
        public readonly string BotName = Settings.Default.TwitchBotUsername;
        public readonly string UserDataFolder = Settings.Default.UserDataFolder;
        public readonly string CmdPrefix = Settings.Default.CommandsPrefix;

        public string UserPath(string dir) => Path.Combine(UserDataFolder, dir);

        public string ChannelId { get; private set; }
        public string BotId { get; private set; }
        public bool IsStreaming { get; private set; } = true; // TODO
        public bool IsTwitchConnected { get; private set; }
        public event Action<bool> TwitchConnectionChange = delegate { };

        public readonly TwitchAPI tw;
        public readonly TwitchClient _tw;
        public static OBSWebsocket _obs; // TODO
        public readonly ChatActivity chatActivity;
        public readonly Chatters chatters;
        public readonly Commands commands;

        readonly JoinedChannel _twJC; // fake object with no data for quick TwSendMessage
        readonly BlockingCollection<ChatMessage> _msgQueue = [];
        readonly Thread _msgProcessing;

        public Bot()
        {
            if (ONE != null) throw new ApplicationException();
            ONE = this;
            try
            {
                if (string.IsNullOrEmpty(Channel))
                    Err.Fatal($"{nameof(Settings.Default.Channel)} must not be empty");
                if (string.IsNullOrEmpty(BotName))
                    Err.Fatal($"{nameof(Settings.Default.TwitchBotUsername)} must not be empty");
                if (string.IsNullOrEmpty(UserDataFolder))
                    Err.Fatal($"{nameof(Settings.Default.UserDataFolder)} must not be empty");
                if (CmdPrefix.Length > 0 && char.IsLetterOrDigit(CmdPrefix[0]))
                    Err.Fatal($"{nameof(Settings.Default.CommandsPrefix)} must begin with non-alphanumeric character");

                _twJC = new JoinedChannel(Channel);
                tw = new TwitchAPI(settings: new ApiSettings { ClientId = Settings.Default.TwitchClientId, AccessToken = File.ReadAllText(Settings.Default.TwitchOAuth) });
                ChannelId = tw.GetUserIdAsync(Channel.CanonicalUsername()).Result;
                if (ChannelId == null)
                    Err.Fatal("Failed to get twitch user id of the streamer");
                BotId = tw.GetUserIdAsync(BotName.CanonicalUsername()).Result;
                if (BotId == null)
                    Err.Fatal("Failed to get twitch user id of the bot account");

                _tw = new TwitchClient(new WebSocketClient(new ClientOptions { DisconnectWait = 5000 }));
                _tw.Initialize(new ConnectionCredentials(BotName, File.ReadAllText(Settings.Default.TwitchOAuthBot)), Channel);

                _tw.OnConnected += (o, e) => { Log.Info("", nameof(_tw.OnConnected)); TwitchConnectionChange(IsTwitchConnected = true); };
                _tw.OnReconnected += (o, e) => { Log.Info("", nameof(_tw.OnReconnected)); TwitchConnectionChange(IsTwitchConnected = true); };
                _tw.OnDisconnected += (o, e) => { Log.Info("", nameof(_tw.OnDisconnected)); TwitchConnectionChange(IsTwitchConnected = false); };
                _tw.OnError += (o, e) => { Log.Err(e.Exception.ToString(), nameof(_tw.OnError)); };
                _tw.OnConnectionError += (o, e) => { Log.Err(e.Error.Message, nameof(_tw.OnConnectionError)); };
                _tw.OnNoPermissionError += (o, e) => { Log.Err("generic error", nameof(_tw.OnNoPermissionError)); };
                _tw.OnIncorrectLogin += (o, e) => { Err.Fatal(e.Exception, nameof(_tw.OnIncorrectLogin)); };
                _tw.OnMessageReceived += (o, e) => { _msgQueue.Add(e.ChatMessage); };
                _tw.Connect();

                _msgProcessing = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            twOnMessage(_msgQueue.Take());
                        }
                        catch (Exception ex)
                        {
                            Err.Fatal(ex, nameof(_tw.OnMessageReceived));
                        }
                    }
                }) { IsBackground = true };

                Directory.CreateDirectory(UserDataFolder);
                chatActivity = new(this);
                chatters = new(this);
                commands = new();

                _msgProcessing.Start();
            }
            catch (Exception ex)
            {
                Err.Fatal(ex);
            }
        }

        public void TwSendMsg(Chatter tagChatter, string msg)
        {
            if (tagChatter != null) msg = $"@{tagChatter.DisplayName} {msg}";
            if (msg.Length > 499)
                msg = msg[0..499] + "…";
#if DEBUG
            if (msg.Length != 0 && !(msg[0] is '.' or '/'))
                msg = "* " + msg;
#endif
            _tw.SendMessage(_twJC, msg);
        }

        private void twOnMessage(ChatMessage msg)
        {
            Chatter chatter = chatActivity._OnMessage(msg);
            return;
            // TODO
            string cmdName = "";
            Commands handler = null;

            try
            {
                //handler.action(null, null, null, null);
                ;// ChatActivity.IncCommandCounter(null, 0);
            }
            catch (Exception ex)
            {
                Log.Err($"Command '{cmdName}' threw an exception: " + ex);
            }
        }


    }
}
