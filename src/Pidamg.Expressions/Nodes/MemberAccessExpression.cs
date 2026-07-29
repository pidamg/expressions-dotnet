using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pidamg.Expressions;

/// <summary>
/// <c>target.Member</c>. Null-safe: a null target yields null. A string-keyed dictionary target (a dynamic
/// bag) resolves by key, an absent key yielding null. Otherwise reflection over <em>public instance</em>
/// members (case-sensitive); a member missing on such a typed object throws.
/// </summary>
internal sealed record MemberAccessExpression(IEvaluable Target, string Member) : IEvaluable
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;
    private static readonly ConditionalWeakTable<Type, ConcurrentDictionary<string, MemberInfo>> MemberCache = new();

    public object? Evaluate(IEvaluationContext context)
    {
        var target = Target.Evaluate(context);
        if (target is null) return null;                                   // null-safe navigation

        if (target is IDictionary bag)                                     // dynamic bag → key lookup
            return bag.Contains(Member) ? bag[Member] : null;

        var type = target.GetType();
        var members = MemberCache.GetValue(type, static _ => new ConcurrentDictionary<string, MemberInfo>());
        if (!members.TryGetValue(Member, out var member))
        {
            member = (MemberInfo?)type.GetProperty(Member, Flags) ?? type.GetField(Member, Flags);
            if (member is null)
                throw new EvaluationException($"Member '{Member}' not found on type '{type.Name}'.");
            members.TryAdd(Member, member);
        }

        try
        {
            return member switch
            {
                PropertyInfo property => property.GetValue(target),
                FieldInfo field => field.GetValue(target),
                _ => throw new EvaluationException(
                    $"Member '{Member}' on type '{type.Name}' is not readable."),
            };
        }
        catch (TargetInvocationException exception)
        {
            var inner = exception.InnerException ?? exception;
            throw new EvaluationException(
                $"Error reading member '{Member}' on '{type.Name}': {inner.Message}",
                inner);
        }
    }
}
