namespace Pidamg.Expressions;

internal readonly record struct Token(TokenKind Kind, string Text, int Position);
