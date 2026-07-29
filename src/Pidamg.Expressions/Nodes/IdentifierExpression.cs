namespace Pidamg.Expressions;

/// <summary>A bare variable reference. The root must be bound — an undefined variable throws (navigation
/// through it is null-safe, but the root itself is not).</summary>
internal sealed record IdentifierExpression(string Name) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
        => context.TryGetValue(Name, out var value)
            ? value
            : throw new EvaluationException($"Undefined variable '{Name}'.");
}
