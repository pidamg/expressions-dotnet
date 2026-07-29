using System.Collections;
using System.Reflection;

namespace Pidamg.Expressions;

/// <summary>
/// <c>target.Member</c>. Null-safe: a null target yields null. A string-keyed dictionary target (a dynamic
/// bag) resolves by key, an absent key yielding null. Otherwise reflection over <em>public instance</em>
/// members (case-sensitive); a member missing on such a typed object throws.
/// </summary>
internal sealed record MemberAccessExpression(IEvaluable Target, string Member) : IEvaluable
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

    public object? Evaluate(IEvaluationContext context)
    {
        var target = Target.Evaluate(context);
        if (target is null) return null;                                   // null-safe navigation

        if (target is IDictionary bag)                                     // dynamic bag → key lookup
            return bag.Contains(Member) ? bag[Member] : null;

        var type = target.GetType();
        var prop = type.GetProperty(Member, Flags);
        if (prop is not null) return prop.GetValue(target);
        var field = type.GetField(Member, Flags);
        if (field is not null) return field.GetValue(target);

        throw new EvaluationException($"Member '{Member}' not found on type '{type.Name}'.");
    }
}
