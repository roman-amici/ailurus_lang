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

    public class BaseTypeNames
    {
        public static readonly TypeName Void = new TypeName() { Name = Token.StaticIdentifier("void") };
        public static readonly TypeName Bool = new TypeName() { Name = Token.StaticIdentifier("bool") };
        public static readonly TypeName Int = new TypeName() { Name = Token.StaticIdentifier("int") };
        public static readonly TypeName Byte = new TypeName() { Name = Token.StaticIdentifier("byte") };
        public static readonly TypeName Short = new TypeName() { Name = Token.StaticIdentifier("short") };
        public static readonly TypeName Ushort = new TypeName() { Name = Token.StaticIdentifier("ubyte") };
        public static readonly TypeName UByte = new TypeName() { Name = Token.StaticIdentifier("ushort") };
        public static readonly TypeName Uint = new TypeName() { Name = Token.StaticIdentifier("uint") };
        public static readonly TypeName Float = new TypeName() { Name = Token.StaticIdentifier("flaot") };
        public static readonly TypeName Double = new TypeName() { Name = Token.StaticIdentifier("double") };
    }

    public class TypeName
    {
        public Token Name { get; set; }
        public bool IsPtr { get; set; }
    }
}