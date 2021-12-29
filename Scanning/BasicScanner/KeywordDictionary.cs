using System.Collections.Generic;

namespace AilurusLang.Scanning.BasicScanner
{
    public class KeywordDictionary : Dictionary<string, TokenType>
    {
        public KeywordDictionary() : base()
        {
            // Operators
            Add("mod", TokenType.Mod);

            // Control Structures
            Add("if", TokenType.If);
            Add("then", TokenType.Then);
            Add("else", TokenType.Else);
            Add("while", TokenType.While);
            Add("for", TokenType.For);
            Add("do", TokenType.Do);
            Add("match", TokenType.Match);
            Add("case", TokenType.Case);
            Add("break", TokenType.Break);
            Add("continue", TokenType.Continue);
            Add("return", TokenType.Return);
            Add("foreach", TokenType.ForEach);
            Add("in", TokenType.In);

            // Values
            Add("null", TokenType.Null);
            Add("true", TokenType.True);
            Add("false", TokenType.False);

            // Declarations
            Add("struct", TokenType.Struct);
            Add("type", TokenType.Type);
            Add("fn", TokenType.Fn);
            Add("variant", TokenType.Variant);
            Add("let", TokenType.Let);

            // Modifiers
            Add("static", TokenType.Static);
            Add("volatile", TokenType.Volatile);
            Add("mut", TokenType.Mut);
            Add("ptr", TokenType.Ptr);
            Add("varptr", TokenType.VarPtr);
            Add("var", TokenType.Var);

            // Modules
            Add("submodule", TokenType.Submodule);
            Add("export", TokenType.Export);
            Add("import", TokenType.Import);

            // Array Operators
            Add("lenOf", TokenType.LenOf);

            // Type Casting
            Add("is", TokenType.Is);
            Add("as", TokenType.As);

            // Memory Operators
            Add("sizeOf", TokenType.SizeOf);
            Add("addrOf", TokenType.AddrOf);
            Add("varAddrOf", TokenType.VarAddrOf);
            Add("free", TokenType.Free);
            Add("new", TokenType.New);

            //Temp
            Add("print", TokenType.DebugPrint);
        }
    }
}