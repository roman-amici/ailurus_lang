using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class StandardScope
    {
        public Dictionary<string, BaseTypeDeclaration> TypeDeclarations { get; set; }

        public StandardScope()
        {
            TypeDeclarations = new Dictionary<string, BaseTypeDeclaration>
            {
                { "bool", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Bool, DataType = BooleanType.Instance } },
                { "byte", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Int, DataType = ByteType.InstanceSigned } },
                { "ubyte", new BaseTypeDeclaration() { TypeName = StandardTypeNames.UByte, DataType = ByteType.InstanceUnsigned } },
                { "short", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Short, DataType = ShortType.InstanceSigned } },
                { "ushort", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Ushort, DataType = ShortType.InstanceUnsigned } },
                { "int", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Int, DataType = IntType.InstanceSigned } },
                { "uint", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Uint, DataType = IntType.InstanceUnsigned } },
                { "float", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Float, DataType = FloatType.Instance } },
                { "double", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Double, DataType = DoubleType.Instance } },
                { "void", new BaseTypeDeclaration() {TypeName = StandardTypeNames.Void, DataType = VoidType.Instance}},
                { "string", new BaseTypeDeclaration() {TypeName = StandardTypeNames.StringName, DataType = StringType.Instance}},
                { "char", new BaseTypeDeclaration() {TypeName = StandardTypeNames.Char, DataType= CharType.Instance}},
            };
        }
    }

    public class ModuleScope
    {
        public Dictionary<string, TypeDeclaration> TypeDeclarations { get; set; } = new Dictionary<string, TypeDeclaration>();
        public Dictionary<string, VariableResolution> VariableResolutions { get; set; } = new Dictionary<string, VariableResolution>();
        public Dictionary<string, FunctionResolution> FunctionResolutions { get; set; } = new Dictionary<string, FunctionResolution>();
    }

    public class BlockScope
    {
        public Dictionary<string, VariableResolution> VariableResolutions { get; set; } = new Dictionary<string, VariableResolution>();
    }
}