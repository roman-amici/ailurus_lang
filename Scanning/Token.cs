namespace AilurusLang.Scanning
{
    public class Token
    {
        public TokenType Type { get; set; }
        public string Lexeme { get; set; }
        public long Line { get; set; }
        public long Column { get; set; }
        public string? Identifier { get; set; }

        public Token(TokenType type, string lexeme, long line, long column, string? identifier = null)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Column = column;
            Identifier = identifier;
        }

        public override string ToString()
        {
            return $"<Token {Type} {Lexeme} {Identifier ?? string.Empty}";
        }
    }
}