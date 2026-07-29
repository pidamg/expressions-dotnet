namespace Pidamg.Expressions;

/// <summary>Raised when parsing or evaluating an expression fails.</summary>
public sealed class EvaluationException : Exception
{
    /// <summary>Initializes an exception with the specified message.</summary>
    public EvaluationException(string message) : base(message) { }

    /// <summary>Initializes an exception with the specified message and underlying exception.</summary>
    public EvaluationException(string message, Exception innerException) : base(message, innerException) { }
}
