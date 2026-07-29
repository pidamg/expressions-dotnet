namespace Pidamg.Expressions;

/// <summary>
/// Bridges the untyped <see cref="IEvaluable"/> and the typed <see cref="IExpression{T}"/> contracts. The shared
/// adapter is used both by the parser's <c>Parse&lt;T&gt;</c> factories and by consumers holding an
/// <em>already-compiled</em> <see cref="IEvaluable"/> that need the typed contract. The concrete wrapper
/// remains a private implementation detail.
/// </summary>
public static class EvaluableExtensions
{
    /// <summary>View this evaluable as a typed expression: evaluate, then coerce to <typeparamref name="T"/>.</summary>
    public static IExpression<T> AsTyped<T>(this IEvaluable evaluable)
    {
        ArgumentNullException.ThrowIfNull(evaluable);
        return new Typed<T>(evaluable);
    }

    // A thin behavioural adapter, not a value — no value equality, hence a class, not a record.
    private sealed class Typed<T>(IEvaluable inner) : IExpression<T>
    {
        public T? Evaluate(IEvaluationContext context) => inner.Evaluate<T>(context);
    }
}
