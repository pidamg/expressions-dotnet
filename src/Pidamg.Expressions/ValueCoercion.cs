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
        string s => s.Length > 0,
        _ => true,
    };

    /// <summary>
    /// Null-safe equality: <c>null == null</c> is true, <c>x == null</c> is false; two numbers compare by
    /// value across <c>int</c>/<c>long</c>/<c>double</c> (<c>1 == 1.0</c> is true); otherwise <c>Equals</c>
    /// (so incompatible types are simply not equal).
    /// </summary>
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null) return right is null;
        if (right is null) return false;
        if (TryToDouble(left, out var dl) && TryToDouble(right, out var dr)) return dl == dr;
        return left.Equals(right);
    }

    /// <summary>
    /// Null-safe ordering, returning <c>null</c> when the values are not orderable — either operand null,
    /// or incompatible types — so every ordering operator yields <c>false</c> for it ("null is not
    /// orderable"). Two numbers compare across numeric types; same-type <see cref="IComparable"/> values
    /// compare via <c>CompareTo</c>.
    /// </summary>
    public static int? Compare(object? left, object? right)
    {
        if (left is null || right is null) return null;
        if (TryToDouble(left, out var dl) && TryToDouble(right, out var dr)) return dl.CompareTo(dr);
        if (left.GetType() == right.GetType() && left is IComparable c) return c.CompareTo(right);
        return null;
    }

    /// <summary>Generic sugar over <see cref="Convert(object?, Type)"/>.</summary>
    public static T? Convert<T>(object? value) => (T?)Convert(value, typeof(T));

    /// <summary>
    /// Coerce a value to <paramref name="target"/>: pass-through if already assignable; <c>bool</c> by
    /// parsing 'true'/'false' (NOT truthiness — that is <see cref="IsTruthy"/>, a predicate concern);
    /// <c>string</c> via <c>ToString</c>; enums from name or value; otherwise
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
            if (underlying == typeof(string)) return value.ToString() ?? string.Empty;
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

    // Numeric widening used by equality/ordering. bool and string are deliberately not numeric.
    private static bool TryToDouble(object? value, out double result)
    {
        switch (value)
        {
            case int i: result = i; return true;
            case long l: result = l; return true;
            case double d: result = d; return true;
            case float f: result = f; return true;
            default: result = 0; return false;
        }
    }
}
