# Pidamg.Expressions — Expression engine for .NET

**English** | [Français](README.fr.md)

An embeddable, host-neutral .NET expression engine. Parse an expression once, then evaluate it
against any number of scoped contexts.

> [!NOTE]
> This project targets .NET 8 and supports PowerShell 7.4 or later. While the package remains
> below `1.0.0`, its public API may evolve between minor versions in accordance with Semantic
> Versioning.

## Features

- Arithmetic, logical, comparison, null-coalescing, and ternary operators
- Typed and untyped evaluation
- Reusable parsed expression trees
- Parent/child variable scopes with shadowing
- Null-safe member access, method calls, and indexing
- Public property and field access through reflection
- Dictionary member and key lookup
- Optional and `params` method arguments
- `${expression}` string interpolation with native-value preservation
- Null-safe comparison and numeric promotion
- No runtime dependencies

## Installation

Stable packages are published on
[NuGet.org](https://www.nuget.org/packages/Pidamg.Expressions):

```bash
dotnet add package Pidamg.Expressions
```

Prerelease packages are also available from
[GitHub Packages](https://github.com/pidamg/expressions-dotnet/packages) and attached to
[GitHub Releases](https://github.com/pidamg/expressions-dotnet/releases). After
[configuring GitHub Packages authentication](https://docs.github.com/en/packages/working-with-the-nuget-registry),
install a prerelease with:

```bash
dotnet add package Pidamg.Expressions --prerelease
```

## Quick start

```csharp
using Pidamg.Expressions;

var expression = ExpressionParser.Parse<int>("quantity * unitPrice");

var context = new EvaluationContext();
context.Set("quantity", 3);
context.Set("unitPrice", 12);

Console.WriteLine(expression.Evaluate(context)); // 36
```

`ExpressionParser.Parse<T>()` returns an `IExpression<T>`. Use the non-generic `Parse()` when the
result type must remain dynamic:

```csharp
IEvaluable expression = ExpressionParser.Parse("enabled ? name : null");
object? result = expression.Evaluate(context);
```

## Supported expressions

| Category | Syntax |
|---|---|
| Literals | `null`, `true`, `false`, `42`, `3.14`, `"text"`, `'text'` |
| Arithmetic | `+`, `-`, `*`, `/` |
| Comparison | `==`, `!=`, `<`, `<=`, `>`, `>=` |
| Logic | `!`, `&&`, `||` |
| Conditional | `condition ? whenTrue : whenFalse` |
| Null coalescing | `value ?? fallback` |
| Member access | `customer.Address.City` |
| Indexing | `items[0]`, `values["key"]` |
| Calls | `service.Find(name)`, `callback(value)` |

Member access, indexing, and method calls return `null` when their target is `null`. Missing
dictionary keys and out-of-range list indexes also return `null`.

## Scoped contexts

```csharp
var root = new EvaluationContext();
root.Set("environment", "production");
root.Set("retries", 3);

var child = root.CreateChild(new Dictionary<string, object?>
{
    ["retries"] = 5,
});

var expression = ExpressionParser.Parse<string>("environment + \":\" + retries");
Console.WriteLine(expression.Evaluate(child)); // production:5
```

A child inherits values from its parent and can shadow them locally. `Add()` rejects a duplicate
name in the current scope; `Set()` creates or replaces a local value.

## String interpolation

```csharp
var context = new EvaluationContext(values: new Dictionary<string, object?>
{
    ["name"] = "api",
    ["port"] = 8080,
});

object? endpoint = Interpolator.Evaluate("https://${name}:${port}", context);
object? nativePort = Interpolator.Evaluate("${port}", context);
```

Mixed text always produces a string. A template containing only one placeholder preserves the
expression's native value, so `nativePort` above is an `int`.

Typed interpolation also converts literal text or an evaluated placeholder:

```csharp
var timeout = Interpolator.Parse<int>("30").Evaluate(context);
var port = Interpolator.Parse<int>("${port}").Evaluate(context);
```

## Value conversion

`ValueCoercion` exposes the same conversion and comparison rules used by the evaluator. Numeric
types are promoted for comparisons, integral comparisons preserve their precision, ordering with
`null` is false, and equality is null-safe. Integral arithmetic is checked for overflow. Numeric
values converted to strings use the invariant culture.

## Trust boundary

Pidamg.Expressions is an expression engine, not a security sandbox. An expression can read public
properties and fields and invoke public instance methods or delegates exposed through its
evaluation context. Those operations may have side effects.

Only evaluate trusted expressions when the context contains privileged objects. For untrusted
input, expose purpose-built immutable data objects and avoid providing service objects, file or
process handles, mutable collections, or delegates.

## Potential roadmap

The following capabilities are candidates for future releases and are not currently supported:

- `%` modulo operator
- `in` and `not in` membership operators for collections and dictionaries
- List and dictionary literals
- Large integer, decimal, and scientific-notation literals
- An escape sequence for a literal `${` in interpolated strings
- Source spans and more precise parser diagnostics
- A constrained evaluation mode with explicit member and method allowlists
- Configurable limits for expression depth, argument count, and evaluation complexity

Roadmap additions should preserve deterministic, culture-independent evaluation and avoid adding
runtime dependencies.

## Development

```bash
dotnet restore Pidamg.Expressions.slnx
dotnet build Pidamg.Expressions.slnx
dotnet test Pidamg.Expressions.slnx
dotnet format Pidamg.Expressions.slnx --verify-no-changes
```

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for contribution, public API compatibility, and release
guidelines.

## License

This project is available under the [MIT License](LICENSE).
