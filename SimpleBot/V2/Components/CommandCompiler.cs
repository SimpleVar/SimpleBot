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

        public static CommandHandler.Callback CreateHandler(ReadOnlySpan<char> input, int index)
        {
            // TODO handle semantic errors
            Ctx ctx = new Ctx(input, index);
            Node root = parseRoot(ref ctx);
            if (root.Children.Count == 0)
                return null;

            var varsExpr = Expression.Parameter(typeof(Dictionary<string, object>), "vars");
            var chatterExpr = Expression.Parameter(typeof(Chatter), "chatter");
            var codeExpr = Build(root, varsExpr, chatterExpr);
            var code = (Action<Dictionary<string, object>, Chatter>)Expression.Lambda(codeExpr, varsExpr, chatterExpr).Compile();
            var inclDollarNames = ctx.referencedDollarNames.ToArray();

            // Callback(Chatter chatter, string cmdAlias, List<string> args, string argsStr);
            CommandHandler.Callback cb = (chatter, cmdAlias, args, argsStr) =>
            {
                var vars = new Dictionary<string, object> { { "$", args.Count }, { "0", argsStr } };
                for (int i = 0; i < 9 && i < args.Count; i++)
                    vars.Add((i + 1) + "", args[i]);

                string targetName = args.Count == 0 ? null : args[0].CanonicalUsername();

                var getTargetInfo = Extensions.Cached(() => Bot.ONE.tw.GetChannelInfo(targetName));
                var getChatterInfo = Extensions.Cached(() => Bot.ONE.tw.GetChannelInfo(chatter.name));
                var getTargetChatter = Extensions.Cached(() => Bot.ONE.chatters.GetOrNull(targetName));

                for (int i = 0; i < inclDollarNames.Length; i++)
                {
                    var name = inclDollarNames[i];
                    switch (name)
                    {
                        case "query": vars.Add(name, HttpUtility.UrlEncode(argsStr) ?? ""); break;
                        case "channel": vars.Add(name, Bot.ONE.Channel); break;
                        case "channel_id": vars.Add(name, Bot.ONE.ChannelId); break;

                        case "user_name": vars.Add(name, chatter.DisplayName); break;
                        case "user_id": vars.Add(name, chatter.uid); break;
                        case "user_game": vars.Add(name, getChatterInfo()?.GameName ?? "<no game>"); break;
                        case "user_title": vars.Add(name, getChatterInfo()?.Title ?? "<no title>"); break;
                        case "user_level": vars.Add(name, chatter.userLevel.ToString()); break;

                        case "target_name": vars.Add(name, getTargetChatter()?.DisplayName ?? (args.Count == 0 ? null : args[0].CleanUsername()) ?? "<no target>"); break;
                        case "target_id": vars.Add(name, Bot.ONE.tw.GetUserId(targetName) ?? "<not found>"); break;
                        case "target_game": vars.Add(name, getTargetInfo()?.GameName ?? "<no game>"); break;
                        case "target_title": vars.Add(name, getTargetInfo()?.Title ?? "<no title>"); break;
                        case "target_level": vars.Add(name, getTargetChatter()?.userLevel.ToString() ?? "<not found>"); break;

                        case "targetorself_name": if (args.Count == 0) goto case "user_name"; else goto case "target_name";
                        case "targetorself_id": if (args.Count == 0) goto case "user_id"; else goto case "target_id";
                        case "targetorself_game": if (args.Count == 0) goto case "user_game"; else goto case "target_game";
                        case "targetorself_title": if (args.Count == 0) goto case "user_title"; else goto case "target_title";
                        case "targetorself_level": if (args.Count == 0) goto case "user_level"; else goto case "target_level";

                        case "randomchatter": vars.Add(name, Bot.ONE.chatters.RandomChatter()?.DisplayName ?? "<nobody>"); break;
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

        static float Obj2Float(object o)
        {
            if (o == null) return float.NaN;
            if (o.GetType() == typeof(float))
                return (float)o;
            return float.TryParse(Convert.ToString(o), out float val) ? val : float.NaN;
        }
        static string Obj2Str(object o) => o?.ToString();
        static float Obj2Bool(object o) => o == null ? 0 : (o.GetType() == typeof(float) ? ((float)o == 0 ? 0 : 1) : (o.GetType() == typeof(string) ? (string.IsNullOrEmpty(o as string) ? 0 : 1) : 1));
        static readonly MethodInfo _strConcatMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]);
        static readonly MethodInfo _obj2strMethod = typeof(CommandCompiler).GetMethod("Obj2Str", [typeof(object)]);
        static readonly MethodInfo _obj2floatMethod = typeof(CommandCompiler).GetMethod("Obj2Float", [typeof(object)]);
        static readonly MethodInfo _obj2boolMethod = typeof(CommandCompiler).GetMethod("Obj2Bool", [typeof(object)]);
        static readonly Expression<Action<Chatter, string, object[]>> _callExpr = (chatter, cmdName, args) => Call(chatter, cmdName, args);
        static readonly Expression<Func<Dictionary<string, object>, string, object>> _varsGetValOrNull = (vars, key) => vars.ContainsKey(key) ? vars[key] : null;
        
        static void Call(Chatter chatter, string func, object[] args)
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
                case NodeKind.ExprList:
                    var exprs = new Expression[node.Children.Count];
                    for (int i = 0; i < exprs.Length; i++)
                        exprs[i] = Build(node.Children[i], vars, chatter);
                    return Expression.NewArrayInit(typeof(object), exprs);
                case NodeKind.Call:
                    return Expression.Invoke(_callExpr, chatter, Build(node.Children[0], vars, chatter), Build(node.Children[1], vars, chatter));
                case NodeKind.String:
                    Expression concat = Expression.Constant("", typeof(string));
                    for (int i = 0; i < node.Children.Count; i++)
                        concat = Expression.Add(concat, Build(node.Children[i], vars, chatter), _strConcatMethod);
                    return concat;
                case NodeKind.DollarName:
                    return Expression.Invoke(_varsGetValOrNull, vars, Expression.Constant(node.StrValue, typeof(string)));
                case NodeKind.Identifier:
                    return Expression.Constant(node.StrValue, typeof(string));
                case NodeKind.TextLiteral:
                    return Expression.Constant(node.StrValue, typeof(string));
                case NodeKind.NumberLiteral:
                    return Expression.Constant(node.FloatValue, typeof(float));
                case NodeKind.NullLiteral:
                    return Expression.Constant(null, typeof(object));
                case NodeKind.CastToString:
                    return Expression.Convert(Build(node.Children[0], vars, chatter), typeof(string), _obj2strMethod);
                case NodeKind.Expr_Group:
                    return Build(node.Children[0], vars, chatter);
                case NodeKind.Expr_MemberAccess:
                    throw new NotImplementedException();
                case NodeKind.Expr_Indexer:
                    throw new NotImplementedException();
                case NodeKind.Expr_PrefixMinus:
                    return Expression.Negate(Expression.Convert(Build(node.Children[0], vars, chatter), typeof(float), _obj2floatMethod));
                case NodeKind.Expr_PrefixPlus:
                    return Expression.Convert(Build(node.Children[0], vars, chatter), typeof(float), _obj2floatMethod);
                case NodeKind.Expr_PrefixDec:
                    // Expression.PreDecrementAssign
                    throw new NotImplementedException();
                case NodeKind.Expr_PrefixInc:
                    throw new NotImplementedException();
                case NodeKind.Expr_PrefixBang:
                    return Expression.Subtract(Expression.Constant(1.0f, typeof(float)), Expression.Convert(Build(node.Children[0], vars, chatter), typeof(float), _obj2boolMethod));
                default:
                    throw new ApplicationException($"Unhandled {nameof(NodeKind)} {node.Kind}");
            }
        }

        public enum NodeKind
        {
            Invalid,
            StatementList,
            ExprList,
            Call,
            String,
            DollarName,
            Identifier,
            TextLiteral,
            NumberLiteral,
            NullLiteral,
            CastToString,
            Expr_Group,
            Expr_MemberAccess,
            Expr_Indexer,
            Expr_PrefixMinus,
            Expr_PrefixPlus,
            Expr_PrefixDec,
            Expr_PrefixInc,
            Expr_PrefixBang,
            Expr_Add, // TODO ...
        }
        public record Node(NodeKind Kind, List<Node> Children = null, string StrValue = null)
        {
            public string StrValue { get; init; } = StrValue;
            public float FloatValue { get; init; }
            public Node Copy() => new(Kind, Children?.Select(x => x.Copy())?.ToList(), StrValue);
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
                    throw new ApplicationException("TODO illegal token, expected '" + c + "'");
                i++;
            }
            public void skipWhitespace()
            {
                while (i < input.Length && char.IsWhiteSpace(input[i]))
                    i++;
            }
            public readonly bool lookahead(int indexOffset, char c1, char c2) => lookahead(indexOffset) == c1 && lookahead(indexOffset + 1) == c2;
            public readonly bool lookahead(int indexOffset, char c) => lookahead(indexOffset) == c;
            public readonly char lookahead(int indexOffset) => i + indexOffset < input.Length ? input[i + indexOffset] : '\0';
            public readonly char peak() => i < input.Length ? input[i] : '\0';
            public static bool isAlpha(char c) => c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            public static bool isDigit(char c) => c >= '0' && c <= '9';
            public static int tryParseHexaDigit(char c)
            {
                if (isDigit(c))
                    return c - '0';
                int x = (c | 32) - 'a'; // c|32 == ToLowerCase
                if (x >= 0 && x < 6)
                    return 10 + x;
                return -1;
            }
        }

        static Node parseRoot(ref Ctx ctx)
        {
            Node codeBlock = new(NodeKind.StatementList, []);
            parseCodeBlock(ref ctx, codeBlock.Children);
            
            Node extraString = parseString(ref ctx, false);
            if (extraString != null)
            {
                codeBlock.Children.Add(new(NodeKind.Call, [
                    new(NodeKind.Identifier, null, "chat"),
                    new(NodeKind.ExprList, [extraString])
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
            ctx.skipWhitespace();
            ctx.requireChar('}');
            ctx.skipWhitespace();
        }
        
        static Node parseStatement(ref Ctx ctx)
        {
            // ; <expression>;    :: has to be "call", "assignment", "increment", "decrement", or something that actually does a thing

            // Skip parsing code D:
            //ctx.skipWhitespace(); while (ctx.i < ctx.input.Length && ctx.input[ctx.i] != '}') ctx.i++; return null;
            
            var expr = parseExpression(ref ctx);
            if (expr == null)
                return null;
            ctx.skipWhitespace();
            if (ctx.peak() != '}')
                ctx.requireChar(';');
            return expr;
        }

        static List<Node> parseExpressionList(ref Ctx ctx)
        {
            // ; <expr> , <expr> , ...
            List<Node> nodes = [];
            while (true)
            {
                var expr = parseExpression(ref ctx);
                if (expr == null)
                    break;
                nodes.Add(expr);
                ctx.skipWhitespace();
                if (ctx.peak() != ',')
                    break;
                ctx.i++;
            }
            return nodes;
        }

        // TODO
        static Node parseExpression(ref Ctx ctx)
        {
            // ; <atom>
            // ; <> <binary-op> <>
            // ; <> ( <exprList> )

            // atom may be degenerated to null, meaning no expression

            ctx.skipWhitespace();
            var atom = parseAtom(ref ctx);
            ctx.skipWhitespace();
            return atom;
        }

        static Node parseAtom(ref Ctx ctx) => parseDegenerateAtom_PrefixOp(ref ctx);

        #region Atom Degenerecy

        static Node parseDegenerateAtom_PrefixOp(ref Ctx ctx)
        {
            // ; !<prefixOp>
            // | -<prefixOp>
            // | +<prefixOp>
            // | --<prefixOp>
            // | ++<prefixOp>
            // | <memberAccessOrIndexer>
            switch (ctx.peak())
            {
                case '!':
                    ctx.i++;
                    ctx.skipWhitespace();
                    return new Node(NodeKind.Expr_PrefixBang, [parseDegenerateAtom_PrefixOp(ref ctx)]);
                case '-':
                    if (ctx.peak() == ctx.lookahead(1))
                    {
                        ctx.i += 2;
                        ctx.skipWhitespace();
                        return new Node(NodeKind.Expr_PrefixDec, [parseDegenerateAtom_PrefixOp(ref ctx)]);
                    }
                    ctx.i++;
                    ctx.skipWhitespace();
                    return new Node(NodeKind.Expr_PrefixMinus, [parseDegenerateAtom_PrefixOp(ref ctx)]);
                case '+':
                    if (ctx.peak() == ctx.lookahead(1))
                    {
                        ctx.i += 2;
                        ctx.skipWhitespace();
                        return new Node(NodeKind.Expr_PrefixInc, [parseDegenerateAtom_PrefixOp(ref ctx)]);
                    }
                    ctx.i++;
                    ctx.skipWhitespace();
                    return new Node(NodeKind.Expr_PrefixPlus, [parseDegenerateAtom_PrefixOp(ref ctx)]);
                default:
                    return parseDegenerateAtom_MemberAccessOrIndexer(ref ctx);
            }
        }

        static Node parseDegenerateAtom_MemberAccessOrIndexer(ref Ctx ctx)
        {
            // ; <groupExpr>
            // | <>.<identifier>
            // | <>[<expr>]
            var expr = parseDegenerateAtom_GroupExpr(ref ctx);
            while (true)
            {
                ctx.skipWhitespace();
                switch (ctx.peak())
                {
                    case '.':
                        if (Ctx.isDigit(ctx.lookahead(1)))
                            return expr; // .5 is not identifier
                        ctx.i++;
                        ctx.skipWhitespace();
                        int len = ctx.peakIdentifierLength(0);
                        if (len == 0)
                            throw new Exception("Member access - expected identifier");
                        var memberName = new Node(NodeKind.Identifier, null, ctx.input.Slice(ctx.i, len).ToString());
                        ctx.i += len;
                        expr = new Node(NodeKind.Expr_MemberAccess, [expr, memberName]);
                        continue;
                    case '[':
                        ctx.i++;
                        ctx.skipWhitespace();
                        var index = parseExpression(ref ctx);
                        ctx.skipWhitespace();
                        ctx.requireChar(']');
                        expr = new Node(NodeKind.Expr_Indexer, [expr, index]);
                        break;
                    default:
                        return expr;
                }
            }
        }

        static Node parseDegenerateAtom_GroupExpr(ref Ctx ctx)
        {
            // ; ( <expr> )
            // | <dollarParticle>
            // | <identifierOrLiteral>
            ctx.skipWhitespace();
            if (ctx.peak() == '(')
            {
                ctx.i++;
                ctx.skipWhitespace();
                var g = new Node(NodeKind.Expr_Group, [parseExpression(ref ctx)]);
                ctx.skipWhitespace();
                ctx.requireChar(')');
                return g;
            }

            var dollarParticle = tryParseDollarParticle(ref ctx);
            if (dollarParticle != null)
                return dollarParticle;

            return parseDegenerateAtom_IdentifierOrLiteral(ref ctx);
        }

        static Node parseDegenerateAtom_IdentifierOrLiteral(ref Ctx ctx)
        {
            int len = ctx.peakIdentifierLength(0);
            if (len == 0)
                return parseDegenerateAtom_LiteralArray(ref ctx);
            var identifier = ctx.input.Slice(ctx.i, len).ToString();
            ctx.i += len;
            if (identifier == "null") return new Node(NodeKind.NullLiteral);
            if (identifier == "NaN") return new Node(NodeKind.NumberLiteral) { FloatValue = float.NaN };
            return new Node(NodeKind.Identifier, null, identifier);
        }

        static Node parseDegenerateAtom_LiteralArray(ref Ctx ctx)
        {
            // ; [<exprList>]
            // | <literalStrOrNum>
            if (ctx.peak() != '[')
                return parseDegenerateAtom_LiteralStrOrNum(ref ctx);
            ctx.i++;
            ctx.skipWhitespace();
            var elements = parseExpressionList(ref ctx);
            ctx.skipWhitespace();
            ctx.requireChar(']');
            return new Node(NodeKind.ExprList, elements);
        }

        static Node parseDegenerateAtom_LiteralStrOrNum(ref Ctx ctx)
        {
            // ; "text"
            // | <number>
            if (ctx.peak() == '"')
            {
                ctx.i++;
                return parseString(ref ctx, true);
            }
            return tryParseLiteralNum(ref ctx);
        }

        #endregion

        static Node tryParseLiteralNum(ref Ctx ctx)
        {
            // 1
            // 1.5
            // 1.5e+10
            // 1.5e10
            // .5
            // 0xDEADBEEF
            // 0b11110000

            // hexadecimal
            if (ctx.lookahead(0, '0', 'x'))
            {
                ctx.i += 2;
                float val = Ctx.tryParseHexaDigit(ctx.peak());
                if (val == -1)
                    throw new Exception("Invalid hexadecimal number");
                ctx.i++;
                while (true)
                {
                    int digit = Ctx.tryParseHexaDigit(ctx.peak());
                    if (digit == -1)
                        break;
                    val *= 16;
                    val += digit;
                    ctx.i++;
                }
                return new Node(NodeKind.NumberLiteral) { FloatValue = val };
            }
            // binary
            if (ctx.lookahead(0, '0', 'b'))
            {
                ctx.i += 2;
                char c = ctx.peak();
                if (c != '0' && c != '1')
                    throw new Exception("Invalid binary number");
                float val = 0;
                while (c == '0' || c == '1')
                {
                    val *= 2;
                    val += c - '0';
                    ctx.i++;
                    c = ctx.peak();
                }
                return new Node(NodeKind.NumberLiteral) { FloatValue = val };
            }
            // floating point
            {
                float val = 0;
                bool isValid = false;
                char c = ctx.peak();
                while (Ctx.isDigit(c))
                {
                    isValid = true;
                    val *= 10;
                    val += c - '0';
                    ctx.i++;
                    c = ctx.peak();
                }
                if (ctx.peak() == '.' && Ctx.isDigit(ctx.lookahead(1)))
                {
                    isValid = true;
                    ctx.i++;
                    c = ctx.peak();
                    float fractionPower = 0.1f;
                    float fractionVal = 0;
                    while (Ctx.isDigit(c))
                    {
                        fractionVal += fractionPower * (c - '0');
                        fractionPower *= 0.1f;
                        ctx.i++;
                        c = ctx.peak();
                    }
                    val += fractionVal;
                }
                // make sure we have parsed any digits
                if (isValid)
                {
                    c = ctx.peak();
                    bool isSign = c == '-' || c == '+';
                    // scientific notation
                    if (c == 'e' || (isSign && ctx.lookahead(1, 'e')))
                    {
                        bool isPositive = c != '-';
                        if (isSign) ctx.i++; // +-
                        ctx.i++; // e
                        float exponent = 0;
                        c = ctx.peak();
                        if (!Ctx.isDigit(c))
                            throw new Exception("Invalid number scientific notation");
                        while (Ctx.isDigit(c))
                        {
                            exponent *= 10;
                            exponent += c - '0';
                            ctx.i++;
                            c = ctx.peak();
                        }
                        float factor = MathF.Pow(10, exponent);
                        if (isPositive)
                            val *= factor;
                        else
                            val /= factor;
                    }
                    return new Node(NodeKind.NumberLiteral) { FloatValue = val };
                }
            }

            return null;
        }

        static Node tryParseDollarParticle(ref Ctx ctx)
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
                ctx.skipWhitespace();
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
                Node dollarNode = tryParseDollarParticle(ref ctx);
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
                    if (_actual.Kind != _expected.Kind || _actual.StrValue != _expected.StrValue)
                        throw new ApplicationException();
                }
                Debug.WriteLine(input);
                for (int j = 0; j < node.Children.Count; j++)
                    Debug.WriteLine($"  :: {node.Children[j].Kind} :: '{node.Children[j].StrValue}'");
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
