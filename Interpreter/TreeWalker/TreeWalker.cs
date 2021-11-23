using System;
using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Interpreter.Runtime;
using AilurusLang.Interpreter.TreeWalker.Evaluators;
using AilurusLang.Parsing.AST;
using AilurusLang.Scanning;
using AilurusLang.StaticAnalysis.TypeChecking;

namespace AilurusLang.Interpreter.TreeWalker
{
    public class TreeWalker
    {
        public Evaluator Evaluator { get; set; }

        public TreeWalkerEnvironment ModuleEnvironment { get; set; } = new TreeWalkerEnvironment();

        public List<TreeWalkerEnvironment> BlockEnvironments { get; set; } = new List<TreeWalkerEnvironment>();

        public TreeWalker(Evaluator evaluator)
        {
            Evaluator = evaluator;
            // TODO: Setup module environment from the resolver
        }

        #region Environment Management

        void PushEnvironment()
        {
            BlockEnvironments.Add(new TreeWalkerEnvironment());
        }

        void PopEnvironment()
        {
            BlockEnvironments.RemoveAt(BlockEnvironments.Count - 1);
        }

        #endregion

        #region Statements

        public void EvalStatements(List<StatementNode> statements)
        {
            foreach (var statement in statements)
            {
                try
                {
                    EvalStatement(statement);
                }
                catch (RuntimeError _)
                {
                    return;
                }
            }
        }

        public void EvalStatement(StatementNode stmt)
        {
            switch (stmt)
            {
                case ExpressionStatement expressionStatement:
                    EvalExpression(expressionStatement.Expr);
                    break;
                case LetStatement letStatement:
                    EvalLetStatement(letStatement);
                    break;
                case PrintStatement printStatement:
                    EvalPrintStatement(printStatement);
                    break;
                case BlockStatement blockStatement:
                    EvalBlockStatement(blockStatement);
                    break;
                case IfStatement ifStatement:
                    EvalIfStatement(ifStatement);
                    break;
                case WhileStatement whileStatement:
                    EvalWhileStatement(whileStatement);
                    break;
                case ForStatement forStatement:
                    EvalForStatement(forStatement);
                    break;
                case BreakStatement breakStatement:
                    EvalBreakStatement(breakStatement);
                    break;
                case ContinueStatement continueStatement:
                    EvalContinueStatement(continueStatement);
                    break;
                default:
                    throw new NotImplementedException();

            }
        }

        void EvalContinueStatement(ContinueStatement _continueStatement)
        {
            throw new ControlFlowException(ControlFlowType.Continue);
        }

        void EvalBreakStatement(BreakStatement _breakStatement)
        {
            throw new ControlFlowException(ControlFlowType.Break);
        }

        void EvalForStatement(ForStatement forStatement)
        {
            EvalStatement(forStatement.Initializer);
            while (EvalExpression(forStatement.Predicate).GetAs<bool>())
            {
                try
                {
                    EvalStatement(forStatement.Statements);
                }
                catch (ControlFlowException e)
                {
                    if (e.ControlFlowType == ControlFlowType.Break)
                    {
                        break;
                    }
                    else if (e.ControlFlowType == ControlFlowType.Continue)
                    {
                        continue;
                    }
                    else
                    {
                        throw e;
                    }
                }
                EvalExpression(forStatement.Update);
            }
        }

        void EvalWhileStatement(WhileStatement whileStatement)
        {
            if (whileStatement.IsDoWhile)
            {
                while (EvalExpression(whileStatement.Predicate).GetAs<bool>())
                {
                    try
                    {
                        EvalStatement(whileStatement.Statements);
                    }
                    catch (ControlFlowException e)
                    {
                        if (e.ControlFlowType == ControlFlowType.Break)
                        {
                            break;
                        }
                        else if (e.ControlFlowType == ControlFlowType.Continue)
                        {
                            continue;
                        }
                        else
                        {
                            throw e;
                        }
                    }
                }
            }
            else
            {
                do
                {
                    try
                    {
                        EvalStatement(whileStatement.Statements);
                    }
                    catch (ControlFlowException e)
                    {
                        if (e.ControlFlowType == ControlFlowType.Break)
                        {
                            break;
                        }
                        else if (e.ControlFlowType == ControlFlowType.Continue)
                        {
                            continue;
                        }
                        else
                        {
                            throw e;
                        }
                    }
                } while (EvalExpression(whileStatement.Predicate).GetAs<bool>());
            }
        }



        void EvalIfStatement(IfStatement ifStatement)
        {
            var pred = EvalExpression(ifStatement.Predicate);
            if (pred.GetAs<bool>())
            {
                EvalBlockStatement(ifStatement.ThenStatements);
            }
            else if (ifStatement.ElseStatements != null)
            {
                EvalBlockStatement(ifStatement.ElseStatements);
            }
        }

        void EvalPrintStatement(PrintStatement print)
        {
            var value = EvalExpression(print.Expr);
            Console.WriteLine($"{value}");
        }

        void EvalLetStatement(LetStatement stmt)
        {
            AilurusValue initializer = null;
            if (stmt.Initializer != null)
            {
                initializer = EvalExpression(stmt.Initializer);
            }

            TreeWalkerEnvironment env;
            if (stmt.Resolution.ScopeDepth == null)
            {
                env = ModuleEnvironment;
            }
            else
            {
                env = BlockEnvironments[(int)stmt.Resolution.ScopeDepth];
            }
            env.SetValue(stmt.Resolution, initializer);

        }

