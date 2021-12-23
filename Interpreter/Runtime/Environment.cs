using System.Collections.Generic;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.Runtime
{

    public class TreeWalkerEnvironment
    {
        Dictionary<Resolution, MemoryLocation> Values { get; set; } = new Dictionary<Resolution, MemoryLocation>();

        public AilurusValue GetValue(Resolution resolution)
        {
            return Values[resolution].Value;
        }

        public void SetValue(Resolution resolution, AilurusValue value)
        {
            if (Values.ContainsKey(resolution))
            {
                Values[resolution].Value = value;
            }
            else
            {
                Values[resolution] = new MemoryLocation() { Value = value };
            }
        }

        public MemoryLocation GetAddress(Resolution resolution)
        {
            return Values[resolution];
        }

        public void MarkInvalid()
        {
            foreach (var v in Values.Values)
            {
                v.MarkInvalid();
            }
        }
    }
}