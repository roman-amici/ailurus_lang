namespace AilurusLang.Scanning
{
    public class Token
    {
        public TokenType Type { get; set; }
        public string Lexeme { get; set; }
        public long Line { get; set; }
        public long Column { get; set; }
        public string SourceFile { get; set; }
        public string? Identifier { get; set; }

        public static Token StaticIdentifier(string lexeme)
        {
            return new Token(TokenType.Identifier, lexeme, 0, 0, string.Empty, lexeme);
        }

        public Token(
            TokenType type,
            string lexeme,
            long line,
            long column,
            string sourceFile,
            string? identifier = null)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Column = column;
            SourceFile = sourceFile;
            Identifier = identifier;
        }

        public override string ToString()
        {
            return $"<Token {Type} {Lexeme} {Identifier ?? string.Empty}>";
        }
    }
}