using System;

namespace AilurusLang.Scanning.BasicScanner
{
    public class SyntaxError : Exception
    {
        public long Line { get; set; }
        public long Column { get; set; }

        public string SourceFile { get; set; }

        public SyntaxError(string message, string sourceFile, long line, long column) : base(message)
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