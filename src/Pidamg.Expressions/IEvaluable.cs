namespace Pidamg.Expressions;

/// <summary>
/// A parsed expression node — the unit of <em>parse-once / evaluate-many</em>: the AST is independent
/// of any context and can be evaluated repeatedly against different <see cref="IEvaluationContext"/>s.
/// </summary>
/// <remarks>
/// The AST is dynamically typed (<see cref="Evaluate"/> returns <c>object?</c>): the same node can yield
/// different runtime types depending on the values. The desired output type is a <em>consumer</em>
/// concern, applied at evaluation through <see cref="Evaluate{T}"/> — there is no typed AST node.
/// </remarks>
public interface IEvaluable
{
    /// <summary>Evaluate this node against <paramref name="context"/>, yielding its runtime value.</summary>
    object? Evaluate(IEvaluationContext context);

    /// <summary>
    /// Evaluate, then coerce the result to <typeparamref name="T"/> with the expression coercion rules
    /// (strict parsing for boolean strings, numeric conversion, invariant formatting for <c>string</c>,
    /// …). A default interface method so every node gets it for free; see
    /// <see cref="ValueCoercion.Convert{T}"/>.
    /// </summary>
    T? Evaluate<T>(IEvaluationContext context) => ValueCoercion.Convert<T>(Evaluate(context));
}
