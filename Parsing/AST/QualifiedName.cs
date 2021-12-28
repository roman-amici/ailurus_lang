using System.Collections.Generic;
using AilurusLang.Scanning;
using System.Linq;

namespace AilurusLang.Parsing.AST
{
    public class QualifiedName
    {
        public QualifiedName() { }
        public QualifiedName(Token name)
        {
            Name = new List<Token>() { name };
        }
        public QualifiedName(QualifiedName baseName, Token newName)
        {
            Name = new List<Token>(baseName.Name) { newName };
        }

        public List<Token> Name { get; set; }

        public Token SourceStart => Name.Count > 0 ? Name[0] : null;

        public Token ConcreteName => Name.Count > 0 ? Name[^1] : null;

        public Token VariantName => Name.Count > 1 ? Name[^2] : null;

        public Token VariantMemberName => ConcreteName;

        public override string ToString()
        {
            return string.Join("::", Name.Select(t => t.Identifier));
        }
    }
}