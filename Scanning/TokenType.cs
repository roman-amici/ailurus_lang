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
        Amp, // &
        AmpAmp, // &&
        Bar, // |
        BarBar, // ||
        Carrot, // ^
        CarrotCarrot, // ^^
        Slash, // /

        //Literals
        Identifier, // Alphanumeric, not starting with a number. Includes _
        StringConstant,
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
        Ptr,

        // Modules
        Module,
        Export,
        Import,
        From,

        // Misc
        SizeOf,
        PtrTo,

        // Reserved Types
        Byte,
        UByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Void,
        Str,
        Bool,
    }
}

