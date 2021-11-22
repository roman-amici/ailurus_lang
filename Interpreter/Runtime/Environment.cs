using System.Collections.Generic;
using AilurusLang.Parsing.AST;

namespace AilurusLang.Interpreter.Runtime
{

    public class TreeWalkerEnvironment
    {
        Dictionary<Declaration, AilurusValue> Values { get; set; } = new Dictionary<Declaration, AilurusValue>();

        public AilurusValue GetValue(Declaration declaration)
        {
            return Values[declaration];
        }

        public void SetValue(Declaration declaration, AilurusValue value)
        {
            Values[declaration] = value;
        }

    }
}