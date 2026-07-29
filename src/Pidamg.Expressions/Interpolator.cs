using System.Text;

namespace Pidamg.Expressions;

/// <summary>
/// Resolves a raw template string — literal text mixed with <c>${expr}</c> placeholders — into an
/// <see cref="IEvaluable"/>, applying the native-vs-string rule <em>at parse time</em> (so a template is
/// parsed once and evaluated many):
/// <list type="bullet">
///   <item>a lone <c>${expr}</c> with no surrounding text → the expression itself (its native typed value);</item>
///   <item>plain text with no placeholder (e.g. <c>"vsphere:cluster"</c>) → that string verbatim;</item>
///   <item>text with one or more placeholders (<c>"a-${x}"</c>, <c>"${x}${y}"</c>) → a concatenated string.</item>
/// </list>
/// This is the envelope around <see cref="ExpressionParser"/>: it owns the <c>${...}</c> syntax, the parser
/// only ever sees the bare expression inside each placeholder. Decoupled from YAML.
/// </summary>
public static class Interpolator
{
    /// <summary>Parse a template into a single evaluable encoding the native-vs-string rule.</summary>
    public static IEvaluable Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parts = ParseParts(text);
        return parts.Count switch
        {
            0 => new LiteralExpression(string.Empty),
            1 => parts[0],                                  // lone ${expr} → native; lone literal → string
            _ => new InterpolatedStringExpression(parts),   // mixed → string
        };
    }

    /// <summary>Parse a template and coerce its result to <typeparamref name="T"/> (literal or expression).</summary>
    public static IExpression<T> Parse<T>(string text) => Parse(text).AsTyped<T>();

    /// <summary>Parse and evaluate a template against <paramref name="context"/>.</summary>
    public static object? Evaluate(string text, IEvaluationContext context) => Parse(text).Evaluate(context);

    // Split into literal segments (LiteralExpression) and parsed ${expr} parts. Inside a placeholder,
    // nested braces and quoted strings are tracked so a '}' inside a string argument does not close it.
    private static List<IEvaluable> ParseParts(string text)
    {
        var parts = new List<IEvaluable>();
        var literal = new StringBuilder();
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] == '$' && i + 1 < text.Length && text[i + 1] == '{')
            {
                if (literal.Length > 0)
                {
                    parts.Add(new LiteralExpression(literal.ToString()));
                    literal.Clear();
                }

                var start = i + 2;   // past '${'
                i = start;
                var depth = 1;
                var quote = '\0';
                while (i < text.Length && depth > 0)
                {
                    var c = text[i];
                    if (quote != '\0')
                    {
                        if (c == '\\' && i + 1 < text.Length) i += 2;   // skip escape inside a string
                        else { if (c == quote) quote = '\0'; i++; }
                    }
                    else if (c is '"' or '\'') { quote = c; i++; }
                    else if (c == '{') { depth++; i++; }
                    else if (c == '}') { depth--; i++; }
                    else i++;
                }
                if (depth != 0)
                    throw new EvaluationException($"Unterminated '${{' at position {start - 2}.");

                parts.Add(ExpressionParser.Parse(text[start..(i - 1)]));   // i-1 = closing '}'
            }
            else
            {
                literal.Append(text[i]);
                i++;
            }
        }

        if (literal.Length > 0)
            parts.Add(new LiteralExpression(literal.ToString()));

        return parts;
    }
}
