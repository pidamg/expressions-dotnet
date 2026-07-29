namespace Pidamg.Expressions;

/// <summary>
/// Concrete scope: a dictionary plus an optional parent. Lookup checks this scope then walks the parent
/// chain; <see cref="Set"/> writes locally (shadowing the parent); <see cref="CreateChild"/> derives a
/// nested scope.
/// </summary>
public sealed class EvaluationContext : IEvaluationContext
{
    private readonly Dictionary<string, object?> _values;
    private readonly IEvaluationContext? _parent;

    /// <summary>
    /// Initializes a context with an optional parent and an optional initial set of local values.
    /// </summary>
    public EvaluationContext(IEvaluationContext? parent = null, IReadOnlyDictionary<string, object?>? values = null)
    {
        _parent = parent;
        _values = values is not null ? new Dictionary<string, object?>(values) : new Dictionary<string, object?>();
    }

    /// <inheritdoc />
    public bool TryGetValue(string name, out object? value)
        => _values.TryGetValue(name, out value) || (_parent?.TryGetValue(name, out value) ?? false);

    /// <inheritdoc />
    public void Add(string name, object? value)
    {
        if (!_values.TryAdd(name, value))
            throw new InvalidOperationException($"Variable '{name}' is already defined in the current scope.");
    }

    /// <inheritdoc />
    public void Set(string name, object? value) => _values[name] = value;

    /// <inheritdoc />
    public IEvaluationContext CreateChild(IReadOnlyDictionary<string, object?>? values = null)
        => new EvaluationContext(this, values);
}
