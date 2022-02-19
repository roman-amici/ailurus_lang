namespace AilurusLang.ByteCodeInterpreter.Compiler
{
    public abstract class AilurusValue
    {
        public abstract V GetAs<V>();

        public abstract dynamic GetDynamic();

        public static implicit operator AilurusValue(ulong v)
        {
            return new AilurusInt<ulong>(v);
        }

        public static implicit operator AilurusValue(uint v)
        {
            return new AilurusInt<uint>(v);
        }

        public static implicit operator AilurusValue(ushort v)
        {
            return new AilurusInt<ushort>(v);
        }

        public static implicit operator AilurusValue(byte v)
        {
            return new AilurusInt<byte>(v);
        }

        public static implicit operator AilurusValue(long v)
        {
            return new AilurusInt<long>(v);
        }

        public static implicit operator AilurusValue(int v)
        {
            return new AilurusInt<int>(v);
        }

        public static implicit operator AilurusValue(short v)
        {
            return new AilurusInt<short>(v);
        }

        public static implicit operator AilurusValue(sbyte v)
        {
            return new AilurusInt<sbyte>(v);
        }

        public static implicit operator AilurusValue(double v)
        {
            return new AilurusFloat<double>(v);
        }

        public static implicit operator AilurusValue(float v)
        {
            return new AilurusFloat<float>(v);
        }
    }

    public abstract class TypedValue<T> : AilurusValue
    {
        public T Value { get; set; }
        public override V GetAs<V>()
        {
            return (V)(dynamic)Value;
        }

        public override dynamic GetDynamic()
        {
            return (dynamic)Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class AilurusInt<T> : TypedValue<T>
    {
        public AilurusInt(T value)
        {
            Value = value;
        }
    }

    public class AilurusFloat<T> : TypedValue<T>
    {
        public AilurusFloat(T value)
        {
            Value = value;
        }
    }

    public class StringLiteral : TypedValue<string>
    {
        public StringLiteral(string value)
        {
            Value = value;
        }
    }

    public class AilurusArray<T> : TypedValue<T[]>
    {
        public AilurusArray(T[] value)
        {
            Value = value;
        }
    }

    public class ValuePointer : TypedValue<uint>
    {
        public ValuePointer(uint value)
        {
            Value = value;
        }
    }
}