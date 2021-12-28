using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual void MarkInvalid()
        {

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
            var inner = string.Join(',', Elements);
            return $"({inner})";
        }

        public override bool AssertType(AilurusDataType dataType)
        {
            return dataType is TupleType;
        }

        public AilurusValue this[int i] => Elements[i].Value;

        public MemoryLocation GetElementAddress(int i) => Elements[i];

        public override void MarkInvalid() => Elements.ForEach(v => v.MarkInvalid());
    }
}