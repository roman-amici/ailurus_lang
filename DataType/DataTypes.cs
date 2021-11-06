using System.Collections.Generic;

namespace AilurusLang.DataType
{
    public abstract class AilurusDataType { }

    public class VoidType : AilurusDataType
    {
        public static readonly VoidType Instance = new VoidType();
    }

    public class BooleanType : AilurusDataType
    {
        public static readonly BooleanType Instance = new BooleanType();
    }

    public class ByteType : AilurusDataType
    {
        public static readonly ByteType InstanceSigned = new ByteType();
        public static readonly ByteType InstanceUnsigned = new ByteType() { Unsigned = true };

        public bool Unsigned { get; set; } = false;
    }

    public class ShortType : AilurusDataType
    {
        public static readonly ShortType InstanceSigned = new ShortType();
        public static readonly ShortType InstanceUnsigned = new ShortType() { Unsigned = true };
        public bool Unsigned { get; set; } = false;
    }

    public class IntType : AilurusDataType
    {
        public static readonly IntType InstanceSigned = new IntType();
        public static readonly IntType InstanceUnsigned = new IntType() { Unsigned = true };
        public bool Unsigned { get; set; } = false;
    }

    public class FloatType : AilurusDataType
    {
        public readonly static FloatType Instance = new FloatType();
    }

    public class DoubleType : AilurusDataType
    {
        public readonly static DoubleType Instance = new DoubleType();
    }

    public class StringType : AilurusDataType
    {
        public readonly static StringType Instance = new StringType();
    }

    public class StructType : AilurusDataType
    {
        public string TypeName { get; set; }
        public Dictionary<string, AilurusDataType> Definitions { get; set; }
    }
    public class FunctionType : AilurusDataType
    {
        public AilurusDataType ReturnType { get; set; }
        public List<AilurusDataType> ArgumentTypes { get; set; }
    }
    public class AliasType : AilurusDataType
    {
        public string Alias { get; set; }
        public AilurusDataType BaseType { get; set; }
    }

    public class PointerType : AilurusDataType
    {
        public AilurusDataType BaseType { get; set; }
    }

    public class NullType : AilurusDataType
    {
        public static readonly NullType Instance = new NullType();
    }
}