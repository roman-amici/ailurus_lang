using System.Collections.Generic;
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
        public VariableResolution Resolution { get; set; }
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

    public class BlockStatement : StatementNode
    {
        public BlockStatement() : base(StatementType.Block) { }
        public List<StatementNode> Statements { get; set; }
    }

    public class ForStatement : StatementNode
    {
        public ForStatement() : base(StatementType.For) { }
        public StatementNode Initializer { get; set; }
        public ExpressionNode Predicate { get; set; }
        public ExpressionNode Update { get; set; }
        public BlockStatement Statements { get; set; }
    }

    public class WhileStatement : StatementNode
    {
        public WhileStatement() : base(StatementType.While) { }
        public ExpressionNode Predicate { get; set; }
        public BlockStatement Statements { get; set; }
        public bool IsDoWhile { get; set; }
    }

    public class IfStatement : StatementNode
    {
        public IfStatement() : base(StatementType.If) { }
        public ExpressionNode Predicate { get; set; }
        public BlockStatement ThenStatements { get; set; }
        public BlockStatement? ElseStatements { get; set; }
    }

    public abstract class ControlStatement : StatementNode
    {
        public ControlStatement(StatementType type) : base(type) { }
    }

    public class BreakStatement : ControlStatement
    {
        public BreakStatement() : base(StatementType.Break) { }
    }

    public class ContinueStatement : ControlStatement
    {
        public ContinueStatement() : base(StatementType.Continue) { }
    }

    public class ReturnStatement : ControlStatement
    {
        public ReturnStatement() : base(StatementType.Return) { }
        public ExpressionNode ReturnValue { get; set; }
    }

}