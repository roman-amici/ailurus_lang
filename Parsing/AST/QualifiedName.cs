using System.Collections.Generic;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{
    public class QualifiedName
    {
        public QualifiedName() { }
        public QualifiedName(Token name)
        {
            Name = new List<Token>() { name };
        }

        public List<Token> Name { get; set; }

        public Token SourceStart => Name[0];

        public Token ConcreteName => Name[^1];

        public override string ToString()
        {
            return string.Join("::", Name);
        }
    }
}