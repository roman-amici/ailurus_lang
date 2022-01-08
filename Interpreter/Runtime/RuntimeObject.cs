using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AilurusLang.DataType;
using AilurusLang.Interpreter.TreeWalker.Evaluators;
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
                throw new RuntimeError($"Unable to convert {GetType()} to {typeof(T)}", 0, 0, string.Empty);
            }
        }

        public virtual bool TryGetAs<T>(out T t)
        {
            t = default;
            if (this is T tt)
            {
                t = tt;
                return true;
            }
            return false;
        }

        public abstract string TypeName { get; }

        public virtual AilurusValue ByValue()
        {
            return this;
        }

        public virtual void MarkInvalid()
        {

        }

        public virtual AilurusValue CastTo(AilurusDataType dataType) => throw new RuntimeError("Invalid typecast.");
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

        public static implicit operator DynamicValue(short i)
        {
            return new DynamicValue() { Value = i };
        }

        public static implicit operator DynamicValue(ushort i)
        {
            return new DynamicValue() { Value = i };
        }

        public static implicit operator DynamicValue(sbyte i)
        {
            return new DynamicValue() { Value = i };
        }

        public static implicit operator DynamicValue(byte i)
        {
            return new DynamicValue() { Value = i };
        }

        public static implicit operator DynamicValue(uint i)
        {
            return new DynamicValue() { Value = i };
        }

        public static implicit operator DynamicValue(long l)
        {
            return new DynamicValue() { Value = l };
        }

        public static implicit operator DynamicValue(ulong l)
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

        public override AilurusValue CastTo(AilurusDataType dataType)
        {
            DynamicValue value = null;
            if (dataType is UnsignedSizeType || dataType is Unsigned64Type)
            {
                value = GetAs<ulong>();
            }
            else if (dataType is SignedSizeType || dataType is Signed64Type)
            {
                value = GetAs<long>();
            }
            else if (dataType is Signed32Type)
            {
                value = GetAs<int>();
            }
            else if (dataType is Unsigned32Type)
            {
                value = GetAs<uint>();
            }
            else if (dataType is Signed16Type)
            {
                value = GetAs<short>();
            }
            else if (dataType is Unsigned16Type)
            {
                value = GetAs<ushort>();
            }
            else if (dataType is Unsigned8Type)
            {
                value = GetAs<byte>();
            }
            else if (dataType is Signed8Type)
            {
                value = GetAs<sbyte>();
            }
            else if (dataType is Float64Type)
            {
                value = GetAs<double>();
            }
            else if (dataType is Float32Type)
            {
                value = GetAs<float>();
            }

            return value ?? base.CastTo(dataType);
        }

        public override T GetAs<T>()
        {
            object value = Value;

            // C# will only allow you to cast an object to a type that the underlying value inherits
            // Thus (int)Value will cause an excpetion where (int)(long)Value would not.
            // Since we use "int" for array access we need a special case here.
            if (DynamicValueEvaluator.IsNumeric(this))
            {
                DynamicValueEvaluator.Upcast(this, out long llong, out ulong lulong, out double ldouble, out int which);

                var type = typeof(T);
                if (typeof(long) == type)
                {
                    value = which switch
                    {
                        0 => (long)llong,
                        1 => (long)lulong,
                        2 => (long)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(ulong) == type)
                {
                    value = which switch
                    {
                        0 => (ulong)llong,
                        1 => (ulong)lulong,
                        2 => (ulong)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(int) == type)
                {
                    value = which switch
                    {
                        0 => (int)llong,
                        1 => (int)lulong,
                        2 => (int)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(uint) == type)
                {
                    value = which switch
                    {
                        0 => (uint)llong,
                        1 => (uint)lulong,
                        2 => (uint)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(short) == type)
                {
                    value = which switch
                    {
                        0 => (short)llong,
                        1 => (short)lulong,
                        2 => (short)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(ushort) == type)
                {
                    value = which switch
                    {
                        0 => (ushort)llong,
                        1 => (ushort)lulong,
                        2 => (ushort)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(sbyte) == type)
                {
                    value = which switch
                    {
                        0 => (sbyte)llong,
                        1 => (sbyte)lulong,
                        2 => (sbyte)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(byte) == type)
                {
                    value = which switch
                    {
                        0 => (byte)llong,
                        1 => (byte)lulong,
                        2 => (byte)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(double) == type)
                {
                    value = which switch
                    {
                        0 => (double)llong,
                        1 => (double)lulong,
                        2 => (double)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
                else if (typeof(float) == type)
                {
                    value = which switch
                    {
                        0 => (float)llong,
                        1 => (float)lulong,
                        2 => (float)ldouble,
                        _ => throw new NotImplementedException()
                    };
                }
            }

            return (T)value;
        }

        public override bool TryGetAs<T>(out T t)
        {
            t = default;
            if (Value is T tt)
            {
                t = tt;
                return true;
            }
            return false;
        }

        public override string TypeName { get => Value.GetType().ToString(); }

        public override bool AssertType(AilurusDataType dataType)
        {
            switch (dataType)
            {
                case Signed8Type _:
                    return Value is sbyte;
                case Unsigned8Type _:
                    return Value is byte;
                case Signed16Type _:
                    return Value is short;
                case Unsigned16Type _:
                    return Value is ushort;
                case Signed32Type _:
                    return Value is int;
                case Unsigned32Type _:
                    return Value is uint;
                case Signed64Type _:
                    return Value is long;
                case Unsigned64Type _:
                    return Value is ulong;
                case SignedSizeType _:
                    return Value is long;
                case UnsignedSizeType _:
                    return Value is ulong;
                case Float32Type _:
                    return Value is float;
                case Float64Type _:
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

    public class MemoryLocation : AilurusValue
    {
        public MemoryLocation() { }
        public MemoryLocation(MemoryLocation m)
        {
            Value = m.Value;
        }

        public AilurusValue Value { get; set; }
        public bool IsValid { get; set; } = true;
        public bool IsOnHeap { get; set; }

        public override string TypeName => "MemoryLocation";

        public override bool AssertType(AilurusDataType dataType)
        {
            throw new NotImplementedException();
        }

        public override AilurusValue ByValue()
        {
            return new MemoryLocation()
            {
                Value = Value.ByValue()
            };
        }

        public override void MarkInvalid()
        {
            IsValid = false;
            if (Value != null)
            {
                Value.MarkInvalid();
            }
        }
    }

    public class Pointer : AilurusValue
    {
        public MemoryLocation Memory { get; set; }
        public AilurusValue Deref()
        {
            return Memory.Value;
        }
        public void Assign(AilurusValue value)
        {
            Memory.Value = value;
        }

        public override bool AssertType(AilurusDataType dataType)
        {
            return dataType is PointerType;
        }

        public override string TypeName => "Pointer";

        public bool IsNull { get; protected set; }

        public virtual bool IsValid
        {
            get => !IsNull && Memory.IsValid;
        }
    }

    public class NullPointer : Pointer
    {
        public NullPointer() : base()
        {
            IsNull = true;
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
            var newMembers = new Dictionary<string, MemoryLocation>();
            foreach (var kvp in Members)
            {
                newMembers.Add(kvp.Key, (MemoryLocation)kvp.Value.ByValue());
            }

            return new StructInstance()
            {
                StructType = StructType,
                Members = newMembers
            };
        }

        public StructType StructType { get; set; }
        public Dictionary<string, MemoryLocation> Members { get; set; }

        public AilurusValue this[string s]
        {
            get => Members[s].Value;
            set => Members[s].Value = value;
        }

        public override void MarkInvalid()
        {
            foreach (var v in Members.Values)
            {
                v.MarkInvalid();
            }
        }

        public MemoryLocation GetMemberAddress(string memberName)
        {
            return Members[memberName];
        }
    }

    public interface IArrayInstanceLike
    {
        int Count { get; }
        AilurusValue this[int i] { get; set; }
        bool AccessIsValid(int i);
        IEnumerable<AilurusValue> ValueList();
        MemoryLocation GetElementAddress(int i);
    }

    public class StringInstance : ArrayInstance
    {
        public StringInstance(string initialValue, bool isOnHeap) :
            base(initialValue.Select(c => new DynamicValue() { Value = c }), isOnHeap)
        {
        }

        public StringInstance(StringInstance s, bool isOnHeap) : base(s, isOnHeap) { }

        public override string ToString()
        {
            var charList = new List<char>();
            foreach (var m in Values)
            {
                charList.Add(m.Value.GetAs<char>());
            }

            return string.Join("", charList);
        }
    }

    public class ArrayInstance : AilurusValue, IArrayInstanceLike
    {
        public ArrayInstance(IEnumerable<AilurusValue> values, bool isOnHeap)
        {
            Values = values.Select(v => new MemoryLocation() { Value = v, IsOnHeap = isOnHeap }).ToList();
        }

        public ArrayInstance(ArrayInstance a, bool isOnHeap) : this(a.ValueList(), isOnHeap) { }

        public int Count => Values.Count;

        public AilurusValue this[int i]
        {
            get => Values[i].Value;
            set => Values[i].Value = value;
        }

        public ArrayType ArrayType { get; set; }
        public List<MemoryLocation> Values { get; set; }

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
            var v = Values.Select(v => v.Value);
            return $"[{string.Join(",", v)}]";
        }

        public bool AccessIsValid(int i)
        {
            return i < Count && Values[i].IsValid;
        }

        public IEnumerable<AilurusValue> ValueList()
            => Values.Select(v => v.Value);

        public override void MarkInvalid()
        {
            foreach (var m in Values)
            {
                m.MarkInvalid();
            }
        }

        public MemoryLocation GetElementAddress(int i)
        {
            return Values[i];
        }
    }

    public class TupleInstance : AilurusValue
    {
        public TupleInstance(IEnumerable<AilurusValue> values, bool isOnHeap)
        {
            Elements = values
                .Select(v => new MemoryLocation() { Value = v, IsOnHeap = isOnHeap })
                .ToList();
        }

        public List<MemoryLocation> Elements { get; set; }

        public override string TypeName => "Tuple";

        public override string ToString()
        {
            var inner = string.Join(',', Elements.Select(m => m.Value));
            return $"({inner})";
        }

        public override bool AssertType(AilurusDataType dataType)
        {
            return dataType is TupleType;
        }

        public AilurusValue this[int i] => Elements[i].Value;

        public MemoryLocation GetElementAddress(int i) => Elements[i];

        public override void MarkInvalid() => Elements.ForEach(v => v.MarkInvalid());

        public override AilurusValue ByValue()
        {
            return new TupleInstance(Elements.Select(m => m.Value.ByValue()), false);
        }
    }

    public class VariantInstance : AilurusValue
    {
        public VariantType VariantType { get; set; }
        public VariantMemberType VariantMemberType { get; set; }

        public int Index { get; set; }

        public MemoryLocation Value { get; set; }

        public override string TypeName => $"{VariantType.DataTypeName}${VariantMemberType.MemberName}";

        public override bool AssertType(AilurusDataType dataType)
        {
            if (dataType is VariantType v)
            {
                return v == VariantType;
            }
            else if (dataType is VariantMemberType m)
            {
                return m == VariantMemberType;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {

            return $"{TypeName}({Value.Value})";
        }
    }
}