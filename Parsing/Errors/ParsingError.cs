using System;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.Errors
{
    public class ParsingError : Exception
    {
        public Token ErrorLocation { get; set; }

        public ParsingError(Token token, string message) : base(message)
        {
            ErrorLocation = token;
        }

        public override string ToString()
        {
            var e = ErrorLocation;
            return $"{e.SourceFile}:{e.Line}:{e.Column} - {Message}";
        }
    }
}