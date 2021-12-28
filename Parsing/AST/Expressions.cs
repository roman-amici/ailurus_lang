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
        AddrOfExpression,
        ArrayLiteral,
        ArrayIndex,
        ArraySetExpression,
        New,
        VarCast,
        Tuple,
        TupleDestructure,
        VariantConstructor
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

    public class NewAlloc : ExpressionNode
    {
        public NewAlloc() : base(ExpressionType.New) { }
        public ExpressionNode Expr { get; set; }
        public bool CopyInPlace { get; set; }
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

    public interface ILValue
    {
        Token SourceStart { get; set; }
    }

    public class Variable : ExpressionNode, ILValue
    {
        public Variable() : base(ExpressionType.Variable) { }
        public QualifiedName Name { get; set; }

        // Resolver Properties
        public Resolution Resolution { get; set; }
    }

    public class Assign : ExpressionNode
    {
        public Assign() : base(ExpressionType.Assign) { }
        public QualifiedName Name { get; set; }
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
        public QualifiedName StructName { get; set; }
        public List<(Token, ExpressionNode)> Initializers { get; set; }
    }

    public class AddrOfExpression : ExpressionNode
    {
        public AddrOfExpression() : base(ExpressionType.AddrOfExpression) { }

        public ExpressionNode OperateOn { get; set; }
        public bool VarAddr { get; set; }
    }

    public class ArrayLiteral : ExpressionNode
    {
        public ArrayLiteral() : base(ExpressionType.ArrayLiteral) { }

        public List<ExpressionNode> Elements { get; set; }
        public ExpressionNode FillExpression { get; set; }
        public ExpressionNode FillLength { get; set; }
    }

    public class ArrayIndex : ExpressionNode
    {
        public ArrayIndex() : base(ExpressionType.ArrayIndex) { }

        public ExpressionNode CallSite { get; set; }
        public ExpressionNode IndexExpression { get; set; }
    }

    public class ArraySetExpression : ExpressionNode
    {
        public ArraySetExpression() : base(ExpressionType.ArraySetExpression) { }

        public ArrayIndex ArrayIndex { get; set; }
        public ExpressionNode Value { get; set; }
        public bool PointerAssign { get; set; }
    }

    public class VarCast : ExpressionNode
    {
        public VarCast() : base(ExpressionType.VarCast) { }

        // Should be array literal or string constant
        public ExpressionNode Expr { get; set; }
    }

    public class TupleExpression : ExpressionNode, ILValue
    {
        public TupleExpression() : base(ExpressionType.Tuple) { }

        public List<ExpressionNode> Elements { get; set; }
    }

    public class TupleDestructure : ExpressionNode
    {
        public TupleDestructure() : base(ExpressionType.TupleDestructure) { }

        public TupleExpression AssignmentTarget { get; set; }
        public ExpressionNode Value { get; set; }
    }

    public class VariantConstructor : ExpressionNode
    {
        public VariantConstructor() : base(ExpressionType.VariantConstructor) { }

        public QualifiedName VariantName { get; set; }
        public Token MemberName { get; set; }
        public List<ExpressionNode> Arguments { get; set; }

        public VariantMemberType MemberType { get; set; }
    }

}