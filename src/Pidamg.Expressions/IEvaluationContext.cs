namespace Pidamg.Expressions;

/// <summary>
/// The variable-resolution environment an expression evaluates against — a tree of scopes:
/// lookup walks from the current scope up the parent chain, and a child scope can shadow the parent.
/// </summary>
public interface IEvaluationContext
{
    /// <summary>Resolve <paramref name="name"/>, walking up the parent chain. False if undefined anywhere.</summary>
    bool TryGetValue(string name, out object? value);

    /// <summary>Add a value to the current scope. Throws if the name is already defined in it.</summary>
    void Add(string name, object? value);

    /// <summary>Set a value unconditionally in the current scope (shadows the parent).</summary>
    void Set(string name, object? value);

    /// <summary>Create a child scope that inherits this scope's values (and may seed its own).</summary>
    IEvaluationContext CreateChild(IReadOnlyDictionary<string, object?>? values = null);
}
