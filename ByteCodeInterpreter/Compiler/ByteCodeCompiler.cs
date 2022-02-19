using System.Collections.Generic;

namespace AilurusLang.ByteCodeInterpreter.Compiler
{
    public class Compiler
    {
        List<AilurusValue> Constants { get; set; }
        List<ByteCodeOp> Ops { get; set; }
    }
}