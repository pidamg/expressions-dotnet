using Xunit;

namespace Pidamg.Expressions.Tests;

public sealed class InvocationTests
{
    [Fact]
    public void Null_target_does_not_evaluate_arguments()
    {
        var result = Eval("host.Child.Echo(missing)", ("host", new InvocationHost()));

        Assert.Null(result);
    }

    [Fact]
    public void Exact_overload_is_preferred_over_a_convertible_overload()
        => Assert.Equal("int", Eval("host.Select(1)", ("host", new InvocationHost())));

    [Fact]
    public void Numeric_overload_is_preferred_over_string_conversion()
        => Assert.Equal("long", Eval("host.Widen(1)", ("host", new InvocationHost())));

    [Fact]
    public void Equally_ranked_overloads_are_reported_as_ambiguous()
    {
        var exception = Assert.Throws<EvaluationException>(
            () => Eval("host.Ambiguous(1)", ("host", new InvocationHost())));

        Assert.Contains("ambiguous", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Null_cannot_bind_to_a_non_nullable_value_parameter()
    {
        var exception = Assert.Throws<EvaluationException>(
            () => Eval("host.IntValue(null)", ("host", new InvocationHost())));

        Assert.Contains("No matching overload", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Enum_arguments_use_expression_value_coercion()
        => Assert.Equal(Sample.Second,
            Eval("host.EnumValue(\"Second\")", ("host", new InvocationHost())));

    [Fact]
    public void Optional_parameter_before_params_can_be_omitted()
        => Assert.Equal("default:",
            Eval("host.OptionalAndParams()", ("host", new InvocationHost())));

    [Fact]
    public void Params_array_can_be_supplied_directly()
    {
        var context = new EvaluationContext(values: new Dictionary<string, object?>
        {
            ["host"] = new InvocationHost(),
            ["values"] = new[] { "one", "two" },
        });

        Assert.Equal("one,two", ExpressionParser.Parse("host.Join(values)").Evaluate(context));
    }

    [Fact]
    public void Method_exception_is_wrapped_as_an_evaluation_exception()
    {
        var exception = Assert.Throws<EvaluationException>(
            () => Eval("host.Throw()", ("host", new InvocationHost())));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Delegate_arguments_are_coerced()
        => Assert.Equal(1L, Eval("callback(1)", ("callback", (Func<long, long>)(value => value))));

    [Fact]
    public void Delegate_exception_is_wrapped_as_an_evaluation_exception()
    {
        var exception = Assert.Throws<EvaluationException>(
            () => Eval("callback()", ("callback", (Action)(() => throw new InvalidOperationException("failure")))));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Property_getter_exception_is_wrapped_as_an_evaluation_exception()
    {
        var exception = Assert.Throws<EvaluationException>(
            () => Eval("host.FailingProperty", ("host", new InvocationHost())));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    private static object? Eval(string expression, params (string Name, object? Value)[] values)
    {
        var context = new EvaluationContext();
        foreach (var (name, value) in values)
            context.Set(name, value);
        return ExpressionParser.Parse(expression).Evaluate(context);
    }

    private sealed class InvocationHost
    {
        public InvocationHost? Child => null;

        public string FailingProperty => throw new InvalidOperationException("failure");

        public string Echo(string value) => value;

        public string Select(string value) => "string";

        public string Select(int value) => "int";

        public string Widen(string value) => "string";

        public string Widen(long value) => "long";

        public string Ambiguous(long value) => "long";

        public string Ambiguous(ulong value) => "ulong";

        public int IntValue(int value) => value;

        public Sample EnumValue(Sample value) => value;

        public string OptionalAndParams(string prefix = "default", params string[] values)
            => $"{prefix}:{string.Join(',', values)}";

        public string Join(params string[] values) => string.Join(',', values);

        public void Throw() => throw new InvalidOperationException("failure");
    }

    public enum Sample
    {
        First,
        Second,
    }
}
