using System.Collections.Generic;
using System.Linq;
using AilurusLang.Parsing.AST;

namespace AilurusLang.DataType
{

    public abstract class AilurusDataType
    {
        public virtual string DataTypeName { get => GetType().ToString(); }
        public virtual bool Concrete => true;
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
        public override bool Concrete => false;
    }

    public class AnyType : AilurusDataType
    {
        public static readonly AnyType Instance = new AnyType();
        public override string DataTypeName => "any";
        public override bool Concrete => false;
    }

    public class BooleanType : AilurusDataType
    {
        public static readonly BooleanType Instance = new BooleanType();
        public override string DataTypeName => "bool";
    }

    public abstract class NumericType : AilurusDataType
    {
        public abstract uint NumBytes { get; }
        public abstract uint IntegralBits { get; }
        public abstract bool Signed { get; }
    }

    public abstract class IntegralType : NumericType
    {
        public override uint IntegralBits => Signed ? NumBytes * 8 - 1 : NumBytes * 8;

        public override bool Signed => false;

        public static IntegralType IntegralTypeBySize(uint size, bool isSigned)
        {
            return size switch
            {
                1 => isSigned ? (IntegralType)Signed8Type.Instance : Unsigned8Type.Instance,
                2 => isSigned ? (IntegralType)Signed16Type.Instance : Unsigned16Type.Instance,
                4 => isSigned ? (IntegralType)Signed32Type.Instance : Unsigned32Type.Instance,
                8 => isSigned ? (IntegralType)Signed64Type.Instance : Unsigned64Type.Instance,
                _ => null
            };
        }
    }

    public abstract class SignedIntegralType : IntegralType
    {
        public override bool Signed => true;
    }

    public abstract class FloatingPointType : NumericType
    {
        public override bool Signed => true;
    }

    public class Signed8Type : SignedIntegralType
    {
        public static readonly Signed8Type Instance = new Signed8Type();
        public override string DataTypeName => "i8";

        public override uint NumBytes => 1;
    }

    public class Unsigned8Type : IntegralType
    {
        public static readonly Unsigned8Type Instance = new Unsigned8Type();
        public override string DataTypeName => "u8";

        public override uint NumBytes => 1;
    }

    public class Signed16Type : SignedIntegralType
    {
        public static readonly Signed16Type Instance = new Signed16Type();
        public override string DataTypeName => "i16";

        public override uint NumBytes => 2;
    }

    public class Unsigned16Type : IntegralType
    {
        public static readonly Unsigned16Type Instance = new Unsigned16Type();
        public override string DataTypeName => "u16";

        public override uint NumBytes => 2;
    }

    public class Signed32Type : SignedIntegralType
    {
        public static readonly Signed32Type Instance = new Signed32Type();
        public override string DataTypeName => "i32";

        public override uint NumBytes => 4;
    }

    public class Unsigned32Type : IntegralType
    {
        public static readonly Unsigned32Type Instance = new Unsigned32Type();
        public override string DataTypeName => "u32";

        public override uint NumBytes => 4;
    }

    public class Signed64Type : SignedIntegralType
    {
        public static readonly Signed64Type Instance = new Signed64Type();
        public override string DataTypeName => "i64";

        public override uint NumBytes => 8;
    }

    public class Unsigned64Type : IntegralType
    {
        public static readonly Unsigned64Type Instance = new Unsigned64Type();
        public override string DataTypeName => "u64";

        public override uint NumBytes => 8;
    }

    public class Float32Type : FloatingPointType
    {
        public readonly static Float32Type Instance = new Float32Type();

        public override uint NumBytes => 4;
        public override uint IntegralBits => 24; // Number of bits in fraction portion
    }

    public class Float64Type : FloatingPointType
    {
        public readonly static Float64Type Instance = new Float64Type();
        public override string DataTypeName => "f64";

        public override uint NumBytes => 8;
        public override uint IntegralBits => 52;
    }

    public class SignedSizeType : IntegralType
    {
        public static uint MachineSize { get; set; } = 8;
        public readonly static SignedSizeType Instance = new SignedSizeType();

        public override uint NumBytes => MachineSize;

        public override string DataTypeName => "isize";

        public IntegralType NextLargestType()
        {
            var size = MachineSize * 2;
            return IntegralTypeBySize(size, true);
        }
    }

    public class UnsignedSizeType : IntegralType
    {
        public static uint MachineSize => SignedSizeType.MachineSize;
        public readonly static UnsignedSizeType Instance = new UnsignedSizeType();

        public override uint NumBytes => MachineSize;
        public override string DataTypeName => "usize";
        public IntegralType NextLargestType()
        {
            var size = MachineSize * 2;
            return IntegralTypeBySize(size, false);
        }
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
                var list = string.Join(",", ArgumentTypes.Select(a => a.DataTypeName));
                return $"fn({list}) : {ReturnType.DataTypeName}";
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
        bool IsVariable { get; set; }
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
        public override bool Concrete => false;
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

        public override string DataTypeName => VariantName;
    }

    public class VariantMemberType : AilurusDataType
    {
        public string MemberName { get; set; }
        public AilurusDataType InnerType { get; set; }
        public int MemberIndex { get; set; }
    }

    public class EmptyVariantMemberType : AilurusDataType
    {
        public static readonly EmptyVariantMemberType Instance = new EmptyVariantMemberType();
    }
}
