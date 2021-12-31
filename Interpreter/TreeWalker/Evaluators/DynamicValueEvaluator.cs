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
            return value.AssertType(Signed64Type.Instance) ||
                   value.AssertType(Signed32Type.Instance) ||
                   value.AssertType(Signed16Type.Instance) ||
                   value.AssertType(Signed8Type.Instance);
        }

        bool IsUnsignedInteger(AilurusValue value)
        {
            return value.AssertType(Unsigned64Type.Instance) ||
                   value.AssertType(Unsigned32Type.Instance) ||
                   value.AssertType(Unsigned16Type.Instance) ||
                   value.AssertType(Unsigned8Type.Instance);
        }

        bool IsFloatingPoint(AilurusValue value)
        {
            return value.AssertType(Float32Type.Instance) ||
                   value.AssertType(Float64Type.Instance);
        }

        bool IsNumeric(AilurusValue value)
        {
            return IsSignedInteger(value) || IsUnsignedInteger(value) || IsFloatingPoint(value);
        }

        public override AilurusValue EvalUnaryMinus(AilurusValue inner, Unary u)
        {
            if (IsSignedInteger(inner))
            {
                DynamicValue v = -inner.GetAs<long>();
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
                result = left.GetAs<long>() + right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() + (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() + right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() + right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() + right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() + right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() + right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() + right.GetAs<ulong>();
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
                result = left.GetAs<long>() * right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() * (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() * right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() * right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() * right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() * right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() * right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() * right.GetAs<ulong>();
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
                result = left.GetAs<long>() / right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() / (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() / right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() / right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() / right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() / right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() / right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() / right.GetAs<ulong>();
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
                    result = left.GetAs<long>() == right.GetAs<long>();
                }
                else if (IsSignedInteger(left) && IsUnsignedInteger(right))
                {
                    result = left.GetAs<long>() == (long)right.GetAs<ulong>();
                }
                else if (IsUnsignedInteger(left) && IsSignedInteger(right))
                {
                    result = (long)left.GetAs<ulong>() == right.GetAs<long>();
                }
                else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
                {
                    result = left.GetAs<ulong>() == right.GetAs<ulong>();
                }
                else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
                {
                    result = left.GetAs<long>() == right.GetAs<double>();
                }
                else if (IsSignedInteger(left) && IsFloatingPoint(right))
                {
                    result = left.GetAs<ulong>() == right.GetAs<double>();
                }
                else if (IsFloatingPoint(left) && IsSignedInteger(right))
                {
                    result = left.GetAs<double>() == right.GetAs<long>();
                }
                else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
                {
                    result = left.GetAs<double>() == right.GetAs<ulong>();
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
                result = left.GetAs<long>() > right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() > (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() > right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() > right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() > right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() > right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() > right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() > right.GetAs<ulong>();
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
                result = left.GetAs<long>() >= right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() >= (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() >= right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() >= right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() >= right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() >= right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() >= right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() >= right.GetAs<ulong>();
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
                result = left.GetAs<long>() < right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() < (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() < right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() < right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() < right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() < right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() < right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() < right.GetAs<ulong>();
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
                result = left.GetAs<long>() <= right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() <= (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() <= right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() <= right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() <= right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() <= right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() <= right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() <= right.GetAs<ulong>();
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
                result = left.GetAs<long>() - right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() - (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() - right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() - right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<long>() - right.GetAs<double>();
            }
            else if (IsSignedInteger(left) && IsFloatingPoint(right))
            {
                result = left.GetAs<ulong>() - right.GetAs<double>();
            }
            else if (IsFloatingPoint(left) && IsSignedInteger(right))
            {
                result = left.GetAs<double>() - right.GetAs<long>();
            }
            else if (IsFloatingPoint(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<double>() - right.GetAs<ulong>();
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
                result = left.GetAs<long>() & right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() & (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() & right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() & right.GetAs<ulong>();
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
                result = left.GetAs<long>() | right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() | (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() | right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() | right.GetAs<ulong>();
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
                result = left.GetAs<long>() ^ right.GetAs<long>();
            }
            else if (IsSignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<long>() ^ (long)right.GetAs<ulong>();
            }
            else if (IsUnsignedInteger(left) && IsSignedInteger(right))
            {
                result = (long)left.GetAs<ulong>() ^ right.GetAs<long>();
            }
            else if (IsUnsignedInteger(left) && IsUnsignedInteger(right))
            {
                result = left.GetAs<ulong>() ^ right.GetAs<ulong>();
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

        public override AilurusValue EvalNumberLiteral(NumberLiteral literal)
        {
            return ValueFromDatatype(literal.Value, literal.DataType);
        }

        AilurusValue ValueFromDatatype(object Value, AilurusDataType type)
        {
            switch (type)
            {
                case BooleanType _:
                case Signed8Type _:
                case Signed16Type _:
                case Signed32Type _:
                case Signed64Type _:
                case SignedSizeType _:
                case Unsigned8Type _:
                case Unsigned16Type _:
                case Unsigned32Type _:
                case Unsigned64Type _:
                case UnsignedSizeType _:
                case Float32Type _:
                case Float64Type _:
                case NullType _:
                case CharType _:
                    return new DynamicValue() { Value = Value };
                case StructType structType:
                    return new StructInstance()
                    {
                        StructType = structType,
                        Members = (Dictionary<string, MemoryLocation>)Value
                    };
                case AliasType alias:
                    return ValueFromDatatype(Value, alias.BaseType);
                case StringType _:
                    return new StringInstance((string)Value, false);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}