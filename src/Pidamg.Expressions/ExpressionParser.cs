using System.Globalization;

namespace Pidamg.Expressions;

/// <summary>
/// Pratt parser turning an expression string into an <see cref="IEvaluable"/> AST (parse once, evaluate
/// many). <see cref="Parse{T}"/> wraps the result in a typed <see cref="IExpression{T}"/>.
/// </summary>
public static class ExpressionParser
{
    // Left binding powers (higher = binds tighter).
    private static int Lbp(TokenKind kind) => kind switch
    {
        TokenKind.Question => 1,
        TokenKind.QuestionQuestion => 2,
        TokenKind.PipePipe => 3,
        TokenKind.AmpAmp => 4,
        TokenKind.EqualEqual or TokenKind.BangEqual => 5,
        TokenKind.Less or TokenKind.LessEqual or TokenKind.Greater or TokenKind.GreaterEqual => 6,
        TokenKind.Plus or TokenKind.Minus => 7,
        TokenKind.Star or TokenKind.Slash => 8,
        TokenKind.Dot or TokenKind.LeftParen or TokenKind.LeftBracket => 10,
        _ => 0,
    };

    private const int UnaryBp = 9;   // tighter than '*'/'/', looser than postfix '.'/'('/'['

    /// <summary>Parse <paramref name="input"/> into an untyped AST.</summary>
    public static IEvaluable Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var tokens = new Lexer(input).Tokenize();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression(0);
        if (parser.Current.Kind != TokenKind.Eof)
            throw new EvaluationException(
                $"Unexpected token '{parser.Current.Text}' at position {parser.Current.Position}.");
        return expr;
    }

    /// <summary>Parse and wrap as a typed expression that coerces its result to <typeparamref name="T"/>.</summary>
    public static IExpression<T> Parse<T>(string input) => Parse(input).AsTyped<T>();

    private sealed class Parser(IReadOnlyList<Token> tokens)
    {
        private int _index;

        public Token Current => _index < tokens.Count ? tokens[_index] : new Token(TokenKind.Eof, "", -1);

        private Token Consume()
        {
            var t = Current;
            _index++;
            return t;
        }

        private Token Expect(TokenKind kind)
        {
            if (Current.Kind != kind)
                throw new EvaluationException(
                    $"Expected {kind} but got '{Current.Text}' at position {Current.Position}.");
            return Consume();
        }

        public IEvaluable ParseExpression(int minBp)
        {
            var left = ParsePrefix();

            while (true)
            {
                var op = Current;
                var bp = Lbp(op.Kind);
                if (bp <= minBp) break;
                Consume();

                switch (op.Kind)
                {
                    case TokenKind.Dot:
                        var member = Expect(TokenKind.Identifier);
                        IEvaluable access = new MemberAccessExpression(left, member.Text);
                        if (Current.Kind == TokenKind.LeftParen)
                            access = ParseCall(access);
                        left = access;
                        break;

                    case TokenKind.LeftParen:
                        left = ParseCallArgs(left);   // direct call on an already-parsed callee
                        break;

                    case TokenKind.LeftBracket:
                        var index = ParseExpression(0);
                        Expect(TokenKind.RightBracket);
                        left = new IndexExpression(left, index);
                        break;

                    case TokenKind.Question:
                        var then = ParseExpression(0);
                        Expect(TokenKind.Colon);
                        var @else = ParseExpression(0);
                        left = new TernaryExpression(left, then, @else);
                        break;

                    case TokenKind.QuestionQuestion:
                        // Right-associative: a ?? b ?? c == a ?? (b ?? c).
                        left = new CoalesceExpression(left, ParseExpression(bp - 1));
                        break;

                    default:
                        left = new BinaryExpression(left, op.Text, ParseExpression(bp));
                        break;
                }
            }

            return left;
        }

        private IEvaluable ParsePrefix()
        {
            var tok = Current;
            switch (tok.Kind)
            {
                case TokenKind.Bang:
                    Consume();
                    return new UnaryExpression("!", ParseExpression(UnaryBp));
                case TokenKind.Minus:
                    Consume();
                    return new UnaryExpression("-", ParseExpression(UnaryBp));

                case TokenKind.LeftParen:
                    Consume();
                    var inner = ParseExpression(0);
                    Expect(TokenKind.RightParen);
                    return inner;

                case TokenKind.Integer:
                    Consume();
                    return int.TryParse(tok.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                        ? new LiteralExpression(i)
                        : throw new EvaluationException(
                            $"Integer literal '{tok.Text}' at position {tok.Position} is out of range.");

                case TokenKind.Float:
                    Consume();
                    return double.TryParse(tok.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                        ? new LiteralExpression(d)
                        : throw new EvaluationException(
                            $"Float literal '{tok.Text}' at position {tok.Position} is out of range.");

                case TokenKind.String:
                    Consume();
                    return new LiteralExpression(tok.Text);
                case TokenKind.True:
                    Consume();
                    return new LiteralExpression(true);
                case TokenKind.False:
                    Consume();
                    return new LiteralExpression(false);
                case TokenKind.Null:
                    Consume();
                    return new LiteralExpression(null);

                case TokenKind.Identifier:
                    Consume();
                    IEvaluable node = new IdentifierExpression(tok.Text);
                    if (Current.Kind == TokenKind.LeftParen)
                        node = ParseCall(node);
                    return node;

                default:
                    throw new EvaluationException($"Unexpected token '{tok.Text}' at position {tok.Position}.");
            }
        }

        private IEvaluable ParseCall(IEvaluable callee)
        {
            Expect(TokenKind.LeftParen);
            return ParseCallArgs(callee);
        }

        private IEvaluable ParseCallArgs(IEvaluable callee)
        {
            var args = new List<IEvaluable>();
            while (Current.Kind != TokenKind.RightParen && Current.Kind != TokenKind.Eof)
            {
                args.Add(ParseExpression(0));

                if (Current.Kind == TokenKind.Comma)
                {
                    Consume();
                    continue;
                }

                if (Current.Kind != TokenKind.RightParen)
                    throw new EvaluationException(
                        $"Expected comma or {TokenKind.RightParen} but got '{Current.Text}' " +
                        $"at position {Current.Position}.");
            }
            Expect(TokenKind.RightParen);
            return new CallExpression(callee, args);
        }
    }
}
