namespace SimpleBot.v2
{
    class CommandHandler
    {
        public delegate void Callback(Chatter chatter, string cmdAlias, List<string> args, string argsStr);
        public const string OWNER_EVERYONE = null;
        public const string OWNER_BUILTIN = "";

        public readonly string owner; // "username123" = can by managed only by username123 and streamer
        public readonly Callback action;
        public readonly string actionStr;
        public readonly List<string> aliases;
        public UserLevel minUserLevel;
        public List<string> allowedUsernames;

        public CommandHandler(string owner, IEnumerable<string> aliases, UserLevel minUserLevel, string actionStr, Callback action)
        {
            this.owner = owner;
            this.action = action;
            this.actionStr = actionStr;
            this.minUserLevel = minUserLevel;
            this.aliases = [.. aliases];
            if (this.aliases.Count == 0)
                throw new ArgumentException("Must have at least one alias", nameof(aliases));
        }

        public static CommandHandler Builtin(IEnumerable<string> aliases, UserLevel minUserLevel, Callback action) => new(OWNER_BUILTIN, aliases, minUserLevel, null, action);
    }

    // TODO think about: persistent storage of data - cannot be scoped per command, must be global.
    // the persistent storage of a command owned by someone (or builtin) must be read-only for any user-code that isn't by the owner
    class Commands
    {
        readonly Dictionary<string, CommandHandler> _cmdHandlers; // alias1 -> X, alias2 -> X

        public Commands()
        {
            //Bot.ONE.UserPath("commands")
            AddCommand(CommandHandler.Builtin([BuiltinAlias.title.ToString()], UserLevel.Moderator, Builtins.Title));
            //AddAlias(BuiltinAlias.title.ToString(), ["settitle"], null);
        }

        void AddCommand(CommandHandler handler)
        {
            for (int i = 0; i < handler.aliases.Count; i++)
                _cmdHandlers.Add(handler.aliases[i], handler);
        }

        void AddAlias(string existingCommand, List<string> aliasesToAdd, List<string> errors)
        {
            List<string> usedBuiltinAliases = null;
            for (int i = 0; i < aliasesToAdd.Count; i++)
            {
                aliasesToAdd[i] = aliasesToAdd[i]?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(aliasesToAdd[i]))
                {
                    aliasesToAdd.ReplaceWithLastAndPop(i--);
                    continue;
                }
                if (BuiltinAliasFromName(aliasesToAdd[i]) == null)
                    continue;
                usedBuiltinAliases ??= [];
                usedBuiltinAliases.Add(aliasesToAdd[i]);
            }
            if (usedBuiltinAliases != null && usedBuiltinAliases.Count > 0)
            {
                errors.Add("Cannot use builtin aliases: " + string.Join(", ", usedBuiltinAliases));
                return;
            }
            // TODO check if aliases are already used, and add them
        }

        enum BuiltinAlias
        {
            title
        }
        static BuiltinAlias? BuiltinAliasFromName(string alias) => Enum.TryParse<BuiltinAlias>(alias, out BuiltinAlias o) ? o : null;

        static class Builtins
        {
            public static void Title(Chatter chatter, string cmdAlias, List<string> args, string argsStr)
            {
                // chatter == null :: means silent mode
            }
        }
    }
}
