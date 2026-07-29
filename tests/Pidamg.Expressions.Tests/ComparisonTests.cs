using Xunit;

namespace Pidamg.Expressions.Tests;

/// <summary>The reworked null-safe comparison system (Phase 3a).</summary>
public sealed class ComparisonTests
{
    private static object? Eval(string expr, params (string Name, object? Value)[] vars)
    {
        var ctx = new EvaluationContext();
        foreach (var (n, v) in vars)
            ctx.Set(n, v);
        return ExpressionParser.Parse(expr).Evaluate(ctx);
    }

    // ----- equality is null-safe -----

    [Fact]
    public void Null_equals_null_is_true()
        => Assert.Equal(true, Eval("a == b", ("a", null), ("b", null)));

    [Fact]
    public void Value_equals_null_is_false()
        => Assert.Equal(false, Eval("a == b", ("a", 5), ("b", null)));

    [Fact]
    public void Not_equal_null_detects_presence()
        => Assert.Equal(true, Eval("a != b", ("a", 5), ("b", null)));

    // ----- ordering with null → false, never throws -----

    [Theory]
    [InlineData("a < b")]
    [InlineData("a <= b")]
    [InlineData("a > b")]
    [InlineData("a >= b")]
    public void Ordering_with_a_null_operand_is_false(string expr)
        => Assert.Equal(false, Eval(expr, ("a", null), ("b", 5)));

    [Fact]
    public void Null_less_or_equal_null_is_false_even_though_equal_is_true()
    {
        Assert.Equal(false, Eval("a <= b", ("a", null), ("b", null)));
        Assert.Equal(true, Eval("a == b", ("a", null), ("b", null)));
    }

    // ----- numeric coercion across int/double -----

    [Fact]
    public void Int_equals_double_of_same_value()
        => Assert.Equal(true, Eval("1 == 1.0"));

    [Fact]
    public void Int_orders_against_double()
        => Assert.Equal(true, Eval("1 < 2.0"));

    // ----- incompatible types compare as false, never throw -----

    [Fact]
    public void Incompatible_equality_is_false()
        => Assert.Equal(false, Eval("a == b", ("a", "x"), ("b", 3)));

    [Fact]
    public void Incompatible_ordering_is_false()
        => Assert.Equal(false, Eval("a < b", ("a", "x"), ("b", 3)));

    // ----- strings still order among themselves -----

    [Fact]
    public void Strings_order()
        => Assert.Equal(true, Eval("\"a\" < \"b\""));
}
