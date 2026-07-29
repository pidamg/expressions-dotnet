namespace Pidamg.Expressions;

internal sealed record BinaryExpression(IEvaluable Left, string Operator, IEvaluable Right) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
    {
        // Short-circuit logical operators (truthiness-based).
        if (Operator == "&&")
            return ValueCoercion.IsTruthy(Left.Evaluate(context)) && ValueCoercion.IsTruthy(Right.Evaluate(context));
        if (Operator == "||")
            return ValueCoercion.IsTruthy(Left.Evaluate(context)) || ValueCoercion.IsTruthy(Right.Evaluate(context));

        var l = Left.Evaluate(context);
        var r = Right.Evaluate(context);

        return Operator switch
        {
            "+" => Add(l, r),
            "-" => Arithmetic(l, r, static (a, b) => a - b, static (a, b) => a - b),
            "*" => Arithmetic(l, r, static (a, b) => a * b, static (a, b) => a * b),
            "/" => Arithmetic(l, r, static (a, b) => a / b, static (a, b) => a / b),
            "==" => ValueCoercion.AreEqual(l, r),
            "!=" => !ValueCoercion.AreEqual(l, r),
            // Ordering: a null/incompatible Compare → no value → false ("null is not orderable").
            "<" => ValueCoercion.Compare(l, r) is int c && c < 0,
            "<=" => ValueCoercion.Compare(l, r) is int c && c <= 0,
            ">" => ValueCoercion.Compare(l, r) is int c && c > 0,
            ">=" => ValueCoercion.Compare(l, r) is int c && c >= 0,
            _ => throw new EvaluationException($"Unknown binary operator '{Operator}'."),
        };
    }

    // '+' is string concatenation if either side is a string (null renders as empty), else numeric.
    private static object? Add(object? l, object? r) => (l, r) switch
    {
        (string sl, _) => sl + r?.ToString(),
        (_, string sr) => l?.ToString() + sr,
        _ => Arithmetic(l, r, static (a, b) => a + b, static (a, b) => a + b),
    };

    private static object Arithmetic(object? l, object? r,
        Func<int, int, object> intOp, Func<double, double, object> doubleOp) =>
        (ToNumber(l), ToNumber(r)) switch
        {
            (int il, int ir) => intOp(il, ir),
            (double dl, double dr) => doubleOp(dl, dr),
            (int il, double dr) => doubleOp(il, dr),
            (double dl, int ir) => doubleOp(dl, ir),
            _ => throw new EvaluationException(
                $"Cannot apply arithmetic to '{l?.GetType().Name}' and '{r?.GetType().Name}'."),
        };

    private static object? ToNumber(object? v) => v switch
    {
        int i => i,
        double d => d,
        long lg => (int)lg,
        _ => v,
    };
}
