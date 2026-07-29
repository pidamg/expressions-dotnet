namespace Pidamg.Expressions;

/// <summary>Null-coalescing <c>a ?? b</c>: the left value if non-null, otherwise the right (short-circuit).</summary>
internal sealed record CoalesceExpression(IEvaluable Left, IEvaluable Right) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
        => Left.Evaluate(context) ?? Right.Evaluate(context);
}
