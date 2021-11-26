using System;
using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Interpreter.Runtime;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.TreeWalker.Evaluators
{
    public class DynamicValueEvaluator : Evaluator
    {
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

        bool IsNumeric(AilurusValue value)
        {
            return IsSignedInteger(value) || IsUnsignedInteger(value) || IsFloatingPoint(value);
        }

        public override AilurusValue EvalUnaryMinus(AilurusValue inner, Unary u)
        {
            if (IsSignedInteger(inner))
            {
                DynamicValue v = -inner.GetAs<int>();
                return v;
            }
            else if (IsFloatingPoint(inner))
            {
                DynamicValue v = -inner.GetAs<double>();
                return v;
            }
            else
            {
                throw RuntimeError.UnaryError(inner, u.Operator);
            }
        }

        public override AilurusValue EvalUnaryBang(AilurusValue inner, Unary u)
        {
            if (inner.AssertType(BooleanType.Instance))
            {
                DynamicValue v = !inner.GetAs<bool>();
                return v;
            }
            else
            {
                throw RuntimeError.UnaryError(inner, u.Operator);
            }
        }

        public override AilurusValue EvalAnd(AilurusValue left, AilurusValue right, BinaryShortCircut b)
        {
            DynamicValue val = left.GetAs<bool>() && right.GetAs<bool>();
            return val;
        }

        public override AilurusValue EvalOr(AilurusValue left, AilurusValue right, BinaryShortCircut b)
        {
            DynamicValue val = left.GetAs<bool>() || right.GetAs<bool>();
            return val;
        }

        public override AilurusValue EvalPlus(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() + right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() + right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() + right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() + right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() + right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() + right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() + right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() + right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() + right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalTimes(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() * right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() * right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() * right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() * right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() * right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() * right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() * right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() * right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() * right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalDivision(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() / right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() / right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() / right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() / right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() / right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() / right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() / right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() / right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() / right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalEquality(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsNumeric(left) && IsNumeric(right))
            {
                if (IsSignedInteger(left) && IsSignedInteger(right))
                {
                    result = left.GetAs<int>() == right.GetAs<int>();
                }
                else if (IsSignedInteger(left) && IsUnsignedInteger(right))
                {
                    result = left.GetAs<int>() == right.GetAs<uint>();
                }
                else if (IsUnsignedInteger(left) && IsSignedInteger(right))
                {
                    result = left.GetAs<uint>() == right.GetAs<int>();
                }
                else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
                {
                    result = left.GetAs<uint>() == right.GetAs<uint>();
                }
                else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
                {
                    result = left.GetAs<int>() == right.GetAs<double>();
                }
                else if (IsSignedInteger(left) && IsFloatingPoint(right))
                {
                    result = left.GetAs<uint>() == right.GetAs<double>();
                }
                else if (IsFloatingPoint(left) && IsSignedInteger(right))
                {
                    result = left.GetAs<double>() == right.GetAs<int>();
                }
                else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
                {
                    result = left.GetAs<double>() == right.GetAs<uint>();
                }
                else if (IsFloatingPoint(left) && IsFloatingPoint(right))
                {
                    result = left.GetAs<double>() == right.GetAs<double>();
                }
                else
                {
                    throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
                }
            }

            else if (left.AssertType(BooleanType.Instance) && right.AssertType(BooleanType.Instance))
            {
                result = left.GetAs<bool>() == right.GetAs<bool>();
            }
            else if (left is StructInstance leftStruct && right is StructInstance rightStruct)
            {
                if (leftStruct.StructType != rightStruct.StructType || leftStruct.Members.Count != rightStruct.Members.Count)
                {
                    throw new RuntimeError("Cannot compare structs of different types.", b.Operator);
                }

                foreach (var key in leftStruct.Members.Keys)
                {
                    if (!rightStruct.Members.ContainsKey(key))
                    {
                        result = false;
                        return result;
                    }
                    var equal = EvalEquality(leftStruct.Members[key], rightStruct.Members[key], b);
                    if (!equal.GetAs<bool>())
                    {
                        result = false;
                        return result;
                    }
                }

                result = false;
            }
            else
            {
                throw new NotImplementedException();
            }

            return result;
        }

        public override AilurusValue EvalGreater(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() > right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() > right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() > right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() > right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() > right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() > right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() > right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() > right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() > right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }
        public override AilurusValue EvalGreaterEqual(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() >= right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() >= right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() >= right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() >= right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() >= right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() >= right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() >= right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() >= right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() >= right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }
        public override AilurusValue EvalLess(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() < right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() < right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() < right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() < right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() < right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() < right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() < right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() < right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() < right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }
        public override AilurusValue EvalLessEqual(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() <= right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() <= right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() <= right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() <= right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() <= right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() <= right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() <= right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() <= right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() <= right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalMinus(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() - right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() - right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() - right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() - right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<int>() - right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<uint>() - right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() - right.GetAs<int>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() - right.GetAs<uint>();
            }
            else if (IsFloatingPoint(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<double>() - right.GetAs<double>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalBitwiseAnd(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() & right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() & right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() & right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() & right.GetAs<uint>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalBitwiseOr(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() | right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() | right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() | right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() | right.GetAs<uint>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalBitwiseXOr(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsSignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<int>() ^ right.GetAs<int>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<int>() ^ right.GetAs<uint>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = left.GetAs<uint>() ^ right.GetAs<int>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<uint>() ^ right.GetAs<uint>();
            }
            else
            {
                throw RuntimeError.BinaryOperatorError(left, right, b.Operator);
            }

            return result;
        }

        public override AilurusValue EvalLiteral(Literal literal)
        {
            return ValueFromDatatype(literal.Value, literal.DataType);
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
                        Members = (Dictionary<string, AilurusValue>)Value
                    };
                case AliasType alias:
                    return ValueFromDatatype(Value, alias.BaseType);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}