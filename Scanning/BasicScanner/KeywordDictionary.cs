using System.Collections.Generic;

namespace AilurusLang.Scanning.BasicScanner
{
    public class KeywordDictionary : Dictionary<string, TokenType>
    {
        public KeywordDictionary() : base()
        {
            // Operators
            this.Add("mod", TokenType.Mod);

            // Control Structures
            this.Add("if", TokenType.If);
            this.Add("else", TokenType.Else);
            this.Add("while", TokenType.While);
            this.Add("for", TokenType.For);
            this.Add("do", TokenType.Do);
            this.Add("match", TokenType.Match);
            this.Add("case", TokenType.Case);
            this.Add("break", TokenType.Break);
            this.Add("continue", TokenType.Continue);
            this.Add("return", TokenType.Return);

            // Values
            this.Add("null", TokenType.Null);
            this.Add("true", TokenType.True);
            this.Add("false", TokenType.False);

            // Declarations
            this.Add("struct", TokenType.Struct);
            this.Add("type", TokenType.Type);
            this.Add("fn", TokenType.Fn);
            this.Add("variant", TokenType.Variant);
            this.Add("let", TokenType.Let);

            // Modifiers
            this.Add("static", TokenType.Static);
            this.Add("volatile", TokenType.Volatile);
            this.Add("mut", TokenType.Mut);
            this.Add("ptr", TokenType.Ptr);

            // Modules
            this.Add("module", TokenType.Module);
            this.Add("export", TokenType.Export);
            this.Add("import", TokenType.Import);
            this.Add("from", TokenType.From);

            // Misc
            this.Add("sizeOf", TokenType.SizeOf);
            this.Add("ptrTo", TokenType.PtrTo);

            // Reserved Types
            this.Add("byte", TokenType.Byte);
            this.Add("ubyte", TokenType.UByte);
            this.Add("short", TokenType.Short);
            this.Add("ushort", TokenType.UShort);
            this.Add("int", TokenType.Int);
            this.Add("uint", TokenType.UInt);
            this.Add("long", TokenType.Long);
            this.Add("ulong", TokenType.ULong);
            this.Add("float", TokenType.Float);
            this.Add("double", TokenType.Double);
            this.Add("bool", TokenType.Bool);
        }
    }
}