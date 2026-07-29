using System.Collections;

namespace Pidamg.Expressions;

/// <summary>
/// <c>target[index]</c>. Null-safe: a null target yields null. A dictionary target resolves by key
/// (absent → null); a list/array target resolves by integer index (out of range → null).
/// </summary>
internal sealed record IndexExpression(IEvaluable Target, IEvaluable Index) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
    {
        var target = Target.Evaluate(context);
        if (target is null) return null;                                  // null-safe navigation

        var index = Index.Evaluate(context);

        if (target is IDictionary map)
            return index is not null && map.Contains(index) ? map[index] : null;

        if (target is IList list)
        {
            var i = ValueCoercion.Convert<int?>(index);
            return i is int n && n >= 0 && n < list.Count ? list[n] : null;   // out of range → null
        }

        throw new EvaluationException($"Cannot index into '{target.GetType().Name}'.");
    }
}
