using System.Collections.Generic;
using System.Linq;
using AilurusLang.Parsing.AST;

namespace AilurusLang.DataType
{

    public abstract class AilurusDataType
    {
        public static bool IsNumeric(AilurusDataType t)
        {
            return t is NumericType;
        }

        public virtual string DataTypeName { get => GetType().ToString(); }
    }

    public class PlaceholderType : AilurusDataType
    {
        public TypeName TypeName { get; set; }
        public AilurusDataType ResolvedType { get; set; }
    }

    public class VoidType : AilurusDataType
    {
        public static readonly VoidType Instance = new VoidType();
        public override string DataTypeName => "void";
    }

    public class BooleanType : AilurusDataType
    {
        public static readonly BooleanType Instance = new BooleanType();
        public override string DataTypeName => "bool";
    }

    public abstract class NumericType : AilurusDataType { }

    public abstract class IntegralType : NumericType
    {
        public bool Unsigned { get; set; } = false;

        public abstract IntegralType SignedInstance { get; }
    }

    public class ByteType : IntegralType
    {
        public static readonly ByteType InstanceSigned = new ByteType();
        public static readonly ByteType InstanceUnsigned = new ByteType() { Unsigned = true };
        public override IntegralType SignedInstance => InstanceSigned;
        public override string DataTypeName => Unsigned ? "ubyte" : "byte";
    }

    public class ShortType : IntegralType
    {
        public static readonly ShortType InstanceSigned = new ShortType();
        public static readonly ShortType InstanceUnsigned = new ShortType() { Unsigned = true };
        public override IntegralType SignedInstance => InstanceSigned;
        public override string DataTypeName => Unsigned ? "ushort" : "short";
    }

    public class IntType : IntegralType
    {
        public static readonly IntType InstanceSigned = new IntType();
        public static readonly IntType InstanceUnsigned = new IntType() { Unsigned = true };
        public override IntegralType SignedInstance => InstanceSigned;
        public override string DataTypeName => Unsigned ? "uint" : "int";
    }

    public class FloatType : NumericType
    {
        public readonly static FloatType Instance = new FloatType();
        public override string DataTypeName => "float";
    }

    public class DoubleType : NumericType
    {
        public readonly static DoubleType Instance = new DoubleType();
        public override string DataTypeName => "double";
    }

    public class StringType : AilurusDataType, IArrayLikeType
    {

        public readonly static StringType Instance = new StringType();

        public AilurusDataType ElementType => CharType.Instance;

        public bool IsVariable { get; set; }
        public override string DataTypeName => "string";
    }

    public class CharType : AilurusDataType
    {
        public readonly static CharType Instance = new CharType();
        public override string DataTypeName => "char";
    }

    public class StructType : AilurusDataType
    {
        public string StructName { get; set; }
        // TODO: enforce ordering for C- ABI
        public Dictionary<string, AilurusDataType> Definitions { get; set; }
        public override string DataTypeName => $"struct {StructName}";
    }
    public class FunctionType : AilurusDataType
    {
        public AilurusDataType ReturnType { get; set; }
        public List<bool> ArgumentMutable { get; set; }
        public List<AilurusDataType> ArgumentTypes { get; set; }
        public override string DataTypeName
        {
            get
            {
                var s = "fn(";
                foreach (var argType in ArgumentTypes)
                {
                    s += argType.DataTypeName;
                }
                return s + ")" + ReturnType.DataTypeName;
            }
        }
    }
    public class AliasType : AilurusDataType
    {
        public string Alias { get; set; }
        public AilurusDataType BaseType { get; set; }
        public override string DataTypeName => $"{BaseType.DataTypeName}({Alias})";
    }

    public class PointerType : AilurusDataType
    {
        public bool IsVariable { get; set; }
        public AilurusDataType BaseType { get; set; }
        public override string DataTypeName => $"{BaseType.DataTypeName} ptr";
    }

    public interface IArrayLikeType
    {
        AilurusDataType ElementType { get; }
        public bool IsVariable { get; set; }
    }

    public class ArrayType : AilurusDataType, IArrayLikeType
    {

        public AilurusDataType ElementType => BaseType;

        public AilurusDataType BaseType { get; set; }
        public override string DataTypeName => $"[{BaseType.DataTypeName}]";
        public bool IsVariable { get; set; }
    }

    public class NullType : AilurusDataType
    {
        public static readonly NullType Instance = new NullType();
        public override string DataTypeName => "null";
    }

    public class ErrorType : AilurusDataType
    {
        public static readonly ErrorType Instance = new ErrorType();
        public override string DataTypeName => "Error";
    }

    public class TupleType : AilurusDataType
    {
        public List<AilurusDataType> MemberTypes { get; set; }
        public override string DataTypeName
        {
            get
            {
                var inner = string.Join(',', MemberTypes.Select(d => d.DataTypeName));
                return $"({inner})";
            }
        }
    }

    public class VariantType : AilurusDataType
    {
        public string VariantName { get; set; }
        public Dictionary<string, VariantMemberType> Members { get; set; }
    }

    public class VariantMemberType : AilurusDataType
    {
        public string MemberName { get; set; }
        public AilurusDataType InnerType { get; set; }
        public int MemberIndex { get; set; }
    }
}
