using System.Text;

namespace Pidamg.Expressions;

/// <summary>Turns an expression string into a flat token stream (terminated by <see cref="TokenKind.Eof"/>).</summary>
internal sealed class Lexer(string input)
{
    private readonly string _input = input;
    private int _pos;

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        Token tok;
        do
        {
            tok = NextToken();
            tokens.Add(tok);
        }
        while (tok.Kind != TokenKind.Eof);
        return tokens;
    }

    private Token NextToken()
    {
        SkipWhitespace();

        if (_pos >= _input.Length)
            return new Token(TokenKind.Eof, "", _pos);

        var start = _pos;
        var c = _input[_pos];

        if (c is '"' or '\'')
            return ReadString(c, start);

        if (char.IsDigit(c))
            return ReadNumber(start);

        if (char.IsLetter(c) || c == '_')
            return ReadIdentifier(start);

        // Two-character operators
        if (_pos + 1 < _input.Length)
        {
            var next = _input[_pos + 1];
            if (c == '=' && next == '=') { _pos += 2; return new Token(TokenKind.EqualEqual, "==", start); }
            if (c == '!' && next == '=') { _pos += 2; return new Token(TokenKind.BangEqual, "!=", start); }
            if (c == '<' && next == '=') { _pos += 2; return new Token(TokenKind.LessEqual, "<=", start); }
            if (c == '>' && next == '=') { _pos += 2; return new Token(TokenKind.GreaterEqual, ">=", start); }
            if (c == '&' && next == '&') { _pos += 2; return new Token(TokenKind.AmpAmp, "&&", start); }
            if (c == '|' && next == '|') { _pos += 2; return new Token(TokenKind.PipePipe, "||", start); }
            if (c == '?' && next == '?') { _pos += 2; return new Token(TokenKind.QuestionQuestion, "??", start); }
        }

        // Single-character tokens
        _pos++;
        return c switch
        {
            '+' => new Token(TokenKind.Plus, "+", start),
            '-' => new Token(TokenKind.Minus, "-", start),
            '*' => new Token(TokenKind.Star, "*", start),
            '/' => new Token(TokenKind.Slash, "/", start),
            '<' => new Token(TokenKind.Less, "<", start),
            '>' => new Token(TokenKind.Greater, ">", start),
            '!' => new Token(TokenKind.Bang, "!", start),
            '?' => new Token(TokenKind.Question, "?", start),
            ':' => new Token(TokenKind.Colon, ":", start),
            '.' => new Token(TokenKind.Dot, ".", start),
            '(' => new Token(TokenKind.LeftParen, "(", start),
            ')' => new Token(TokenKind.RightParen, ")", start),
            '[' => new Token(TokenKind.LeftBracket, "[", start),
            ']' => new Token(TokenKind.RightBracket, "]", start),
            ',' => new Token(TokenKind.Comma, ",", start),
            _ => throw new EvaluationException($"Unexpected character '{c}' at position {start}."),
        };
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
            _pos++;
    }

    private Token ReadString(char quote, int start)
    {
        _pos++; // opening quote
        var sb = new StringBuilder();
        var closed = false;
        while (_pos < _input.Length)
        {
            var c = _input[_pos];
            if (c == '\\' && _pos + 1 < _input.Length)
            {
                sb.Append(_input[_pos + 1] switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '\'' => '\'',
                    '"' => '"',
                    var esc => esc,
                });
                _pos += 2;
            }
            else if (c == quote)
            {
                _pos++;
                closed = true;
                break;
            }
            else
            {
                sb.Append(c);
                _pos++;
            }
        }
        if (!closed)
            throw new EvaluationException($"Unterminated string literal at position {start}.");
        return new Token(TokenKind.String, sb.ToString(), start);
    }

    private Token ReadNumber(int start)
    {
        while (_pos < _input.Length && char.IsDigit(_input[_pos]))
            _pos++;

        if (_pos < _input.Length && _input[_pos] == '.' &&
            _pos + 1 < _input.Length && char.IsDigit(_input[_pos + 1]))
        {
            _pos++; // consume '.'
            while (_pos < _input.Length && char.IsDigit(_input[_pos]))
                _pos++;
            return new Token(TokenKind.Float, _input[start.._pos], start);
        }

        return new Token(TokenKind.Integer, _input[start.._pos], start);
    }

    private Token ReadIdentifier(int start)
    {
        while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_'))
            _pos++;

        var text = _input[start.._pos];
        var kind = text switch
        {
            "true" => TokenKind.True,
            "false" => TokenKind.False,
            "null" => TokenKind.Null,
            _ => TokenKind.Identifier,
        };
        return new Token(kind, text, start);
    }
}
