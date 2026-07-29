namespace Pidamg.Expressions;

internal sealed record UnaryExpression(string Operator, IEvaluable Operand) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
    {
        var val = Operand.Evaluate(context);
        return Operator switch
        {
            "!" => !ValueCoercion.IsTruthy(val),
            "-" => NumericOperations.Negate(val),
            _ => throw new EvaluationException($"Unknown unary operator '{Operator}'."),
        };
    }
}
