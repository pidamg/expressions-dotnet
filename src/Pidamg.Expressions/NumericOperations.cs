using System.Globalization;

namespace Pidamg.Expressions;

internal static class NumericOperations
{
    public static bool TryAreEqual(object? left, object? right, out bool result)
    {
        if (!TryGetCommonKind(left, right, out var kind))
        {
            result = false;
            return false;
        }

        try
        {
            result = kind switch
            {
                NumericKind.Int32 => System.Convert.ToInt32(left, CultureInfo.InvariantCulture) ==
                    System.Convert.ToInt32(right, CultureInfo.InvariantCulture),
                NumericKind.Int64 => System.Convert.ToInt64(left, CultureInfo.InvariantCulture) ==
                    System.Convert.ToInt64(right, CultureInfo.InvariantCulture),
                NumericKind.UInt64 => System.Convert.ToUInt64(left, CultureInfo.InvariantCulture) ==
                    System.Convert.ToUInt64(right, CultureInfo.InvariantCulture),
                NumericKind.Single => System.Convert.ToSingle(left, CultureInfo.InvariantCulture) ==
                    System.Convert.ToSingle(right, CultureInfo.InvariantCulture),
                NumericKind.Double => System.Convert.ToDouble(left, CultureInfo.InvariantCulture) ==
                    System.Convert.ToDouble(right, CultureInfo.InvariantCulture),
                NumericKind.Decimal => System.Convert.ToDecimal(left, CultureInfo.InvariantCulture) ==
                    System.Convert.ToDecimal(right, CultureInfo.InvariantCulture),
                _ => false,
            };
        }
        catch (ArithmeticException)
        {
            result = false;
        }

        return true;
    }

    public static bool TryCompare(object? left, object? right, out int result)
    {
        if (!TryGetCommonKind(left, right, out var kind))
        {
            result = 0;
            return false;
        }

        try
        {
            result = kind switch
            {
                NumericKind.Int32 => System.Convert.ToInt32(left, CultureInfo.InvariantCulture)
                    .CompareTo(System.Convert.ToInt32(right, CultureInfo.InvariantCulture)),
                NumericKind.Int64 => System.Convert.ToInt64(left, CultureInfo.InvariantCulture)
                    .CompareTo(System.Convert.ToInt64(right, CultureInfo.InvariantCulture)),
                NumericKind.UInt64 => System.Convert.ToUInt64(left, CultureInfo.InvariantCulture)
                    .CompareTo(System.Convert.ToUInt64(right, CultureInfo.InvariantCulture)),
                NumericKind.Single => System.Convert.ToSingle(left, CultureInfo.InvariantCulture)
                    .CompareTo(System.Convert.ToSingle(right, CultureInfo.InvariantCulture)),
                NumericKind.Double => System.Convert.ToDouble(left, CultureInfo.InvariantCulture)
                    .CompareTo(System.Convert.ToDouble(right, CultureInfo.InvariantCulture)),
                NumericKind.Decimal => System.Convert.ToDecimal(left, CultureInfo.InvariantCulture)
                    .CompareTo(System.Convert.ToDecimal(right, CultureInfo.InvariantCulture)),
                _ => 0,
            };
        }
        catch (ArithmeticException)
        {
            result = System.Convert.ToDouble(left, CultureInfo.InvariantCulture)
                .CompareTo(System.Convert.ToDouble(right, CultureInfo.InvariantCulture));
        }

        return true;
    }

    public static object ApplyBinary(object? left, object? right, string @operator)
    {
        if (!TryGetCommonKind(left, right, out var kind))
            throw new EvaluationException(
                $"Cannot apply arithmetic to '{left?.GetType().Name}' and '{right?.GetType().Name}'.");

