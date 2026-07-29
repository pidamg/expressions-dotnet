namespace Pidamg.Expressions;

internal enum TokenKind
{
    // Literals
    Integer,
    Float,
    String,
    True,
    False,
    Null,

    // Identifiers
    Identifier,

    // Arithmetic
    Plus,
    Minus,
    Star,
    Slash,

    // Comparison
    EqualEqual,
    BangEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // Logical
    AmpAmp,
    PipePipe,
    Bang,

    // Ternary / null-coalescing / member access
    Question,
    QuestionQuestion,
    Colon,
    Dot,

    // Punctuation
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    Comma,

    Eof,
}
