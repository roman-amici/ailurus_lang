using System.Collections.Generic;
using AilurusLang.DataType;
using AilurusLang.Parsing.AST;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class StandardScope
    {
        public Dictionary<string, BaseTypeDeclaration> TypeDeclarations { get; set; }

        public static BaseTypeDeclaration ErrorDeclaration = new BaseTypeDeclaration() { TypeName = StandardTypeNames.Error, DataType = ErrorType.Instance };

        public StandardScope()
        {
            TypeDeclarations = new Dictionary<string, BaseTypeDeclaration>
            {
                { "bool", new BaseTypeDeclaration() { TypeName = StandardTypeNames.Bool, DataType = BooleanType.Instance } },
                { "i8", new BaseTypeDeclaration() { TypeName = StandardTypeNames.I8, DataType = Signed8Type.Instance } },
                { "u8", new BaseTypeDeclaration() { TypeName = StandardTypeNames.U8, DataType = Signed8Type.Instance } },
                { "i16", new BaseTypeDeclaration() { TypeName = StandardTypeNames.I16, DataType = Signed16Type.Instance } },
                { "u16", new BaseTypeDeclaration() { TypeName = StandardTypeNames.U16, DataType = Unsigned16Type.Instance } },
                { "i32", new BaseTypeDeclaration() { TypeName = StandardTypeNames.I32, DataType = Signed32Type.Instance } },
                { "u32", new BaseTypeDeclaration() { TypeName = StandardTypeNames.U32, DataType = Unsigned32Type.Instance } },
                { "isize", new BaseTypeDeclaration() { TypeName = StandardTypeNames.ISize, DataType = SignedSizeType.Instance } },
                { "usize", new BaseTypeDeclaration() { TypeName = StandardTypeNames.USize, DataType = UnsignedSizeType.Instance } },
                { "f32", new BaseTypeDeclaration() { TypeName = StandardTypeNames.F32, DataType = Float32Type.Instance } },
                { "f64", new BaseTypeDeclaration() { TypeName = StandardTypeNames.F64, DataType = Float64Type.Instance } },
                { "void", new BaseTypeDeclaration() {TypeName = StandardTypeNames.Void, DataType = VoidType.Instance}},
                { "string", new BaseTypeDeclaration() {TypeName = StandardTypeNames.StringName, DataType = StringType.Instance}},
                { "char", new BaseTypeDeclaration() {TypeName = StandardTypeNames.Char, DataType = CharType.Instance}},
            };
        }
    }

    public class ModuleScope
    {
        public Dictionary<string, ModuleScope> SubmoduleScopes { get; set; } = new Dictionary<string, ModuleScope>();

        public Dictionary<string, TypeDeclaration> TypeDeclarations { get; set; } = new Dictionary<string, TypeDeclaration>();
        public Dictionary<string, VariableResolution> VariableResolutions { get; set; } = new Dictionary<string, VariableResolution>();
        public Dictionary<string, FunctionResolution> FunctionResolutions { get; set; } = new Dictionary<string, FunctionResolution>();
    }

    public class BlockScope
    {
        public Dictionary<string, VariableResolution> VariableResolutions { get; set; } = new Dictionary<string, VariableResolution>();
    }
}