        void EvalBlockStatement(BlockStatement block)
        {
            PushEnvironment();
            foreach (var statement in block.Statements)
            {
                EvalStatement(statement);
            }
            PopEnvironment();
        }

        #endregion

        #region Expressions

        public AilurusValue EvalExpression(ExpressionNode expr)
        {
            return expr.ExprType switch
            {
                ExpressionType.Literal => Evaluator.EvalLiteral((Literal)expr),
                ExpressionType.Grouping => EvalGrouping((Grouping)expr),
                ExpressionType.Unary => EvalUnary((Unary)expr),
                ExpressionType.Binary => EvalBinary((Binary)expr),
                ExpressionType.BinaryShortCircut => EvalBinaryShortCircut((BinaryShortCircut)expr),
                ExpressionType.IfExpression => EvalIfExpression((IfExpression)expr),
                ExpressionType.Variable => EvalVariableExpression((Variable)expr),
                ExpressionType.Assign => EvalAssignExpression((Assign)expr),
                _ => throw new NotImplementedException(),
            };
        }

        AilurusValue EvalGrouping(Grouping grouping)
        {
            return EvalExpression(grouping.Inner);
        }

        AilurusValue EvalUnary(Unary unary)
        {
            var value = EvalExpression(unary.Expr);
            return unary.Operator.Type switch
            {
                TokenType.Bang => Evaluator.EvalUnaryBang(value, unary),
                TokenType.Minus => Evaluator.EvalUnaryMinus(value, unary),
                _ => throw new NotImplementedException(),
            };
        }

        AilurusValue EvalBinary(Binary binary)
        {
            var left = EvalExpression(binary.Left);
            var right = EvalExpression(binary.Right);
            return binary.Operator.Type switch
            {
                TokenType.Plus => Evaluator.EvalPlus(left, right, binary),
                TokenType.Minus => Evaluator.EvalMinus(left, right, binary),
                TokenType.Amp => Evaluator.EvalBitwiseAnd(left, right, binary),
                TokenType.Bar => Evaluator.EvalBitwiseOr(left, right, binary),
                TokenType.Carrot => Evaluator.EvalBitwiseXOr(left, right, binary),
                TokenType.EqualEqual => Evaluator.EvalEquality(left, right, binary),
                TokenType.Greater => Evaluator.EvalGreater(left, right, binary),
                TokenType.GreaterEqual => Evaluator.EvalGreaterEqual(left, right, binary),
                TokenType.Less => Evaluator.EvalLess(left, right, binary),
                TokenType.LessEqual => Evaluator.EvalLessEqual(left, right, binary),
                _ => throw new NotFiniteNumberException(),
            };
        }

        AilurusValue EvalBinaryShortCircut(BinaryShortCircut binary)
        {
            AilurusValue left;
            AilurusValue right;
            switch (binary.Operator.Type)
            {
                case TokenType.AmpAmp:
                    left = EvalExpression(binary.Left);
                    if (left.AssertType(BooleanType.Instance))
                    {
                        right = EvalExpression(binary.Right);
                        if (right.AssertType(BooleanType.Instance))
                        {
                            Evaluator.EvalAnd(left, right, binary);
                        }
                        throw RuntimeError.BinaryOperatorError(left, right, binary.Operator);
                    }
                    throw RuntimeError.BinaryOperatorErrorFirstOp(left, binary.Operator);
                case TokenType.BarBar:
                    left = EvalExpression(binary.Left);
                    if (left.AssertType(BooleanType.Instance))
                    {
                        right = EvalExpression(binary.Right);
                        if (right.AssertType(BooleanType.Instance))
                        {
                            return Evaluator.EvalOr(left, right, binary);
                        }
                        throw RuntimeError.BinaryOperatorError(left, right, binary.Operator);
                    }
                    throw RuntimeError.BinaryOperatorErrorFirstOp(left, binary.Operator);
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusValue EvalIfExpression(IfExpression ifExpr)
        {
            var predicate = EvalExpression(ifExpr.Predicate);
            if (predicate.AssertType(BooleanType.Instance))
            {
                if (predicate.GetAs<bool>())
                {
                    return EvalExpression(ifExpr.TrueExpr);
                }
                else
                {
                    return EvalExpression(ifExpr.FalseExpr);
                }
            }
            else
            {
                throw new RuntimeError("Predicate in 'If' expression must be of type 'bool'", ifExpr.SourceStart);
            }
        }

        AilurusValue EvalVariableExpression(Variable varExpr)
        {

            if (varExpr.Resolution is VariableResolution v)
            {
                if (!v.IsInitialized)
                {
                    // TODO: Reject statically
                    throw new RuntimeError("Referenced an uninitialized variable", varExpr.Name);
                }
                if (v.ScopeDepth is null)
                {
                    return ModuleEnvironment.GetValue(v);
                }
                else
                {
                    return BlockEnvironments[(int)v.ScopeDepth].GetValue(v);
                }
            }
            else
            {
                // TODO: Handle functions
                throw new NotImplementedException();
            }
        }

        AilurusValue EvalAssignExpression(Assign assign)
        {
            var value = EvalExpression(assign.Assignment);
            if (assign.Resolution.ScopeDepth == null)
            {
                ModuleEnvironment.SetValue(assign.Resolution, value);
            }
            else
            {
                BlockEnvironments[(int)assign.Resolution.ScopeDepth]
                    .SetValue(assign.Resolution, value);
            }

            return value;
        }

        #endregion
    }
}