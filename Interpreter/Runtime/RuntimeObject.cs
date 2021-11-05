using System.Collections.Generic;
using AilurusLang.DataType;

namespace AilurusLang.Interpreter.Runtime
{
    public abstract class AilurusValue
    {
    }

    public class SimpleValue : AilurusValue
    {
        public object Value { get; set; }
    }

    public class StructInstance
    {
        StructType StructType { get; set; }
        List<AilurusValue> Members { get; set; }
    }
}