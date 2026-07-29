using System.Text;

namespace Pidamg.Expressions;

/// <summary>
/// A string template mixing literal text and embedded expressions (e.g. <c>"/PROD/${param.VCenter}"</c>).
/// Every part is an <see cref="IEvaluable"/> — literal segments are <see cref="LiteralExpression"/>s — and
/// evaluation always yields a <see cref="string"/> (each part rendered via <c>ToString</c>, nulls as empty).
/// </summary>
internal sealed record InterpolatedStringExpression(IReadOnlyList<IEvaluable> Parts) : IEvaluable
{
    public object Evaluate(IEvaluationContext context)
    {
        var sb = new StringBuilder();
        foreach (var part in Parts)
            sb.Append(ValueCoercion.ToInvariantString(part.Evaluate(context)));
        return sb.ToString();
    }
}
