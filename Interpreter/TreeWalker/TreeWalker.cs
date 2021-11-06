using System;
using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Interpreter.Runtime;
using AilurusLang.Parsing.AST;
using AilurusLang.Scanning;

namespace AilurusLang.Interpreter.TreeWalker
{
    public class TreeWalker
    {
        public AilurusValue EvalExpression(ExpressionNode expr)
        {
            return expr.ExprType switch
            {
                ExpressionType.Literal => EvalLiteral((Literal)expr),
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

        bool IsSignedInteger(AilurusValue value)
        {
            return value.AssertType(IntType.InstanceSigned) ||
                   value.AssertType(ShortType.InstanceSigned) ||
                   value.AssertType(ByteType.InstanceSigned);
        }

        bool IsUnsignedInteger(AilurusValue value)
        {
            return value.AssertType(IntType.InstanceUnsigned) ||
                   value.AssertType(ShortType.InstanceUnsigned) ||
                   value.AssertType(ByteType.InstanceUnsigned);
        }

        bool IsFloatingPoint(AilurusValue value)
        {
            return value.AssertType(FloatType.Instance) ||
                   value.AssertType(DoubleType.Instance);
        }

        AilurusValue EvalUnary(Unary unary)
        {
            var value = EvalExpression(unary.Expr);
            switch (unary.Operator.Type)
            {
                case TokenType.Bang:
                    if (value.AssertType(BooleanType.Instance))
                    {
                        DynamicValue v = !value.GetAs<bool>();
                        return v;
                    }
                    else
                    {
                        throw new RuntimeError(
                            $"Unable to apply operator '!' to value of type '{value.TypeName}'",
                            unary.Operator);
                    }
                case TokenType.Minus:
                    if (IsSignedInteger(value))
                    {
                        DynamicValue v = -value.GetAs<int>();
                        return v;
                    }
                    else if (IsFloatingPoint(value))
                    {
                        DynamicValue v = -value.GetAs<double>();
                        return v;
                    }
                    else
                    {
                        throw new RuntimeError(
                            $"Unable to apply operator '-' to value of type '{value.TypeName}",
                            unary.Operator);
                    }
                case TokenType.At:
                default:
                    throw new NotImplementedException();
            }
        }

        AilurusValue EvalBinary(Binary binary)
        {
            var left = EvalExpression(binary.Left);
            var right = EvalExpression(binary.Right);
            switch (binary.Operator.Type)
            {
                case TokenType.Plus:
                    return EvalPlus(left, right, binary);
                default:
                    throw new NotFiniteNumberException();
            }
        }

        AilurusValue EvalBinaryShortCircut(BinaryShortCircut binary)
        {
            throw new NotImplementedException();
        }

        AilurusValue EvalIfExpression(IfExpression ifExpr)
        {
            throw new NotImplementedException();
        }

        AilurusValue EvalPlus(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue sum;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                sum = left.GetAs<int>() + right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                sum = left.GetAs<int>() + right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                sum = left.GetAs<uint>() + right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                sum = left.GetAs<uint>() + right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                sum = left.GetAs<int>() + right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                sum = left.GetAs<uint>() + right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                sum = left.GetAs<double>() + right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                sum = left.GetAs<double>() + right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                sum = left.GetAs<double>() + right.GetAs<double>();
            }
            else
            {
                throw new RuntimeError($"Unable to apply operator '+' between types {left.GetType()} and {right.GetType()}", b.Operator);
            }

            return sum;
        }

        AilurusValue EvalLiteral(Literal literal)
        {
            return ValueFromDatatype(literal.Value, literal.ValueType);
        }

        AilurusValue ValueFromDatatype(object Value, AilurusDataType type)
        {
            switch (type)
            {
                case BooleanType _:
                case ByteType _:
                case ShortType _:
                case IntType _:
                case FloatType _:
                case DoubleType _:
                case StringType _:
                case NullType _:
                    return new DynamicValue() { Value = Value };
                case StructType structType:
                    return new StructInstance()
                    {
                        StructType = structType,
                        Members = (List<AilurusValue>)Value
                    };
                case AliasType alias:
                    return ValueFromDatatype(Value, alias.BaseType);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}