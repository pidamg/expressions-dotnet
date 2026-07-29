using System.Globalization;
using Xunit;

namespace Pidamg.Expressions.Tests;

public sealed class NumericTests
{
    [Fact]
    public void Long_arithmetic_preserves_the_value_and_type()
    {
        var result = Eval("value + 1", ("value", 3_000_000_000L));

        Assert.Equal(3_000_000_001L, result);
        Assert.IsType<long>(result);
    }

    [Fact]
    public void Distinct_large_longs_are_not_equal()
        => Assert.Equal(false, Eval("left == right",
            ("left", 9_007_199_254_740_992L),
            ("right", 9_007_199_254_740_993L)));

    [Fact]
    public void Large_longs_retain_exact_ordering()
        => Assert.Equal(true, Eval("left < right",
            ("left", 9_007_199_254_740_992L),
            ("right", 9_007_199_254_740_993L)));

    [Fact]
    public void Float_arithmetic_preserves_the_float_type()
    {
        var result = Eval("value + 1", ("value", 1.5f));

        Assert.Equal(2.5f, result);
        Assert.IsType<float>(result);
    }

    [Fact]
    public void Decimal_arithmetic_preserves_the_decimal_type()
    {
        var result = Eval("value * 2", ("value", 1.25m));

        Assert.Equal(2.5m, result);
        Assert.IsType<decimal>(result);
    }

    [Fact]
    public void Decimal_zero_is_falsy()
        => Assert.False(ValueCoercion.IsTruthy(0m));

    [Fact]
    public void Integer_overflow_is_an_evaluation_error()
    {
        var exception = Assert.Throws<EvaluationException>(
            () => Eval("value + 1", ("value", int.MaxValue)));

        Assert.IsType<OverflowException>(exception.InnerException);
    }

    [Fact]
    public void Integer_division_by_zero_is_an_evaluation_error()
    {
        var exception = Assert.Throws<EvaluationException>(() => Eval("1 / 0"));

        Assert.IsType<DivideByZeroException>(exception.InnerException);
    }

    [Fact]
    public void Unary_negation_supports_float_and_checks_integral_overflow()
    {
        Assert.Equal(-1.5f, Eval("-value", ("value", 1.5f)));

        var exception = Assert.Throws<EvaluationException>(
            () => Eval("-value", ("value", long.MinValue)));
        Assert.IsType<OverflowException>(exception.InnerException);
    }

    [Fact]
    public void String_conversion_is_invariant()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var context = Context(("value", 1.5m));

            Assert.Equal("1.5", ValueCoercion.Convert<string>(1.5m));
            Assert.Equal("value=1.5", ExpressionParser.Parse("\"value=\" + value").Evaluate(context));
            Assert.Equal("value=1.5", Interpolator.Evaluate("value=${value}", context));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void String_ordering_is_ordinal()
        => Assert.Equal(false, Eval("\"ä\" < \"z\""));

    private static object? Eval(string expression, params (string Name, object? Value)[] values)
        => ExpressionParser.Parse(expression).Evaluate(Context(values));

    private static IEvaluationContext Context(params (string Name, object? Value)[] values)
    {
        var context = new EvaluationContext();
        foreach (var (name, value) in values)
            context.Set(name, value);
        return context;
    }
}
