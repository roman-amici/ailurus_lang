using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class StandardScope
    {
        public Dictionary<string, TypeDeclaration> TypeDeclarations { get; set; }

        public StandardScope()
        {
            TypeDeclarations = new Dictionary<string, TypeDeclaration>
            {
                { "bool", new TypeDeclaration() { TypeName = "bool", Type = BooleanType.Instance } },
                { "byte", new TypeDeclaration() { TypeName = "byte", Type = ByteType.InstanceSigned } },
                { "ubyte", new TypeDeclaration() { TypeName = "ubyte", Type = ByteType.InstanceUnsigned } },
                { "short", new TypeDeclaration() { TypeName = "short", Type = ShortType.InstanceSigned } },
                { "ushort", new TypeDeclaration() { TypeName = "ushort", Type = ShortType.InstanceUnsigned } },
                { "int", new TypeDeclaration() { TypeName = "int", Type = IntType.InstanceSigned } },
                { "uint", new TypeDeclaration() { TypeName = "uint", Type = IntType.InstanceUnsigned } },
                { "float", new TypeDeclaration() { TypeName = "float", Type = FloatType.Instance } },
                { "double", new TypeDeclaration() { TypeName = "double", Type = DoubleType.Instance } }
            };
        }
    }

    public class ModuleScope
    {
        public Dictionary<string, TypeDeclaration> TypeDeclarations { get; set; } = new Dictionary<string, TypeDeclaration>();
        public Dictionary<string, VariableDeclaration> VariableDeclarations { get; set; } = new Dictionary<string, VariableDeclaration>();
        public Dictionary<string, FunctionDeclaration> FunctionDeclarations { get; set; } = new Dictionary<string, FunctionDeclaration>();
    }

    public class BlockScope
    {
        public Dictionary<string, VariableDeclaration> VariableDeclarations { get; set; } = new Dictionary<string, VariableDeclaration>();
    }
}