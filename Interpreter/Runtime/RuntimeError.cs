using System;
using AilurusLang.Scanning;

namespace AilurusLang.Interpreter.Runtime
{
    public class RuntimeError : Exception
    {
        public long? Line { get; set; }
        public long? Column { get; set; }
        public string? SourceFile { get; set; }

        public RuntimeError(string message, Token t) : this(message, t.Line, t.Column, t.SourceFile) { }
        public RuntimeError(string message, long line, long column, string sourceFile) : base(message)
        {
            Line = line;
            Column = column;
            SourceFile = sourceFile;
        }

        public override string ToString()
        {
            return $"{SourceFile}:{Line}:{Column} - {Message}";
        }
    }
}