using System.Collections.Generic;

namespace AilurusLang.StaticAnalysis.TypeChecking
{
    public class ReservedWords : HashSet<string>
    {
        public ReservedWords() : base()
        {
            // Operators
            Add("mod");

            // Control Structures
            Add("if");
            Add("then");
            Add("else");
            Add("while");
            Add("for");
            Add("do");
            Add("match");
            Add("case");
            Add("break");
            Add("continue");
            Add("return");
            Add("foreach");
            Add("in");

            // Values
            Add("null");
            Add("true");
            Add("false");

            // Declarations
            Add("struct");
            Add("type");
            Add("fn");
            Add("variant");
            Add("let");

            // Expressions
            Add("new");
            Add("free");

            // Modifiers
            Add("static");
            Add("volatile");
            Add("mut");
            Add("ptr");
            Add("var");
            Add("varptr");

            // Modules
            Add("module");
            Add("export");
            Add("import");

            // Misc
            Add("sizeOf");
            Add("addrOf");
            Add("varAddrOf");

            // Array
            Add("lenOf");

            //Temp
            Add("print");

            // Reserved Types
            Add("byte");
            Add("ubyte");
            Add("short");
            Add("ushort");
            Add("int");
            Add("uint");
            Add("long");
            Add("ulong");
            Add("float");
            Add("double");
            Add("bool");
            Add("string");
            Add("char");
        }
    }
}