namespace Pidamg.Expressions;

/// <summary>
/// A parsed expression carrying its expected output type — the typed counterpart of <see cref="IEvaluable"/>.
/// Lets a consumer parse once and store the contract <c>IExpression&lt;T&gt;</c>, evaluating to
/// <typeparamref name="T"/> later without re-stating the type. Obtained via the parser's
/// <c>Parse&lt;T&gt;</c>.
/// </summary>
public interface IExpression<out T>
{
    /// <summary>Evaluate and coerce to <typeparamref name="T"/> (see <see cref="ValueCoercion.Convert{T}"/>).</summary>
    T? Evaluate(IEvaluationContext context);
}