        try
        {
            return kind switch
            {
                NumericKind.Int32 => Apply(
                    System.Convert.ToInt32(left, CultureInfo.InvariantCulture),
                    System.Convert.ToInt32(right, CultureInfo.InvariantCulture),
                    @operator),
                NumericKind.Int64 => Apply(
                    System.Convert.ToInt64(left, CultureInfo.InvariantCulture),
                    System.Convert.ToInt64(right, CultureInfo.InvariantCulture),
                    @operator),
                NumericKind.UInt64 => Apply(
                    System.Convert.ToUInt64(left, CultureInfo.InvariantCulture),
                    System.Convert.ToUInt64(right, CultureInfo.InvariantCulture),
                    @operator),
                NumericKind.Single => Apply(
                    System.Convert.ToSingle(left, CultureInfo.InvariantCulture),
                    System.Convert.ToSingle(right, CultureInfo.InvariantCulture),
                    @operator),
                NumericKind.Double => Apply(
                    System.Convert.ToDouble(left, CultureInfo.InvariantCulture),
                    System.Convert.ToDouble(right, CultureInfo.InvariantCulture),
                    @operator),
                NumericKind.Decimal => Apply(
                    System.Convert.ToDecimal(left, CultureInfo.InvariantCulture),
                    System.Convert.ToDecimal(right, CultureInfo.InvariantCulture),
                    @operator),
                _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
            };
        }
        catch (ArithmeticException exception)
        {
            throw new EvaluationException(
                $"Arithmetic operator '{@operator}' failed for '{left}' and '{right}'.",
                exception);
        }
    }

    public static object Negate(object? value)
    {
        try
        {
            return value switch
            {
                sbyte number => checked(-number),
                byte number => -number,
                short number => checked(-number),
                ushort number => -number,
                int number => checked(-number),
                uint number => checked(-(long)number),
                long number => checked(-number),
                ulong number => checked(-(decimal)number),
                float number => -number,
                double number => -number,
                decimal number => -number,
                _ => throw new EvaluationException($"Cannot negate '{value?.GetType().Name}'."),
            };
        }
        catch (OverflowException exception)
        {
            throw new EvaluationException($"Cannot negate '{value}'.", exception);
        }
    }

    private static object Apply(int left, int right, string @operator) => @operator switch
    {
        "+" => checked(left + right),
        "-" => checked(left - right),
        "*" => checked(left * right),
        "/" => left / right,
        _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
    };

    private static object Apply(long left, long right, string @operator) => @operator switch
    {
        "+" => checked(left + right),
        "-" => checked(left - right),
        "*" => checked(left * right),
        "/" => left / right,
        _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
    };

    private static object Apply(ulong left, ulong right, string @operator) => @operator switch
    {
        "+" => checked(left + right),
        "-" => checked(left - right),
        "*" => checked(left * right),
        "/" => left / right,
        _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
    };

    private static object Apply(float left, float right, string @operator) => @operator switch
    {
        "+" => left + right,
        "-" => left - right,
        "*" => left * right,
        "/" => left / right,
        _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
    };

    private static object Apply(double left, double right, string @operator) => @operator switch
    {
        "+" => left + right,
        "-" => left - right,
        "*" => left * right,
        "/" => left / right,
        _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
    };

    private static object Apply(decimal left, decimal right, string @operator) => @operator switch
    {
        "+" => left + right,
        "-" => left - right,
        "*" => left * right,
        "/" => left / right,
        _ => throw new EvaluationException($"Unknown arithmetic operator '{@operator}'."),
    };

    private static bool TryGetCommonKind(object? left, object? right, out NumericKind kind)
    {
        if (!TryGetKind(left, out var leftKind) || !TryGetKind(right, out var rightKind))
        {
            kind = default;
            return false;
        }

        if (leftKind == NumericKind.Decimal || rightKind == NumericKind.Decimal)
            kind = NumericKind.Decimal;
        else if (leftKind == NumericKind.Double || rightKind == NumericKind.Double)
            kind = NumericKind.Double;
        else if (leftKind == NumericKind.Single || rightKind == NumericKind.Single)
            kind = NumericKind.Single;
        else if (leftKind == NumericKind.UInt64 || rightKind == NumericKind.UInt64)
            kind = IsSigned(leftKind) || IsSigned(rightKind) ? NumericKind.Decimal : NumericKind.UInt64;
        else if (leftKind == NumericKind.Int64 || rightKind == NumericKind.Int64)
            kind = NumericKind.Int64;
        else
            kind = NumericKind.Int32;

        return true;
    }

    private static bool TryGetKind(object? value, out NumericKind kind)
    {
        kind = value switch
        {
            sbyte or short or int => NumericKind.Int32,
            byte or ushort => NumericKind.Int32,
            uint or long => NumericKind.Int64,
            ulong => NumericKind.UInt64,
            float => NumericKind.Single,
            double => NumericKind.Double,
            decimal => NumericKind.Decimal,
            _ => default,
        };
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static bool IsSigned(NumericKind kind)
        => kind is NumericKind.Int32 or NumericKind.Int64;

    private enum NumericKind
    {
        Int32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal,
    }
}
