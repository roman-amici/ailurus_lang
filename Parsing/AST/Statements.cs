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
        Free,
        ForEach,
        MatchStatement,
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
        public ILValue AssignmentTarget { get; set; }
        public ExpressionNode Initializer { get; set; }
        public TypeName AssertedType { get; set; }
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
        public IfStatement? ElseStatements { get; set; }
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

    public class FreeStatement : StatementNode
    {
        public FreeStatement() : base(StatementType.Free) { }
        public ExpressionNode Expr { get; set; }
    }

    public class ForEachStatement : StatementNode
    {
        public ForEachStatement() : base(StatementType.ForEach) { }

        public ILValue AssignmentTarget { get; set; }
        public TypeName? AssertedTypeName { get; set; }
        public ExpressionNode IteratedValue { get; set; }
        public BlockStatement Body { get; set; }
        public bool IsMutable { get; set; }

        // Resolved Members
        public bool IterateOverReference { get; set; }
    }

    public class MatchStatement : StatementNode
    {
        public MatchStatement() : base(StatementType.MatchStatement) { }

        public ExpressionNode ToMatch { get; set; }
        public List<(IMatchable, StatementNode)> Patterns { get; set; }
        public StatementNode? DefaultPattern { get; set; }

        public bool PatternsAreLiterals { get; set; }
    }

}