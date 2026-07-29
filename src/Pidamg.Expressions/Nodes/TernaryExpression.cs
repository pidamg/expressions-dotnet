namespace Pidamg.Expressions;

internal sealed record TernaryExpression(IEvaluable Condition, IEvaluable Then, IEvaluable Else) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
        => ValueCoercion.IsTruthy(Condition.Evaluate(context))
            ? Then.Evaluate(context)
            : Else.Evaluate(context);
}
