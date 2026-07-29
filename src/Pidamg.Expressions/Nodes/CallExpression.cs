using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pidamg.Expressions;

/// <summary>
/// <c>target.Method(args)</c> (the common case) or a direct call on a delegate value. Public instance
/// methods only. Null-safe: a null target/callee yields null without evaluating the arguments. Overload
/// resolution prefers exact and assignable matches over numeric and other conversions, and supports
/// optional parameters and a trailing <c>params</c> array.
/// </summary>
internal sealed record CallExpression(IEvaluable Callee, IReadOnlyList<IEvaluable> Arguments) : IEvaluable
{
    private static readonly ConditionalWeakTable<Type, ConcurrentDictionary<string, MethodInfo[]>> MethodCache = new();

    public object? Evaluate(IEvaluationContext context)
    {
        if (Callee is MemberAccessExpression member)
        {
            var target = member.Target.Evaluate(context);
            if (target is null) return null;

            return InvokeMethod(target, member.Member, EvaluateArguments(context));
        }

        var callee = Callee.Evaluate(context);
        if (callee is null) return null;
        if (callee is Delegate @delegate)
            return InvokeDelegate(@delegate, EvaluateArguments(context));

        throw new EvaluationException("Expression is not callable.");
    }

    private object?[] EvaluateArguments(IEvaluationContext context)
        => Arguments.Select(argument => argument.Evaluate(context)).ToArray();

    private static object? InvokeMethod(object target, string methodName, object?[] arguments)
    {
        var type = target.GetType();
        var methods = MethodCache
            .GetValue(type, static _ => new ConcurrentDictionary<string, MethodInfo[]>())
            .GetOrAdd(
                methodName,
                name => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.Name == name && !method.ContainsGenericParameters)
                .ToArray());

        var candidates = methods
            .Select(method => TryBindArguments(method.GetParameters(), arguments, out var bound, out var score)
                ? new BoundMethod(method, bound, score)
                : null)
            .OfType<BoundMethod>()
            .OrderBy(candidate => candidate.Score)
            .ToArray();

        if (candidates.Length == 0)
            throw new EvaluationException(
                $"No matching overload of '{methodName}' on '{type.Name}' for the provided arguments.");

        if (candidates.Length > 1 && candidates[0].Score == candidates[1].Score)
            throw new EvaluationException(
                $"Call to '{methodName}' on '{type.Name}' is ambiguous for the provided arguments.");

        try
        {
            return candidates[0].Method.Invoke(target, candidates[0].Arguments);
        }
        catch (TargetInvocationException exception)
        {
            var inner = exception.InnerException ?? exception;
            throw new EvaluationException($"Error calling '{methodName}': {inner.Message}", inner);
        }
        catch (Exception exception) when (
            exception is ArgumentException or MethodAccessException or InvalidOperationException)
        {
            throw new EvaluationException($"Unable to call '{methodName}' on '{type.Name}'.", exception);
        }
    }

    private static object? InvokeDelegate(Delegate @delegate, object?[] arguments)
    {
        if (!TryBindArguments(@delegate.Method.GetParameters(), arguments, out var bound, out _))
            throw new EvaluationException("Delegate arguments do not match its parameter types.");

        try
        {
            return @delegate.DynamicInvoke(bound);
        }
        catch (TargetInvocationException exception)
        {
            var inner = exception.InnerException ?? exception;
            throw new EvaluationException($"Error calling delegate: {inner.Message}", inner);
        }
        catch (ArgumentException exception)
        {
            throw new EvaluationException("Unable to invoke delegate.", exception);
        }
    }

    private static bool TryBindArguments(
        ParameterInfo[] parameters,
        object?[] arguments,
        out object?[] bound,
        out int score)
    {
        bound = [];
        score = 0;

        if (parameters.Any(parameter => parameter.ParameterType.IsByRef))
            return false;

        var hasParams = parameters.Length > 0 &&
            parameters[^1].IsDefined(typeof(ParamArrayAttribute), inherit: false);
        var fixedCount = hasParams ? parameters.Length - 1 : parameters.Length;
        var requiredCount = parameters
            .Take(fixedCount)
            .Count(parameter => !parameter.HasDefaultValue);

        if (arguments.Length < requiredCount || (!hasParams && arguments.Length > parameters.Length))
            return false;

        bound = new object?[parameters.Length];
        for (var index = 0; index < fixedCount; index++)
        {
            if (index < arguments.Length)
            {
                if (!TryCoerce(arguments[index], parameters[index].ParameterType, out bound[index], out var cost))
                    return false;
                score += cost;
            }
            else
            {
                bound[index] = parameters[index].DefaultValue;
                score += 10;
            }
        }

        if (!hasParams)
            return true;

        var paramsType = parameters[^1].ParameterType;
        if (arguments.Length == parameters.Length &&
            TryCoerce(arguments[^1], paramsType, out var directArray, out var directCost))
        {
            bound[^1] = directArray;
            score += directCost;
            return true;
        }

        var elementType = paramsType.GetElementType()!;
        var paramsCount = Math.Max(0, arguments.Length - fixedCount);
        var paramsArray = Array.CreateInstance(elementType, paramsCount);
        for (var index = 0; index < paramsCount; index++)
        {
            if (!TryCoerce(arguments[fixedCount + index], elementType, out var value, out var cost))
                return false;
            paramsArray.SetValue(value, index);
            score += cost;
        }

        bound[^1] = paramsArray;
        score += 20;
        return true;
    }

    private static bool TryCoerce(object? value, Type targetType, out object? converted, out int score)
    {
        if (value is null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
            {
                converted = null;
                score = 0;
                return false;
            }

            converted = null;
            score = 1;
            return true;
        }

        var sourceType = value.GetType();
        if (sourceType == targetType)
        {
            converted = value;
            score = 0;
            return true;
        }

        if (targetType.IsInstanceOfType(value))
        {
            converted = value;
            score = 1;
            return true;
        }

        try
        {
            converted = ValueCoercion.Convert(value, targetType);
            score = ConversionScore(sourceType, targetType);
            return true;
        }
        catch (EvaluationException)
        {
            converted = null;
            score = 0;
            return false;
        }
    }

    private static int ConversionScore(Type sourceType, Type targetType)
    {
        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (IsIntegral(sourceType) && IsIntegral(target))
            return 2;
        if (IsNumeric(sourceType) && IsNumeric(target))
            return 3;
        if (target.IsEnum)
            return 3;
        if (target == typeof(string))
            return 5;
        return 4;
    }

    private static bool IsIntegral(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return Type.GetTypeCode(underlying) is
            TypeCode.SByte or TypeCode.Byte or
            TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or
            TypeCode.Int64 or TypeCode.UInt64;
    }

    private static bool IsNumeric(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return IsIntegral(underlying) ||
            Type.GetTypeCode(underlying) is TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
    }

    private sealed record BoundMethod(MethodInfo Method, object?[] Arguments, int Score);
}
