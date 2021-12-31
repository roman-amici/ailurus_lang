using System.Collections.Generic;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{
    public abstract class TypeName
    {
        public virtual QualifiedName Name { get; set; }
    }

    public class BaseTypeName : TypeName
    {
        // Hack for var strings
        public bool VarModifier { get; set; }
    }

    public class ArrayTypeName : TypeName
    {
        public override QualifiedName Name
        {
            get => BaseTypeName.Name;
        }
        public TypeName BaseTypeName { get; set; }
        public bool IsVariable { get; set; }
    }

    public class TupleTypeName : TypeName
    {
        public List<TypeName> ElementTypeNames { get; set; }
    }

    public class PointerTypeName : TypeName
    {
        public override QualifiedName Name
        {
            get => BaseTypeName.Name;
        }
        public bool IsVariable { get; set; }
        public TypeName BaseTypeName { get; set; }
    }

    public struct FunctionArgumentDeclaration
    {
        public Token Name { get; set; }
        public TypeName TypeName { get; set; }
        public bool IsMutable { get; set; }
    }

    public class StandardTypeNames
    {
        public static readonly TypeName Void = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("void")) };
        public static readonly TypeName Bool = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("bool")) };
        public static readonly TypeName I32 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("i32")) };
        public static readonly TypeName I8 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("i8")) };
        public static readonly TypeName I16 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("i16")) };
        public static readonly TypeName U16 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("u16")) };
        public static readonly TypeName U8 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("u8")) };
        public static readonly TypeName U32 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("u32")) };
        public static readonly TypeName F32 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("f32")) };
        public static readonly TypeName F64 = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("f64")) };
        public static readonly TypeName USize = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("usize")) };
        public static readonly TypeName ISize = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("isize")) };
        public static readonly TypeName StringName = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("string")) };
        public static readonly TypeName Char = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("char")) };
        public static readonly TypeName Error = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("error")) };
    }
}