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
            "-" or "*" or "/" => NumericOperations.ApplyBinary(l, r, Operator),
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
        (string sl, _) => sl + ValueCoercion.ToInvariantString(r),
        (_, string sr) => ValueCoercion.ToInvariantString(l) + sr,
        _ => NumericOperations.ApplyBinary(l, r, "+"),
    };
}
