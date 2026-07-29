using Xunit;

namespace Pidamg.Expressions.Tests;

/// <summary>Phase 3b: the <c>${...}</c> template envelope and the native-vs-string rule.</summary>
public sealed class InterpolatorTests
{
    private static IEvaluationContext Ctx(params (string Name, object? Value)[] vars)
    {
        var c = new EvaluationContext();
        foreach (var (n, v) in vars)
            c.Set(n, v);
        return c;
    }

    [Fact]
    public void Plain_text_without_placeholder_is_returned_verbatim()
        => Assert.Equal("vsphere:cluster", Interpolator.Evaluate("vsphere:cluster", Ctx()));

    [Fact]
    public void A_lone_placeholder_yields_the_native_typed_value()
    {
        var value = Interpolator.Evaluate("${x}", Ctx(("x", 42)));
        Assert.Equal(42, value);
        Assert.IsType<int>(value);          // native int, not "42"
    }

    [Fact]
    public void A_lone_placeholder_preserves_bool()
    {
        var value = Interpolator.Evaluate("${b}", Ctx(("b", true)));
        Assert.Equal(true, value);
        Assert.IsType<bool>(value);
    }

    [Fact]
    public void Text_around_a_placeholder_yields_a_string()
        => Assert.Equal("n=5", Interpolator.Evaluate("n=${x}", Ctx(("x", 5))));

    [Fact]
    public void Interpolates_into_a_path()
        => Assert.Equal("/PROD/DC1/vc1",
            Interpolator.Evaluate("/PROD/DC1/${p.VCenter}",
                Ctx(("p", new Dictionary<string, object?> { ["VCenter"] = "vc1" }))));

    [Fact]
    public void Multiple_placeholders_concatenate_as_string()
        => Assert.Equal("1/2", Interpolator.Evaluate("${a}/${b}", Ctx(("a", 1), ("b", 2))));

    [Fact]
    public void Empty_template_is_empty_string()
        => Assert.Equal("", Interpolator.Evaluate("", Ctx()));

    [Fact]
    public void Brace_inside_a_quoted_string_does_not_close_the_placeholder()
        => Assert.Equal("}", Interpolator.Evaluate("${ \"}\" }", Ctx()));

    [Fact]
    public void Unterminated_placeholder_throws()
        => Assert.Throws<EvaluationException>(() => Interpolator.Parse("${x"));

    // ----- typed template resolution (the stored "1" vs "${...}" case) -----

    [Fact]
    public void Typed_resolves_a_literal_to_the_target_type()
        => Assert.Equal(1, Interpolator.Parse<int>("1").Evaluate(Ctx()));   // stored "1" → int 1

    [Fact]
    public void Typed_resolves_an_expression_to_the_target_type()
        => Assert.Equal(7, Interpolator.Parse<int>("${a + b}").Evaluate(Ctx(("a", 3), ("b", 4))));

    [Fact]
    public void Typed_resolves_a_literal_false_to_bool()
        => Assert.False(Interpolator.Parse<bool>("false").Evaluate(Ctx()));   // stored "false" → bool false (parse)
}
