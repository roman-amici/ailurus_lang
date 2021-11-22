using AilurusLang.DataType;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{
    public enum StatementType
    {
        None,
        Expression,
        Let,
        Block,
        If,
        While,
        For,
        Break,
        Continue,
        Return,
        // Temp
        Print
    }

    public abstract class StatementNode : ASTNode
    {
        public readonly StatementType StmtType;

        public StatementNode(StatementType type = StatementType.None)
        {
            NodeType = NodeType.Statement;
            StmtType = type;
        }
    }

    public class LetStatement : StatementNode
    {
        public LetStatement() : base(StatementType.Let) { }

        public bool IsMutable { get; set; }
        public Token Name { get; set; }
        public ExpressionNode Initializer { get; set; }
        public TypeName AssertedType { get; set; }
        public VariableDeclaration Declaration { get; set; }
    }

    public class ExpressionStatement : StatementNode
    {
        public ExpressionStatement() : base(StatementType.Expression) { }

        public ExpressionNode Expr { get; set; }

    }

    public class PrintStatement : StatementNode
    {
        public PrintStatement() : base(StatementType.Print) { }
        public ExpressionNode Expr { get; set; }
    }

}