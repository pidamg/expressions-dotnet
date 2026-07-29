using Xunit;

namespace Pidamg.Expressions.Tests;

public sealed class ParserTests
{
    [Fact]
    public void Call_arguments_must_be_separated_by_a_comma()
    {
        var exception = Assert.Throws<EvaluationException>(() => ExpressionParser.Parse("f(1 2)"));

        Assert.Contains("Expected comma", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Call_allows_a_trailing_comma()
    {
        var context = new EvaluationContext(values: new Dictionary<string, object?>
        {
            ["f"] = (Func<int, int>)(value => value),
        });

        Assert.Equal(1, ExpressionParser.Parse("f(1,)").Evaluate(context));
    }

    [Fact]
    public void Parse_rejects_null()
        => Assert.Throws<ArgumentNullException>(() => ExpressionParser.Parse(null!));

    [Fact]
    public void As_typed_rejects_null()
        => Assert.Throws<ArgumentNullException>(() => ((IEvaluable)null!).AsTyped<int>());
}
