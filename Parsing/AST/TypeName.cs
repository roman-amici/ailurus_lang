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
        public static readonly TypeName Int = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("int")) };
        public static readonly TypeName Byte = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("byte")) };
        public static readonly TypeName Short = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("short")) };
        public static readonly TypeName Ushort = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("ubyte")) };
        public static readonly TypeName UByte = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("ushort")) };
        public static readonly TypeName Uint = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("uint")) };
        public static readonly TypeName Float = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("float")) };
        public static readonly TypeName Double = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("double")) };
        public static readonly TypeName StringName = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("string")) };
        public static readonly TypeName Char = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("char")) };
        public static readonly TypeName Error = new BaseTypeName() { Name = new QualifiedName(Token.StaticIdentifier("error")) };
    }
}