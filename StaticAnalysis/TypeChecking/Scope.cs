using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;
using AilurusLang.Scanning;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class StandardScope
    {
        public Dictionary<string, BaseTypeDeclaration> TypeDeclarations { get; set; }

        public StandardScope()
        {
            TypeDeclarations = new Dictionary<string, BaseTypeDeclaration>
            {
                { "bool", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Bool, DataType = BooleanType.Instance } },
                { "byte", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Int, DataType = ByteType.InstanceSigned } },
                { "ubyte", new BaseTypeDeclaration() { TypeName = BaseTypeNames.UByte, DataType = ByteType.InstanceUnsigned } },
                { "short", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Short, DataType = ShortType.InstanceSigned } },
                { "ushort", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Ushort, DataType = ShortType.InstanceUnsigned } },
                { "int", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Int, DataType = IntType.InstanceSigned } },
                { "uint", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Uint, DataType = IntType.InstanceUnsigned } },
                { "float", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Float, DataType = FloatType.Instance } },
                { "double", new BaseTypeDeclaration() { TypeName = BaseTypeNames.Double, DataType = DoubleType.Instance } }
            };
        }
    }

    public class ModuleScope
    {
        public Dictionary<string, TypeDeclaration> TypeDeclarations { get; set; } = new Dictionary<string, TypeDeclaration>();
        public Dictionary<string, VariableDefinition> VariableDefinitions { get; set; } = new Dictionary<string, VariableDefinition>();
        public Dictionary<string, FunctionDefinition> FunctionDefinitions { get; set; } = new Dictionary<string, FunctionDefinition>();
    }

    public class BlockScope
    {
        public Dictionary<string, VariableDefinition> VariableDefinitions { get; set; } = new Dictionary<string, VariableDefinition>();
    }
}