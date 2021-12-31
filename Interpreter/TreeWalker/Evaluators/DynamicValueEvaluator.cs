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
            Upcast(inner, out long llong, out ulong lulong, out double ldouble, out int lwhich);

            DynamicValue result;
            if (lwhich == 0)
            {
                result = -llong;
            }
            else if (lwhich == 1)
            {
                result = -(long)lulong;
            }
            else if (IsFloatingPoint(inner))
            {
                result = -ldouble;
            }
            else
            {
                throw RuntimeError.UnaryError(inner, u.Operator);
            }

            return result;
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

        void Upcast(AilurusValue value, out long longUp, out ulong ulongUp, out double doubleUp, out int which)
        {
            longUp = default;
            ulongUp = default;
            doubleUp = default;
            which = -1;

            if (value.TryGetAs(out sbyte i8))
            {
                longUp = i8;
                which = 0;
            }
            else if (value.TryGetAs(out short i16))
            {
                longUp = i16;
                which = 0;
            }
            else if (value.TryGetAs(out int i32))
            {
                longUp = i32;
                which = 0;
            }
            else if (value.TryGetAs(out long i64))
            {
                longUp = i64;
                which = 0;
            }
            else if (value.TryGetAs(out byte u8))
            {
                ulongUp = u8;
                which = 1;
            }
            else if (value.TryGetAs(out ushort u16))
            {
                ulongUp = u16;
                which = 1;
            }
            else if (value.TryGetAs(out uint u32))
            {
                ulongUp = u32;
                which = 1;
            }
            else if (value.TryGetAs(out ulong u64))
            {
                ulongUp = u64;
                which = 1;
            }
            else if (value.TryGetAs(out float f32))
            {
                doubleUp = f32;
                which = 2;
            }
            else if (value.TryGetAs(out double f64))
            {
                doubleUp = f64;
                which = 2;
            }
        }

        public override AilurusValue EvalPlus(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong + rlong,
                (0, 1) => llong + (long)rulong,
                (0, 2) => llong + rdouble,
                (1, 0) => (long)lulong + rlong,
                (1, 1) => lulong + rulong,
                (1, 2) => lulong + rdouble,
                (2, 0) => ldouble + rlong,
                (2, 1) => ldouble + rulong,
                (2, 2) => ldouble + rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalTimes(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong * rlong,
                (0, 1) => llong * (long)rulong,
                (0, 2) => llong * rdouble,
                (1, 0) => (long)lulong * rlong,
                (1, 1) => lulong * rulong,
                (1, 2) => lulong * rdouble,
                (2, 0) => ldouble * rlong,
                (2, 1) => ldouble * rulong,
                (2, 2) => ldouble * rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalDivision(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong / rlong,
                (0, 1) => llong / (long)rulong,
                (0, 2) => llong / rdouble,
                (1, 0) => (long)lulong / rlong,
                (1, 1) => lulong / rulong,
                (1, 2) => lulong / rdouble,
                (2, 0) => ldouble / rlong,
                (2, 1) => ldouble / rulong,
                (2, 2) => ldouble / rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalEquality(AilurusValue left, AilurusValue right, Binary b)
        {
            DynamicValue result;
            if (IsNumeric(left) && IsNumeric(right))
            {
                Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
                Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

                result = (lwhich, rwhich) switch
                {
                    (0, 0) => llong == rlong,
                    (0, 1) => llong == (long)rulong,
                    (0, 2) => llong == rdouble,
                    (1, 0) => (long)lulong == rlong,
                    (1, 1) => lulong == rulong,
                    (1, 2) => lulong == rdouble,
                    (2, 0) => ldouble == rlong,
                    (2, 1) => ldouble == rulong,
                    (2, 2) => ldouble == rdouble,
                    _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
                };
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
            else if (left is Pointer lp && right is Pointer rp)
            {
                result = lp.Memory == rp.Memory;
            }
            else
            {
                throw new NotImplementedException();
            }

            return result;
        }

        public override AilurusValue EvalGreater(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong > rlong,
                (0, 1) => llong > (long)rulong,
                (0, 2) => llong > rdouble,
                (1, 0) => (long)lulong > rlong,
                (1, 1) => lulong > rulong,
                (1, 2) => lulong > rdouble,
                (2, 0) => ldouble > rlong,
                (2, 1) => ldouble > rulong,
                (2, 2) => ldouble > rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }
        public override AilurusValue EvalGreaterEqual(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong >= rlong,
                (0, 1) => llong >= (long)rulong,
                (0, 2) => llong >= rdouble,
                (1, 0) => (long)lulong >= rlong,
                (1, 1) => lulong >= rulong,
                (1, 2) => lulong >= rdouble,
                (2, 0) => ldouble >= rlong,
                (2, 1) => ldouble >= rulong,
                (2, 2) => ldouble >= rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }
        public override AilurusValue EvalLess(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong < rlong,
                (0, 1) => llong < (long)rulong,
                (0, 2) => llong < rdouble,
                (1, 0) => (long)lulong < rlong,
                (1, 1) => lulong < rulong,
                (1, 2) => lulong < rdouble,
                (2, 0) => ldouble < rlong,
                (2, 1) => ldouble < rulong,
                (2, 2) => ldouble < rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }
        public override AilurusValue EvalLessEqual(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong <= rlong,
                (0, 1) => llong <= (long)rulong,
                (0, 2) => llong <= rdouble,
                (1, 0) => (long)lulong <= rlong,
                (1, 1) => lulong <= rulong,
                (1, 2) => lulong <= rdouble,
                (2, 0) => ldouble <= rlong,
                (2, 1) => ldouble <= rulong,
                (2, 2) => ldouble <= rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalMinus(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong - rlong,
                (0, 1) => llong - (long)rulong,
                (0, 2) => llong - rdouble,
                (1, 0) => (long)lulong - rlong,
                (1, 1) => lulong - rulong,
                (1, 2) => lulong - rdouble,
                (2, 0) => ldouble - rlong,
                (2, 1) => ldouble - rulong,
                (2, 2) => ldouble - rdouble,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalBitwiseAnd(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong & rlong,
                (0, 1) => llong & (long)rulong,
                (1, 0) => (long)lulong & rlong,
                (1, 1) => lulong & rulong,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalBitwiseOr(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong | rlong,
                (0, 1) => llong | (long)rulong,
                (1, 0) => (long)lulong | rlong,
                (1, 1) => lulong | rulong,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

            return result;
        }

        public override AilurusValue EvalBitwiseXOr(AilurusValue left, AilurusValue right, Binary b)
        {
            Upcast(left, out long llong, out ulong lulong, out double ldouble, out int lwhich);
            Upcast(right, out long rlong, out ulong rulong, out double rdouble, out int rwhich);

            DynamicValue result;
            result = (lwhich, rwhich) switch
            {
                (0, 0) => llong ^ rlong,
                (0, 1) => llong ^ (long)rulong,
                (1, 0) => (long)lulong ^ rlong,
                (1, 1) => lulong ^ rulong,
                _ => throw RuntimeError.BinaryOperatorError(left, right, b.Operator)
            };

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