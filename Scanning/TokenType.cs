// To Add: ~,>>, <<, +=, *= etc

namespace AilurusLang.Scanning
{
    public enum TokenType
    {
        //Single char tokens
        LeftParen, // (
        RightParen, // )
        LeftBrace, // {
        RightBrace, // }
        RightBracket, // [
        LeftBracket, // ]
        Comma, // ,
        Dot, // .
        Plus, // +
        Semicolon, // ;
        Colon, // :
        QuestionMark, // ?
        Star, // *
        At, // @

        //One or two char tokens
        Bang, // !
        BangEqual, // !=
        Equal, // =
        EqualEqual, // ==
        Greater, // >
        GreaterEqual, // >=
        Less, // <
        LessEqual, // <=
        Minus, // -
        Arrow, // ->
        BackwardArrow, // <-
        Amp, // &
        AmpAmp, // &&
        Bar, // |
        BarBar, // ||
        Carrot, // ^
        CarrotCarrot, // ^^
        Slash, // /
        Array, // []

        //Literals
        Identifier, // Alphanumeric, not starting with a number. Includes _
        StringConstant,
        CharConstant,
        Number,

        // -- Keywords --

        // Operators
        Mod,

        // Control Structures
        If,
        Else,
        While,
        For,
        Do,
        Match,
        Case,
        Break,
        Continue,
        Return,
        Then,
        ForEach,
        In,

        // Values
        Null,
        True,
        False,

        // Declarations
        Struct, // struct
        Type, // type
        Fn, // fn
        Variant, // variant
        Let, // let

        // Modifiers
        Static, // 
        Volatile,
        Mut,
        VarPtr,
        Ptr,
        Var,

        // Modules
        Module,
        Export,
        Import,
        From,

        // Array Operators
        LenOf,

        // Memory Operators
        SizeOf,
        AddrOf,
        VarAddrOf,
        Free,
        New,

        DebugPrint,

        // Semantic Types
        EOF,
    }
}

