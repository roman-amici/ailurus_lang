using System.Collections.Generic;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.Runtime
{

    public class TreeWalkerEnvironment
    {
        Dictionary<Resolution, AilurusValue> Values { get; set; } = new Dictionary<Resolution, AilurusValue>();

        public bool IsValid { get; set; } = true;

        public AilurusValue GetValue(Resolution resolution)
        {
            return Values[resolution];
        }

        public void SetValue(Resolution resolution, AilurusValue value)
        {
            Values[resolution] = value;
        }
    }
}