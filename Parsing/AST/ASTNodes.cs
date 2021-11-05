using System.Collections.Generic;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{
    public enum NodeType
    {
        Expression,
        Statement,
        Declaration
    }

    public abstract class ASTNode
    {
        public Token SourceStart { get; set; }
        public NodeType NodeType { get; set; }
    }
}