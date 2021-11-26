using System;
using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.Runtime
{


    public abstract class AilurusValue
    {
        public abstract bool AssertType(AilurusDataType dataType);
        public virtual T GetAs<T>()
        {
            if (this is T t)
            {
                return t;
            }
            else
            {
                throw new RuntimeError($"Unable to convert struct to {nameof(T)}", 0, 0, string.Empty);
            }
        }
        public abstract string TypeName { get; }
    }

    public class DynamicValue : AilurusValue
    {
        public static implicit operator DynamicValue(bool b)
        {
            return new DynamicValue() { Value = b };
        }
        public static implicit operator DynamicValue(int i)
        {
            return new DynamicValue() { Value = i };
        }

        public static implicit operator DynamicValue(long l)
        {
            return new DynamicValue() { Value = l };
        }

        public static implicit operator DynamicValue(float f)
        {
            return new DynamicValue() { Value = f };
        }

        public static implicit operator DynamicValue(double d)
        {
            return new DynamicValue() { Value = d };
        }

        public static implicit operator DynamicValue(string s)
        {
            return new DynamicValue() { Value = s };
        }

        public override T GetAs<T>()
        {
            return (T)Value;
        }

        public override string TypeName { get => Value.GetType().ToString(); }

        public override bool AssertType(AilurusDataType dataType)
        {
            switch (dataType)
            {
                case ByteType b:
                    if (b.Unsigned)
                    {
                        return Value is uint;
                    }
                    else
                    {
                        return Value is int;
                    }
                case ShortType s:
                    if (s.Unsigned)
                    {
                        return Value is uint;
                    }
                    else
                    {
                        return Value is int;
                    }
                case IntType i:
                    if (i.Unsigned)
                    {
                        return Value is uint;
                    }
                    else
                    {
                        return Value is int;
                    }
                case FloatType _:
                case DoubleType _:
                    return Value is double;

                case NullType _:
                case null:
                    return Value is null;

                case BooleanType _:
                    return Value is bool;

                case StringType _:
                    return Value is string;

                default:
                    throw new NotImplementedException();
            }
        }

        public object Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class FunctionPointer : AilurusValue
    {
        public FunctionDeclaration FunctionDeclaration { get; set; }

        public override string TypeName => FunctionDeclaration.DataType.DataTypeName;

        public override bool AssertType(AilurusDataType dataType)
        {
            // Don't need to implement since static type checking should handle it...
            if (dataType == FunctionDeclaration.DataType)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class StructInstance : AilurusValue
    {
        public override string TypeName { get => StructType.ToString(); }

        public override bool AssertType(AilurusDataType dataType)
        {
            if (dataType is StructType)
            {
                return StructType == dataType;
            }
            else
            {
                return false;
            }
        }

        public StructType StructType { get; set; }
        public Dictionary<string, AilurusValue> Members { get; set; }
    }
}