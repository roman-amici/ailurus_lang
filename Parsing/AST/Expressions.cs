using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{

    public enum ExpressionType
    {
        None,
        Literal,
        Grouping,
        Unary,
        Binary,
        BinaryShortCircut,
        IfExpression,
        Variable,
        Assign,
        Call,
        StructInitialization
    }

    public abstract class ExpressionNode : ASTNode
    {
        public readonly ExpressionType ExprType;

        public ExpressionNode(ExpressionType exprType = ExpressionType.None)
        {
            NodeType = NodeType.Expression;
            ExprType = exprType;
        }

        public AilurusDataType DataType { get; set; }
    }

    public class Literal : ExpressionNode
    {
        public Literal() : base(ExpressionType.Literal) { }
        public object Value { get; set; }
    }

    public class Grouping : ExpressionNode
    {
        public Grouping() : base(ExpressionType.Grouping) { }
        public ExpressionNode Inner { get; set; }
    }

    public class Unary : ExpressionNode
    {
        public Unary() : base(ExpressionType.Unary) { }
        public ExpressionNode Expr { get; set; }
        public Token Operator { get; set; } // TODO, should this be a token?
    }

    public class Binary : ExpressionNode
    {
        public Binary() : base(ExpressionType.Binary) { }
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }
        public Token Operator { get; set; }
    }

    public class BinaryShortCircut : ExpressionNode
    {
        public BinaryShortCircut() : base(ExpressionType.BinaryShortCircut) { }
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }
        public Token Operator { get; set; }
    }

    public class IfExpression : ExpressionNode
    {
        public IfExpression() : base(ExpressionType.IfExpression) { }
        public ExpressionNode Predicate { get; set; }
        public ExpressionNode TrueExpr { get; set; }
        public ExpressionNode FalseExpr { get; set; }
    }

    public class Variable : ExpressionNode
    {
        public Variable() : base(ExpressionType.Variable) { }
        public Token Name { get; set; }
    }

    public class Assign : ExpressionNode
    {
        public Assign() : base(ExpressionType.Assign) { }
        public Token Name { get; set; }
        public ExpressionNode Assignment { get; set; }
    }

    public class Call : ExpressionNode
    {
        public Call() : base(ExpressionType.Call) { }

        public ExpressionNode Callee { get; set; }
        public Token OpenParen { get; set; } // Used for error reporting
        public List<ExpressionNode> ArgumentList { get; set; }
    }

    public class StructInitialization : ExpressionNode
    {
        public StructInitialization() : base(ExpressionType.StructInitialization) { }
        public Token StructName { get; set; }
        public List<(Token, ExpressionNode)> Initializers { get; set; }
    }
}