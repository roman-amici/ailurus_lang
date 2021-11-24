using AilurusLang.DataType;

namespace AilurusLang.Parsing.AST
{
    public abstract class Resolution { }

    public class VariableResolution : Resolution
    {
        public string Name { get; set; }
        public bool IsMutable { get; set; }
        public bool IsInitialized { get; set; }
        public int? ScopeDepth { get; set; } = null;
        public AilurusDataType DataType { get; set; }
    }

    public class FunctionResolution : Resolution
    {
        public FunctionDeclaration Declaration { get; set; }
    }
}