using AilurusLang.Interpreter.Runtime;
using AilurusLang.Scanning;

namespace AilurusLang.Interpreter.TreeWalker
{
    public enum ControlFlowType
    {
        Break,
        Continue,
        Return,
    }

    public class ControlFlowException : System.Exception
    {
        public AilurusValue ReturnValue { get; set; }
        public ControlFlowType ControlFlowType { get; set; }

        public ControlFlowException(ControlFlowType type)
        {
            ControlFlowType = type;
        }
    }
}