using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{

    public enum ExpressionType
    {
        None,
        Literal,
        Unary,
        Binary,
        BinaryShortCircut,
        IfExpression,
        Variable,
        Assign,
        Call,
        Get,
        Set,
        StructInitialization,
        AddrOfExpression
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


    public class Unary : ExpressionNode
    {
        public Unary() : base(ExpressionType.Unary) { }
        public ExpressionNode Expr { get; set; }
        public Token Operator { get; set; } // TODO, should this be a token?
    }

    public abstract class BinaryLike : ExpressionNode
    {
        public BinaryLike(ExpressionType type) : base(type) { }

        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }
        public Token Operator { get; set; }
    }

    public class Binary : BinaryLike
    {
        public Binary() : base(ExpressionType.Binary) { }
    }

    public class BinaryShortCircut : BinaryLike
    {
        public BinaryShortCircut() : base(ExpressionType.BinaryShortCircut) { }
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
        public Variable(ExpressionType type) : base(type) { }
        public Token Name { get; set; }
        public Resolution Resolution { get; set; }
    }

    public class Assign : ExpressionNode
    {
        public Assign() : base(ExpressionType.Assign) { }
        public Token Name { get; set; }
        public VariableResolution Resolution { get; set; }
        public ExpressionNode Assignment { get; set; }

        public bool PointerAssign { get; set; }
    }

    public class Call : ExpressionNode
    {
        public Call() : base(ExpressionType.Call) { }

        public Token RightParen { get; set; }
        public ExpressionNode Callee { get; set; }
        public List<ExpressionNode> ArgumentList { get; set; }
    }

    public interface IFieldAccessor
    {
        Token FieldName { get; set; }
        ExpressionNode CallSite { get; set; }
        Token SourceStart { get; set; }
    }

    public class Get : ExpressionNode, IFieldAccessor
    {
        public Get() : base(ExpressionType.Get) { }
        public ExpressionNode CallSite { get; set; }
        public Token FieldName { get; set; }
    }

    public class SetExpression : ExpressionNode, IFieldAccessor
    {
        public SetExpression() : base(ExpressionType.Set) { }
        public Token FieldName { get; set; }
        public ExpressionNode CallSite { get; set; }
        public ExpressionNode Value { get; set; }
        public bool PointerAssign { get; set; }
    }

    public class StructInitialization : ExpressionNode
    {
        public StructInitialization() : base(ExpressionType.StructInitialization) { }
        public Token StructName { get; set; }
        public List<(Token, ExpressionNode)> Initializers { get; set; }
    }

    public class AddrOfExpression : ExpressionNode
    {
        public AddrOfExpression() : base(ExpressionType.AddrOfExpression) { }

        public ExpressionNode OperateOn { get; set; }
        public bool VarAddr { get; set; }
    }
}