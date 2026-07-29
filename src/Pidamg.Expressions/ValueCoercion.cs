using System.Globalization;

namespace Pidamg.Expressions;

/// <summary>
/// The single source of value semantics shared by the typed <see cref="IEvaluable.Evaluate{T}"/> default
/// method and the AST nodes (comparisons, logical operators): truthiness, numeric promotion, null-safe
/// equality/ordering, and <c>object → T</c> conversion.
/// </summary>
public static class ValueCoercion
{
    /// <summary>Truthiness: <c>null</c>, <c>false</c>, <c>0</c> and the empty string are falsy; all else truthy.</summary>
    public static bool IsTruthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        int i => i != 0,
        long l => l != 0,
        double d => d != 0.0,
        float f => f != 0f,
        decimal d => d != 0m,
        string s => s.Length > 0,
        _ => true,
    };

    /// <summary>
    /// Null-safe equality: <c>null == null</c> is true, <c>x == null</c> is false; numeric primitive
    /// values compare using lossless integral promotion where possible (<c>1 == 1.0</c> is true);
    /// otherwise <c>Equals</c> is used, so incompatible types are simply not equal.
    /// </summary>
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null) return right is null;
        if (right is null) return false;
        if (NumericOperations.TryAreEqual(left, right, out var equal)) return equal;
        return left.Equals(right);
    }

    /// <summary>
    /// Null-safe ordering, returning <c>null</c> when the values are not orderable — either operand null,
    /// or incompatible types — so every ordering operator yields <c>false</c> for it ("null is not
    /// orderable"). Numeric primitive values compare across compatible numeric types, strings use
    /// ordinal ordering, and other same-type <see cref="IComparable"/> values compare via
    /// <c>CompareTo</c>.
    /// </summary>
    public static int? Compare(object? left, object? right)
    {
        if (left is null || right is null) return null;
        if (NumericOperations.TryCompare(left, right, out var comparison)) return comparison;
        if (left is string leftString && right is string rightString)
            return StringComparer.Ordinal.Compare(leftString, rightString);
        if (left.GetType() == right.GetType() && left is IComparable c) return c.CompareTo(right);
        return null;
    }

    /// <summary>Generic sugar over <see cref="Convert(object?, Type)"/>.</summary>
    public static T? Convert<T>(object? value) => (T?)Convert(value, typeof(T));

    /// <summary>
    /// Coerce a value to <paramref name="target"/>: pass-through if already assignable; <c>bool</c> by
    /// parsing 'true'/'false' (NOT truthiness — that is <see cref="IsTruthy"/>, a predicate concern);
    /// <c>string</c> via invariant formatting; enums from name or value; otherwise
    /// <see cref="System.Convert.ChangeType(object, Type, IFormatProvider)"/>. <c>null</c> maps to null for
    /// reference/Nullable targets, but a non-nullable value type cannot take null. Non-generic so reflection
    /// (command parameter binding, metadata binding) can use it with a runtime <see cref="Type"/>.
    /// </summary>
    public static object? Convert(object? value, Type target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (value is null)
        {
            if (!target.IsValueType || Nullable.GetUnderlyingType(target) is not null)
                return null;   // reference type or Nullable<T> → null
            throw new EvaluationException($"Cannot convert null to non-nullable '{target.Name}'.");
        }

        if (target.IsInstanceOfType(value)) return value;

        try
        {
            if (underlying == typeof(bool))
            {
                if (value is string boolText)
                    return bool.TryParse(boolText, out var parsed)
                        ? parsed
                        : throw new EvaluationException(
                            $"Cannot convert '{boolText}' to bool (expected 'true' or 'false').");
                return System.Convert.ChangeType(value, typeof(bool), CultureInfo.InvariantCulture);
            }
            if (underlying == typeof(string)) return ToInvariantString(value);
            if (underlying.IsEnum)
                return value is string name
                    ? Enum.Parse(underlying, name, ignoreCase: true)
                    : Enum.ToObject(underlying, value);
            return System.Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is not EvaluationException)
        {
            throw new EvaluationException(
                $"Cannot convert '{value}' ({value.GetType().Name}) to '{target.Name}'.", ex);
        }
    }

    internal static string ToInvariantString(object? value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
