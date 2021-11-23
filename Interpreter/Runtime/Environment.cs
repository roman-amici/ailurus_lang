using System.Collections.Generic;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.Runtime
{

    public class TreeWalkerEnvironment
    {
        Dictionary<Definition, AilurusValue> Values { get; set; } = new Dictionary<Definition, AilurusValue>();

        public AilurusValue GetValue(Definition definition)
        {
            return Values[definition];
        }

        public void SetValue(Definition definition, AilurusValue value)
        {
            Values[definition] = value;
        }

    }
}