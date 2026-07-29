using System.Globalization;
using System.Reflection;

namespace Pidamg.Expressions;

/// <summary>
/// <c>target.Method(args)</c> (the common case) or a direct call on a delegate value. Public instance
/// methods only (purity). Null-safe: a null target/callee yields null. Overload resolution matches by name
/// and arity, supports optional parameters and a trailing <c>params</c> array (variadics), and coerces
/// arguments to the parameter types.
/// </summary>
internal sealed record CallExpression(IEvaluable Callee, IReadOnlyList<IEvaluable> Arguments) : IEvaluable
{
    public object? Evaluate(IEvaluationContext context)
    {
        var rawArgs = Arguments.Select(a => a.Evaluate(context)).ToArray();

        if (Callee is MemberAccessExpression member)
        {
            var target = member.Target.Evaluate(context);
            if (target is null) return null;                          // null-safe: a.m() with a == null → null
            return InvokeMethod(target, member.Member, rawArgs);
        }

        var callee = Callee.Evaluate(context);
        if (callee is null) return null;                              // null-safe
        if (callee is Delegate del) return del.DynamicInvoke(rawArgs);
        throw new EvaluationException("Expression is not callable.");
    }

    private static object? InvokeMethod(object target, string methodName, object?[] args)
    {
        var type = target.GetType();
        var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == methodName && CanAccept(m.GetParameters(), args.Length))
            .ToArray();

        if (candidates.Length == 0)
            throw new EvaluationException(
                $"Method '{methodName}' accepting {args.Length} argument(s) not found on '{type.Name}'.");

        foreach (var method in candidates)
        {
            try
            {
                return method.Invoke(target, BindArgs(method.GetParameters(), args));
            }
            catch (TargetInvocationException tie)
            {
                // The method ran and threw — a real error, not an arg mismatch.
                var inner = tie.InnerException ?? tie;
                throw new EvaluationException($"Error calling '{methodName}': {inner.Message}", inner);
            }
            catch (EvaluationException) { throw; }
            catch { /* argument binding failed → try the next overload */ }
        }

        throw new EvaluationException(
            $"No matching overload of '{methodName}' on '{type.Name}' for the provided arguments.");
    }

    private static bool CanAccept(ParameterInfo[] parameters, int count)
        => IsParams(parameters)
            ? count >= parameters.Length - 1                                     // trailing params collects the rest
            : count <= parameters.Length && parameters.Skip(count).All(p => p.HasDefaultValue);

    private static bool IsParams(ParameterInfo[] parameters)
        => parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), inherit: false);

    private static object?[] BindArgs(ParameterInfo[] parameters, object?[] args)
    {
        var bound = new object?[parameters.Length];

        if (IsParams(parameters))
        {
            var fixedCount = parameters.Length - 1;
            for (var i = 0; i < fixedCount; i++)
                bound[i] = i < args.Length ? Coerce(args[i], parameters[i].ParameterType) : parameters[i].DefaultValue;

            var elementType = parameters[^1].ParameterType.GetElementType()!;
            var rest = Array.CreateInstance(elementType, Math.Max(0, args.Length - fixedCount));
            for (var i = fixedCount; i < args.Length; i++)
                rest.SetValue(Coerce(args[i], elementType), i - fixedCount);
            bound[^1] = rest;
            return bound;
        }

        for (var i = 0; i < parameters.Length; i++)
            bound[i] = i < args.Length ? Coerce(args[i], parameters[i].ParameterType) : parameters[i].DefaultValue;
        return bound;
    }

    private static object? Coerce(object? arg, Type paramType)
        => arg is null || paramType.IsAssignableFrom(arg.GetType())
            ? arg
            : System.Convert.ChangeType(arg, paramType, CultureInfo.InvariantCulture);
}
