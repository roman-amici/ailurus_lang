using System.Collections.Generic;
using AilurusLang.Scanning;

namespace AilurusLang.Parsing.AST
{
    public enum NodeType
    {
        Expression,
        Statement,
        Declaration
    }

    public abstract class ASTNode
    {
        public Token SourceStart { get; set; }
        public NodeType NodeType { get; set; }
    }

    public class StandardTypeNames
    {
        public static readonly TypeName Void = new BaseTypeName() { Name = Token.StaticIdentifier("void") };
        public static readonly TypeName Bool = new BaseTypeName() { Name = Token.StaticIdentifier("bool") };
        public static readonly TypeName Int = new BaseTypeName() { Name = Token.StaticIdentifier("int") };
        public static readonly TypeName Byte = new BaseTypeName() { Name = Token.StaticIdentifier("byte") };
        public static readonly TypeName Short = new BaseTypeName() { Name = Token.StaticIdentifier("short") };
        public static readonly TypeName Ushort = new BaseTypeName() { Name = Token.StaticIdentifier("ubyte") };
        public static readonly TypeName UByte = new BaseTypeName() { Name = Token.StaticIdentifier("ushort") };
        public static readonly TypeName Uint = new BaseTypeName() { Name = Token.StaticIdentifier("uint") };
        public static readonly TypeName Float = new BaseTypeName() { Name = Token.StaticIdentifier("float") };
        public static readonly TypeName Double = new BaseTypeName() { Name = Token.StaticIdentifier("double") };
    }

    public abstract class TypeName
    {
        public virtual Token Name { get; set; }
    }

    public class BaseTypeName : TypeName { }

    public class ArrayTypeName : TypeName
    {
        public override Token Name
        {
            get => BaseTypeName.Name;
        }
        public TypeName BaseTypeName { get; set; }
    }

    public class PointerTypeName : TypeName
    {
        public override Token Name
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
}