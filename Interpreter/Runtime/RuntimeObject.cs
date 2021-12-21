using System;
using System.Collections.Generic;
using System.Text;
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

        public virtual AilurusValue ByValue()
        {
            return this;
        }
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

                case CharType _:
                    return Value is char;

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

    public abstract class Pointer : AilurusValue
    {
        public abstract AilurusValue Deref();
        public abstract void Assign(AilurusValue value);

        public override string TypeName => "Pointer";

        public bool IsNull { get; protected set; }

        public virtual bool IsValid
        {
            get => !IsNull;
        }
    }

    public class NullPointer : Pointer
    {
        public NullPointer() : base()
        {
            IsNull = true;
        }

        public override bool AssertType(AilurusDataType dataType)
        {
            if (dataType is PointerType)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Assign(AilurusValue value)
        {
            throw new NotImplementedException();
        }

        public override AilurusValue Deref()
        {
            throw new NotImplementedException();
        }
    }

    public class StackPointer : Pointer
    {
        public TreeWalkerEnvironment Environment { get; set; }
        public Resolution Variable { get; set; }

        public override bool IsValid
        {
            get
            {
                if (!IsNull)
                {
                    return Environment.IsValid;
                }
                return false;
            }
        }

        public override bool AssertType(AilurusDataType dataType)
        {
            if (dataType is PointerType p && IsValid)
            {
                var variable = Environment.GetValue(Variable);
                return variable.AssertType(p.BaseType);
            }
            else
            {
                return false;
            }
        }

        public override AilurusValue Deref()
        {
            if (IsValid)
            {
                return Environment.GetValue(Variable);
            }
            else
            {
                return null;
            }
        }

        public override void Assign(AilurusValue value)
        {
            if (IsValid)
            {
                Environment.SetValue(Variable, value);
            }
        }
    }

    public class StructMemberPointer : StackPointer
    {
        public List<string> FieldNames { get; set; }

        private StructInstance GetBaseStructInstance()
        {
            var value = base.Deref();
            StructInstance instance = null;
            foreach (var name in FieldNames)
            {
                while (value is Pointer p)
                {
                    value = p.Deref();
                }
                instance = value.GetAs<StructInstance>();
                value = instance.Members[name];
            }

            return instance;
        }

        public override void Assign(AilurusValue value)
        {
            var instance = GetBaseStructInstance();
            instance.Members[FieldNames[^1]] = value;
        }

        public override AilurusValue Deref()
        {
            var instance = GetBaseStructInstance();
            return instance.Members[FieldNames[^1]];
        }
    }

    public class HeapPointer : Pointer
    {
        public AilurusValue Value { get; set; }
        public bool Initialized { get; set; } = false;

        public override bool IsValid
        {
            get => !IsNull && Initialized;
        }

        public override bool AssertType(AilurusDataType dataType)
        {
            if (dataType is PointerType p && IsValid)
            {
                return Value.AssertType(p.BaseType);
            }
            else
            {
                return false;
            }
        }

        public override void Assign(AilurusValue value)
        {
            Value = value;
            Initialized = true;
        }

        public override AilurusValue Deref()
        {
            return Value;
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

        public override AilurusValue ByValue()
        {
            return new StructInstance()
            {
                StructType = StructType,
                Members = new Dictionary<string, AilurusValue>(Members)
            };
        }

        public StructType StructType { get; set; }
        public Dictionary<string, AilurusValue> Members { get; set; }
    }

    public interface IArrayInstanceLike
    {
        int Count { get; }

        bool IsOnHeap { get; set; }
        bool Initialized { get; set; }

        AilurusValue this[int i] { get; set; }
    }

    public class StringInstance : AilurusValue, IArrayInstanceLike
    {
        public override string TypeName => "string";

        // Maybe just store it as a char array?
        public string Value { get; set; }

        public bool IsOnHeap { get; set; }
        public bool Initialized { get; set; }

        public override bool AssertType(AilurusDataType dataType)
        {
            return dataType is StringType;
        }

        public AilurusValue this[int i]
        {
            get => new DynamicValue() { Value = Value[i] };
            set
            {
                var newString = Value.ToCharArray();
                newString[i] = value.GetAs<char>();
                Value = new string(newString);
            }
        }

        public int Count => Value.Length;

        public override string ToString()
        {
            return Value;
        }
    }

    public class ArrayInstance : AilurusValue, IArrayInstanceLike
    {
        public int Count => Values.Count;

        public AilurusValue this[int i]
        {
            get => Values[i];
            set => Values[i] = value;
        }

        public ArrayType ArrayType { get; set; }
        public List<AilurusValue> Values { get; set; }

        public bool IsOnHeap { get; set; }
        public bool Initialized { get; set; }

        public override string TypeName => $"[{ArrayType.DataTypeName}]";

        public override bool AssertType(AilurusDataType dataType)
        {
            if (dataType is ArrayType a)
            {
                return a.BaseType == ArrayType.BaseType;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return $"[{string.Join(",", Values)}]";
        }
    }
}