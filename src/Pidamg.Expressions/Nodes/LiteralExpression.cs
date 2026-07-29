namespace Pidamg.Expressions;

internal sealed record LiteralExpression(object? Value) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context) => Value;
}
