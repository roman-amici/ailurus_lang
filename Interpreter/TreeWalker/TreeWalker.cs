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

        public List<TreeWalkerEnvironment> Environments { get; set; } = new List<TreeWalkerEnvironment>();

        public TreeWalker(Evaluator evaluator)
        {
            Evaluator = evaluator;
            Environments.Add(new TreeWalkerEnvironment());
        }

        #region Statements

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
            }
        }

        void EvalLetStatement(LetStatement stmt)
        {
            AilurusValue initializer = null;
            if (stmt.Initializer != null)
            {
                initializer = EvalExpression(stmt.Initializer);
            }

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

        #endregion
    }
}