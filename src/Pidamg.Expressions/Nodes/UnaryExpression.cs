namespace Pidamg.Expressions;

internal sealed record UnaryExpression(string Operator, IEvaluable Operand) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
    {
        var val = Operand.Evaluate(context);
        return Operator switch
        {
            "!" => !ValueCoercion.IsTruthy(val),
            "-" => val switch
            {
                // Cast each arm to object so the switch does not unify int/long/double to double.
                int i => (object)(-i),
                long l => -l,
                double d => -d,
                _ => throw new EvaluationException($"Cannot negate '{val?.GetType().Name}'."),
            },
            _ => throw new EvaluationException($"Unknown unary operator '{Operator}'."),
        };
    }
}
