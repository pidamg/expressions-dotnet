using Xunit;

namespace Pidamg.Expressions.Tests;

/// <summary>The typed surface: <c>Evaluate&lt;T&gt;</c> (DIM) and <c>Parse&lt;T&gt;</c> → <c>IExpression&lt;T&gt;</c>.</summary>
public sealed class TypedEvaluationTests
{
    private static readonly IEvaluationContext Empty = new EvaluationContext();

    [Fact]
    public void Evaluate_generic_coerces_int()
        => Assert.Equal(3, ExpressionParser.Parse("1 + 2").Evaluate<int>(Empty));

    [Fact]
    public void Evaluate_generic_bool_parses_not_truthiness()
    {
        Assert.True(ExpressionParser.Parse("true").Evaluate<bool>(Empty));         // bool keyword
        Assert.False(ExpressionParser.Parse("\"false\"").Evaluate<bool>(Empty));   // literal string → parsed to false
        Assert.True(ExpressionParser.Parse("5").Evaluate<bool>(Empty));            // non-zero number → true
        Assert.False(ExpressionParser.Parse("0").Evaluate<bool>(Empty));
        // A non-bool string is a conversion error (truthiness is a separate, predicate-only concern).
        Assert.Throws<EvaluationException>(() => ExpressionParser.Parse("\"\"").Evaluate<bool>(Empty));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("x", true)]
    [InlineData(0, false)]
    [InlineData(5, true)]
    [InlineData(true, true)]
    public void IsTruthy_is_the_predicate_primitive(object? value, bool expected)
        => Assert.Equal(expected, ValueCoercion.IsTruthy(value));

    [Fact]
    public void Evaluate_generic_string_uses_to_string()
        => Assert.Equal("42", ExpressionParser.Parse("42").Evaluate<string>(Empty));

    [Fact]
    public void Evaluate_generic_promotes_int_to_double()
        => Assert.Equal(3.0, ExpressionParser.Parse("3").Evaluate<double>(Empty));

    [Fact]
    public void Parse_typed_returns_an_expression_of_T()
    {
        IExpression<int> expr = ExpressionParser.Parse<int>("2 * 21");
        Assert.Equal(42, expr.Evaluate(Empty));
    }

    [Fact]
    public void Typed_predicate_is_reusable()
    {
        IExpression<bool> predicate = ExpressionParser.Parse<bool>("n > 5");
        Assert.True(predicate.Evaluate(Ctx(("n", 8))));
        Assert.False(predicate.Evaluate(Ctx(("n", 2))));
    }

    [Fact]
    public void Convert_null_to_non_nullable_value_type_throws()
        => Assert.Throws<EvaluationException>(() => ValueCoercion.Convert<int>(null));

    [Fact]
    public void Convert_null_to_nullable_is_null()
        => Assert.Null(ValueCoercion.Convert<int?>(null));

    [Fact]
    public void Convert_incompatible_throws()
        => Assert.Throws<EvaluationException>(() => ExpressionParser.Parse("\"abc\"").Evaluate<int>(Empty));

    private static IEvaluationContext Ctx(params (string Name, object? Value)[] vars)
    {
        var c = new EvaluationContext();
        foreach (var (n, v) in vars)
            c.Set(n, v);
        return c;
    }
}
