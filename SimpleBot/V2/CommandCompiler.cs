using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace SimpleBot.v2
{
    class CommandCompiler
    {
        // $$ :: arg count
        // $0 :: all args in single string
        // $1 :: first arg
        // $query :: all args
        // $target :: named variable
        // bleep $1 bloop :: response text
        // ${res := 7} :: code execution
        // ${res := mul(+$1); say("answer is \res"); delay(1); say("bleep")} I'm also gonna say "bleep" in a second :: code execution and response
        //    answer is 7
        //    I'm also gonna say "bleep" in a second
        //    *1 second passes*
        //    bleep

        // $("eval expression" + ($num * +$1))
        // $(res.data.whatever[2].title)

        //  ${say(+$1 + +$2)}
        //  ${say($1 ?? "bleep bloop")}
        //  ${say($1 + "x")}
        //  ${say($1 + "0")}
        // !addcmd hug $name is hugging $target
        // !addcmd gp ${run("game poker");} Game is poker
        // !addcmd troll ${run("game poker"); say("Game is poker"); delay($1); run("game just chatting"); say("Game is just chatting");}

        // !addcmd <name> [ ${<code block>} ] [ <literal response text with $variable syntax> ]
        //    response text must escape \$ literal

        public static BotV2.CommandHandler.Callback CreateHandler(ReadOnlySpan<char> input, int index)
        {
            // TODO handle semantic errors
            Ctx ctx = new Ctx(input, index);
            Node root = parseRoot(ref ctx);
            if (root.Children.Count == 0)
                return null;

            var varsExpr = Expression.Parameter(typeof(Dictionary<string, dynamic>), "vars");
            var chatterExpr = Expression.Parameter(typeof(Chatter), "chatter");
            var codeExpr = Build(root, varsExpr, chatterExpr);
            var code = (Action<Dictionary<string, dynamic>, Chatter>)Expression.Lambda(codeExpr, varsExpr, chatterExpr).Compile();
            var inclDollarNames = ctx.referencedDollarNames.ToArray();

            // Callback(Chatter chatter, string cmdName, List<string> args, string argsStr);
            BotV2.CommandHandler.Callback cb = (chatter, cmdName, args, argsStr) =>
            {
                var vars = new Dictionary<string, dynamic> { { "$", args.Count }, { "0", argsStr } };
                for (int i = 0; i < 9 && i < args.Count; i++)
                    vars.Add((i + 1) + "", args[i]);

                string targetName = args.Count == 0 ? null : args[0].CanonicalUsername();

                var getTargetInfo = Extensions.Cached(() => BotV2.ONE.tw.GetChannelInfo(targetName));
                var getChatterInfo = Extensions.Cached(() => BotV2.ONE.tw.GetChannelInfo(chatter.name));
                var getTargetChatter = Extensions.Cached(() => BotV2.ONE.chatters.GetOrNull(targetName));

                for (int i = 0; i < inclDollarNames.Length; i++)
                {
                    var name = inclDollarNames[i];
                    switch (name)
                    {
                        case "query": vars.Add(name, HttpUtility.UrlEncode(argsStr) ?? ""); break;
                        case "channel": vars.Add(name, BotV2.ONE.Channel); break;
                        case "channel_id": vars.Add(name, BotV2.ONE.ChannelId); break;

                        case "user_name": vars.Add(name, chatter.DisplayName); break;
                        case "user_id": vars.Add(name, chatter.uid); break;
                        case "user_game": vars.Add(name, getChatterInfo()?.GameName ?? "<no game>"); break;
                        case "user_title": vars.Add(name, getChatterInfo()?.Title ?? "<no title>"); break;
                        case "user_level": vars.Add(name, chatter.userLevel.ToString()); break;

                        case "target_name": vars.Add(name, getTargetChatter()?.DisplayName ?? (args.Count == 0 ? null : args[0].CleanUsername()) ?? "<no target>"); break;
                        case "target_id": vars.Add(name, BotV2.ONE.tw.GetUserId(targetName) ?? "<not found>"); break;
                        case "target_game": vars.Add(name, getTargetInfo()?.GameName ?? "<no game>"); break;
                        case "target_title": vars.Add(name, getTargetInfo()?.Title ?? "<no title>"); break;
                        case "target_level": vars.Add(name, getTargetChatter()?.userLevel.ToString() ?? "<not found>"); break;

                        case "targetorself_name": if (args.Count == 0) goto case "user_name"; else goto case "target_name";
                        case "targetorself_id": if (args.Count == 0) goto case "user_id"; else goto case "target_id";
                        case "targetorself_game": if (args.Count == 0) goto case "user_game"; else goto case "target_game";
                        case "targetorself_title": if (args.Count == 0) goto case "user_title"; else goto case "target_title";
                        case "targetorself_level": if (args.Count == 0) goto case "user_level"; else goto case "target_level";

                        case "randomchatter": vars.Add(name, BotV2.ONE.chatters.RandomChatter()?.DisplayName ?? "<nobody>"); break;
                        case "count": vars.Add(name, "TODO"); break;
                    }
                }

                // User code may contain IO-blocking operations (fetch, delay, ...)
                _ = Task.Run(() =>
                {
                    try
                    {
                        code(vars, chatter);
                    }
                    catch (Exception ex)
                    {
                        // TODO handle runtime error
                    }
                });
            };
            return cb;
        }

        static readonly MethodInfo _strConcatMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]);
        static readonly MethodInfo _obj2strMethod = typeof(Convert).GetMethod("ToString", [typeof(object)]);
        static readonly Expression<Action<Chatter, string, object[]>> _callExpr = (chatter, cmdName, args) => Call(chatter, cmdName, args);
        static readonly Expression<Func<Dictionary<string, dynamic>, string, dynamic>> _varsGetValOrNull = (vars, key) => vars.ContainsKey(key) ? vars[key] : null;
        
        static void Call(Chatter chatter, string func, params object[] args)
        {
            // TODO
            // TODO make sure chatter has privilages to execute this cmd
            // wrap each call with trycatched to repropagate the exception with better message
            Console.WriteLine();
        }

        static Expression Build(Node node, Expression vars, Expression chatter)
        {
            switch (node.Kind)
            {
                case NodeKind.Invalid:
                    throw new ApplicationException("Invalid Node should not have built");
                case NodeKind.StatementList:
                    var children = new Expression[node.Children.Count];
                    for (int i = 0; i < children.Length; i++)
                        children[i] = Build(node.Children[i], vars, chatter);
                    return Expression.Block(children);
                case NodeKind.Call:
                    var args = new Expression[node.Children.Count - 1];
                    for (int i = 0; i < args.Length; i++)
                        args[i] = Build(node.Children[i + 1], vars, chatter);
                    return Expression.Invoke(_callExpr, chatter, Build(node.Children[0], vars, chatter), Expression.NewArrayInit(typeof(object), args));
                case NodeKind.String:
                    Expression concat = Expression.Constant("", typeof(string));
                    for (int i = 0; i < node.Children.Count; i++)
                        concat = Expression.Add(concat, Build(node.Children[i], vars, chatter), _strConcatMethod);
                    return concat;
                case NodeKind.DollarName:
                    return Expression.Coalesce(
                        Expression.Invoke(_varsGetValOrNull, vars, Expression.Constant(node.Value, typeof(string))),
                        Expression.Constant("$" + node.Value, typeof(string))
                    );
                case NodeKind.TextLiteral:
                    return Expression.Constant(node.Value, typeof(string));
                case NodeKind.CastToString:
                    return Expression.Convert(Build(node.Children[0], vars, chatter), typeof(string));
                default:
                    throw new ApplicationException($"Unhandled {nameof(NodeKind)} {node.Kind}");
            }
        }

        public enum NodeKind
        {
            Invalid,
            StatementList,
            Call,
            String,
            DollarName,
            TextLiteral,
            CastToString,
            Expr_Add, // TODO ...
        }
        public record Node(NodeKind Kind, List<Node> Children = null, string Value = null)
        {
            public string Value = Value;
            public Node Copy() => new(Kind, Children?.Select(x => x.Copy())?.ToList(), Value);
        }

        ref struct Ctx(ReadOnlySpan<char> input, int i)
        {
            public readonly ReadOnlySpan<char> input = input;
            public int i = i;
            public HashSet<string> referencedDollarNames = [];

            public readonly int peakIdentifierLength(int indexOffset)
            {
                int j = i + indexOffset;
                if (!isAlpha(input[j]))
                    return 0;
                int len = 0;
                while (j < input.Length)
                {
                    char c = input[j++];
                    if (!isAlpha(c) && !isDigit(c))
                        break;
                    len++;
                }
                return len;
            }
            public void requireChar(char c)
            {
                if (i >= input.Length || input[i] != c)
                    throw new ApplicationException("TODO unexpected token");
                i++;
            }
            public void skipWhitespace()
            {
                while (i < input.Length && char.IsWhiteSpace(input[i]))
                    i++;
            }
            public readonly bool lookahead(int indexOffset, char c1, char c2) { char c = lookahead(indexOffset); return c == c1 || c == c2; }
            public readonly bool lookahead(int indexOffset, char c) => lookahead(indexOffset) == c;
            public readonly char lookahead(int indexOffset) => i + indexOffset < input.Length ? input[i + indexOffset] : '\0';
            public readonly char peak() => i < input.Length ? input[i] : '\0';
            public static bool isAlpha(char c) => c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            public static bool isDigit(char c) => c >= '0' && c <= '9';
        }

        static Node parseRoot(ref Ctx ctx)
        {
            Node codeBlock = new(NodeKind.StatementList, []);
            parseCodeBlock(ref ctx, codeBlock.Children);

            Node extraString = parseString(ref ctx, false);
            if (extraString != null)
            {
                codeBlock.Children.Add(new(NodeKind.Call, [
                    new(NodeKind.TextLiteral, null, "chat"),
                    extraString
                ]));
            }
            if (ctx.i < ctx.input.Length)
                throw new ApplicationException("TODO failed to read entire thingy");
            return codeBlock;
        }

        static void parseCodeBlock(ref Ctx ctx, List<Node> children)
        {
            ctx.skipWhitespace();
            if (!ctx.lookahead(0, '$', '{'))
                return;
            ctx.i += 2;
            while (true)
            {
                var stmt = parseStatement(ref ctx);
                if (stmt == null)
                    break;
                children.Add(stmt);
            }
            ctx.requireChar('}');
            ctx.skipWhitespace();
        }
        
        static Node parseStatement(ref Ctx ctx)
        {
            // ; <expression>    :: has to be "call", "assignment", "increment", "decrement", or something that actually does a thing
            ctx.skipWhitespace();
            while (ctx.i < ctx.input.Length && ctx.input[ctx.i] != '}') ctx.i++; return null;
            // TODO
        }

        static Node parseExpression(ref Ctx ctx)
        {
            // ; <atom>
            // ; <expr> <binary-op> <expr>
            ctx.skipWhitespace();
            var atom = parseAtom(ref ctx);
            ctx.skipWhitespace();
            return atom;
        }

        static Node parseAtom(ref Ctx ctx)
        {
            // ; <literal>
            // | <dollarParticle>
            // | (<expression>)
            // | <identifier>(...<expr>)
            // | <atom>.<identifier>
            // | <atom>[<expr>]
            // | <atom><postfix-op>
            // | <prefix-op><atom>
            return null;
        }

        static Node parseString(ref Ctx ctx, bool quoted = true)
        {
            var s = new List<Node>();
            var sb = new StringBuilder();
            for (; ctx.i < ctx.input.Length; ctx.i++)
            {
                if (quoted && ctx.peak() == '"')
                {
                    ctx.i++;
                    break;
                }
                if (ctx.peak() == '\\')
                {
                    if (++ctx.i < ctx.input.Length)
                        sb.Append(ctx.peak());
                    continue;
                }
                Node dollarNode = parseDollarParticle(ref ctx);
                if (dollarNode == null)
                {
                    sb.Append(ctx.peak());
                    continue;
                }
                if (sb.Length != 0)
                    s.Add(new(NodeKind.TextLiteral, null, sb.ToString()));
                sb.Clear();
                s.Add(dollarNode);
                ctx.i--;
            }
            if (sb.Length != 0)
                s.Add(new(NodeKind.TextLiteral, null, sb.ToString()));
            return s.Count == 0 ? null : new Node(NodeKind.String, s);
        }

        static Node parseDollarParticle(ref Ctx ctx)
        {
            // ; $$
            // | $i             where i is in 1..9
            // | $identifier
            // | $(expression)
            if (ctx.i + 1 >= ctx.input.Length || ctx.input[ctx.i] != '$')
                return null;
            char c = ctx.lookahead(1);
            if (c == '$' || (c >= '1' && c <= '9'))
            {
                ctx.i += 2;
                return new Node(NodeKind.DollarName, null, c + "");
            }
            if (c == '(')
            {
                ctx.i += 2;
                Node expr = parseExpression(ref ctx);
                ctx.requireChar(')');
                return new Node(NodeKind.CastToString, [expr]);
            }
            int len = ctx.peakIdentifierLength(1);
            if (len == 0)
                return null;
            string val = ctx.input.Slice(ctx.i + 1, len).ToString();
            ctx.referencedDollarNames.Add(val);
            ctx.i += len + 1;
            return new Node(NodeKind.DollarName, null, val);
        }

        public static void _test_parseString()
        {
            void expect(string input, bool quoted, Node[] expected)
            {
                Ctx ctx = new Ctx(input, 0);
                var node = parseString(ref ctx, quoted);
                if (node.Children.Count != expected.Length)
                    throw new ApplicationException();
                for (int j = 0; j < node.Children.Count; j++)
                {
                    var _expected = expected[j];
                    var _actual = node.Children[j];
                    if (_actual.Kind != _expected.Kind || _actual.Value != _expected.Value)
                        throw new ApplicationException();
                }
                Debug.WriteLine(input);
                for (int j = 0; j < node.Children.Count; j++)
                    Debug.WriteLine($"  :: {node.Children[j].Kind} :: '{node.Children[j].Value}'");
            }

            expect("abc", false, new Node[] {
                new(NodeKind.TextLiteral, null, "abc"),
            });
            expect("a$$c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a"),
                new(NodeKind.DollarName, null, "$"),
                new(NodeKind.TextLiteral, null, "c"),
            });
            expect("a$1c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a"),
                new(NodeKind.DollarName, null, "1"),
                new(NodeKind.TextLiteral, null, "c"),
            });
            expect("a$0c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a$0c"),
            });
            expect("a $$ c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a "),
                new(NodeKind.DollarName, null, "$"),
                new(NodeKind.TextLiteral, null, " c"),
            });
            expect("a\\$c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a$c"),
            });
            expect("a\\$$c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a$"),
                new(NodeKind.DollarName, null, "c"),
            });
            expect("a$\\$c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a$$c"),
            });
            expect("a$$\\c", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a"),
                new(NodeKind.DollarName, null, "$"),
                new(NodeKind.TextLiteral, null, "c"),
            });
            expect("a$xc f", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a"),
                new(NodeKind.DollarName, null, "xc"),
                new(NodeKind.TextLiteral, null, " f"),
            });
            expect("a$x\\c f", false, new Node[] {
                new(NodeKind.TextLiteral, null, "a"),
                new(NodeKind.DollarName, null, "x"),
                new(NodeKind.TextLiteral, null, "c f"),
            });
            expect("$y01\\23", false, new Node[] {
                new(NodeKind.DollarName, null, "y01"),
                new(NodeKind.TextLiteral, null, "23"),
            });
            expect(" $y01\\23", false, new Node[] {
                new(NodeKind.TextLiteral, null, " "),
                new(NodeKind.DollarName, null, "y01"),
                new(NodeKind.TextLiteral, null, "23"),
            });
            expect(@"\\", false, new Node[] {
                new(NodeKind.TextLiteral, null, @"\"),
            });
            expect(@"\\\", false, new Node[] {
                new(NodeKind.TextLiteral, null, @"\"),
            });
            expect(@"\\\$x", false, new Node[] {
                new(NodeKind.TextLiteral, null, @"\$x"),
            });
            expect(@"\\\$f ", false, new Node[] {
                new(NodeKind.TextLiteral, null, @"\$f "),
            });
            expect(@"\\$f ", false, new Node[] {
                new(NodeKind.TextLiteral, null, @"\"),
                new(NodeKind.DollarName, null, "f"),
                new(NodeKind.TextLiteral, null, " "),
            });

            Debug.WriteLine("Done " + nameof(_test_parseString));
        }
    }
}
