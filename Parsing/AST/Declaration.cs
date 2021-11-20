using AilurusLang.DataType;

namespace AilurusLang.Parsing.AST
{
    public abstract class Declaration { }

    public class TypeDeclaration : Declaration
    {
        public AilurusDataType Type { get; set; }
        public string TypeName { get; set; }
    }

    public class VariableDeclaration : Declaration
    {
        public AilurusDataType Type { get; set; }
        public string Name { get; set; }
        public bool IsMutable { get; set; }
        public bool IsInitialized { get; set; }
    }

    public class FunctionDeclaration : Declaration
    {
        public string FunctionName { get; set; }
        public FunctionType FunctionType { get; set; }
    }
}