using System.Collections.Generic;

namespace AilurusLang.DataType
{
    public abstract class DataType { }

    public class VoidType : DataType
    {
        public static readonly VoidType Instance = new VoidType();
    }

    public class BooleanType : DataType
    {
        public static readonly BooleanType Instance = new BooleanType();
    }

    public class ByteType : DataType
    {
        public static readonly ByteType InstanceSigned = new ByteType();
        public static readonly ByteType InstanceUnsigned = new ByteType() { Unsigned = true };

        public bool Unsigned { get; set; } = false;
    }

    public class ShortType : DataType
    {
        public static readonly ShortType InstanceSigned = new ShortType();
        public static readonly ShortType InstanceUnsigned = new ShortType() { Unsigned = true };
        public bool Unsigned { get; set; } = false;
    }

    public class IntType : DataType
    {
        public static readonly IntType InstanceSigned = new IntType();
        public static readonly IntType InstanceUnsigned = new IntType() { Unsigned = true };
        public bool Unsigned { get; set; } = false;
    }

    public class FloatType : DataType
    {
        public readonly static FloatType Instance = new FloatType();
    }

    public class DoubleType : DataType
    {
        public readonly static DoubleType Instance = new DoubleType();
    }

    public class StringType : DataType
    {
        public readonly static StringType Instance = new StringType();
    }

    public class StructType : DataType
    {
        public Dictionary<string, DataType> Definitions { get; set; }
    }
    public class FunctionType : DataType
    {
        public DataType ReturnType { get; set; }
        public List<DataType> ArgumentTypes { get; set; }
    }
    public class AliasType : DataType
    {
        public string Alias { get; set; }
        public DataType BaseType { get; set; }
    }

    public class PointerType : DataType
    {
        public DataType BaseType { get; set; }
    }

    public class NullType : DataType
    {
        public static readonly NullType Instance = new NullType();
    }
}