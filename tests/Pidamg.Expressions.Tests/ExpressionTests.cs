using Xunit;

namespace Pidamg.Expressions.Tests;

/// <summary>A typed "handler"-like object: reflection sees only its public surface (purity).</summary>
public sealed class Cluster
{
    public string Name { get; init; } = "";
    public int Count { get; init; }
    public string[] Tags { get; init; } = [];
    public Cluster? Parent { get; init; }

    public int Add(int a, int b = 10) => a + b;

    public bool ContainsTag(string category, params string[] values)
        => values.Any(v => Tags.Contains($"{category}={v}"));
}

public sealed class ExpressionTests
{
    private static object? Eval(string expr, IEvaluationContext? ctx = null)
        => ExpressionParser.Parse(expr).Evaluate(ctx ?? new EvaluationContext());

    private static IEvaluationContext Ctx(params (string Name, object? Value)[] vars)
    {
        var c = new EvaluationContext();
        foreach (var (n, v) in vars)
            c.Set(n, v);
        return c;
    }

    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("10 - 4", 6)]
    [InlineData("10 / 4", 2)]            // integer division
    [InlineData("-5", -5)]
    [InlineData("!false", true)]
    [InlineData("true && false", false)]
    [InlineData("true || false", true)]
    [InlineData("3 > 2", true)]
    [InlineData("2 >= 2", true)]
    [InlineData("1 < 2 && 2 < 3", true)]
    [InlineData("true ? 1 : 2", 1)]
    [InlineData("false ? 1 : 2", 2)]
    public void Evaluates_basic_expressions(string expr, object expected)
        => Assert.Equal(expected, Eval(expr));

    [Theory]
    [InlineData("1 + 2.0", 3.0)]        // numeric promotion
    [InlineData("3.0 * 2", 6.0)]
    public void Evaluates_float_arithmetic(string expr, double expected)
        => Assert.Equal(expected, Eval(expr));

    [Theory]
    [InlineData("\"a\" + \"b\"", "ab")]
    [InlineData("\"n=\" + 5", "n=5")]
    [InlineData("'hello'", "hello")]
    public void Evaluates_strings(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    [Fact]
    public void Resolves_identifiers_from_context()
        => Assert.Equal(42, Eval("x", Ctx(("x", 42))));

    [Fact]
    public void Undefined_root_variable_throws()
        => Assert.Throws<EvaluationException>(() => Eval("missing"));

    [Fact]
    public void Child_scope_shadows_parent_but_inherits()
    {
        var root = Ctx(("a", 1), ("b", 2));
        var child = root.CreateChild(new Dictionary<string, object?> { ["a"] = 99 });
        Assert.Equal(99, ExpressionParser.Parse("a").Evaluate(child));   // shadowed
        Assert.Equal(2, ExpressionParser.Parse("b").Evaluate(child));    // inherited
    }

    [Fact]
    public void Parse_once_evaluate_many()
    {
        var expr = ExpressionParser.Parse("x * 2");
        Assert.Equal(10, expr.Evaluate(Ctx(("x", 5))));
        Assert.Equal(14, expr.Evaluate(Ctx(("x", 7))));
    }

    // ----- member access -----

    [Fact]
    public void Member_access_reads_public_property()
        => Assert.Equal("prod", Eval("c.Name", Ctx(("c", new Cluster { Name = "prod" }))));

    [Fact]
    public void Member_access_chains()
        => Assert.Equal("root", Eval("c.Parent.Name",
            Ctx(("c", new Cluster { Parent = new Cluster { Name = "root" } }))));

    [Fact]
    public void Member_access_on_string_keyed_dictionary_looks_up_key()
    {
        var param = new Dictionary<string, object?> { ["VCenter"] = "vc1" };
        Assert.Equal("vc1", Eval("p.VCenter", Ctx(("p", param))));
    }

    [Fact]
    public void Member_access_on_dictionary_missing_key_is_null()
        => Assert.Null(Eval("p.Absent", Ctx(("p", new Dictionary<string, object?>()))));

    [Fact]
    public void Member_access_missing_on_typed_object_throws()
        => Assert.Throws<EvaluationException>(() => Eval("c.Nope", Ctx(("c", new Cluster()))));

    // ----- null-safe navigation -----

    [Fact]
    public void Member_access_on_null_is_null()
        => Assert.Null(Eval("c.Parent.Name", Ctx(("c", new Cluster()))));   // Parent is null → null, no throw

    [Fact]
    public void Index_on_null_is_null()
        => Assert.Null(Eval("c.Parent[0]", Ctx(("c", new Cluster()))));

    [Fact]
    public void Method_call_on_null_is_null()
        => Assert.Null(Eval("c.Parent.Add(1)", Ctx(("c", new Cluster()))));

    // ----- indexing -----

    [Fact]
    public void Indexes_a_list_by_int()
        => Assert.Equal("y", Eval("a[1]", Ctx(("a", new[] { "x", "y", "z" }))));

    [Fact]
    public void Indexes_a_dictionary_by_key()
    {
        var tags = new Dictionary<string, object?> { ["Category"] = "infra" };
        Assert.Equal("infra", Eval("t[\"Category\"]", Ctx(("t", tags))));
    }

    [Fact]
    public void Index_out_of_range_is_null()
        => Assert.Null(Eval("a[9]", Ctx(("a", new[] { "x" }))));

    // ----- null-coalescing -----

    [Fact]
    public void Coalesce_returns_left_when_non_null()
        => Assert.Equal(3, Eval("x ?? 5", Ctx(("x", 3))));

    [Fact]
    public void Coalesce_returns_right_when_null()
        => Assert.Equal(5, Eval("x ?? 5", Ctx(("x", null))));

    [Fact]
    public void Coalesce_is_right_associative_and_chains()
        => Assert.Equal("z", Eval("a ?? b ?? \"z\"", Ctx(("a", null), ("b", null))));

    [Fact]
    public void Coalesce_supplies_default_for_null_navigation()
        => Assert.Equal("none", Eval("c.Parent.Name ?? \"none\"", Ctx(("c", new Cluster()))));

    // ----- method calls -----

    [Fact]
    public void Calls_a_method_with_a_default_parameter()
        => Assert.Equal(15, Eval("c.Add(5)", Ctx(("c", new Cluster()))));

    [Fact]
    public void Calls_a_method_with_all_arguments()
        => Assert.Equal(6, Eval("c.Add(5, 1)", Ctx(("c", new Cluster()))));

    [Fact]
    public void Calls_a_variadic_params_method()
    {
        var c = new Cluster { Name = "c1", Tags = ["Category=value2"] };
        Assert.Equal(true, Eval("c.ContainsTag(\"Category\", \"value1\", \"value2\")", Ctx(("c", c))));
        Assert.Equal(false, Eval("c.ContainsTag(\"Category\", \"valueX\")", Ctx(("c", c))));
    }
}
