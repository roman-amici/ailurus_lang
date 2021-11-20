using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class StandardScope
    {
        public Dictionary<string, Declaration> TypeDeclarations { get; set; }

        public StandardScope()
        {
            TypeDeclarations = new Dictionary<string, Declaration>();
            TypeDeclarations.Add("bool", new TypeDeclaration() { TypeName = "bool", Type = BooleanType.Instance });
            TypeDeclarations.Add("byte", new TypeDeclaration() { TypeName = "byte", Type = ByteType.InstanceSigned });
            TypeDeclarations.Add("ubyte", new TypeDeclaration() { TypeName = "ubyte", Type = ByteType.InstanceUnsigned });
            TypeDeclarations.Add("short", new TypeDeclaration() { TypeName = "short", Type = ShortType.InstanceSigned });
            TypeDeclarations.Add("ushort", new TypeDeclaration() { TypeName = "ushort", Type = ShortType.InstanceUnsigned });
            TypeDeclarations.Add("int", new TypeDeclaration() { TypeName = "int", Type = IntType.InstanceSigned });
            TypeDeclarations.Add("uint", new TypeDeclaration() { TypeName = "uint", Type = IntType.InstanceUnsigned });
            TypeDeclarations.Add("float", new TypeDeclaration() { TypeName = "float", Type = FloatType.Instance });
            TypeDeclarations.Add("double", new TypeDeclaration() { TypeName = "double", Type = DoubleType.Instance });
        }
    }

    public class ModuleScope
    {
        public Dictionary<string, TypeDeclaration> TypeDeclarations { get; set; } = new Dictionary<string, TypeDeclaration>();
        public Dictionary<string, VariableDeclaration> VariableDeclarations { get; set; } = new Dictionary<string, VariableDeclaration>();
    }

    public class BlockScope
    {
        public Dictionary<string, VariableDeclaration> VariableDeclarations { get; set; } = new Dictionary<string, VariableDeclaration>();
    }
}