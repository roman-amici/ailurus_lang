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

        public static RuntimeError BinaryOperatorError(
            AilurusValue left,
            AilurusValue right,
            Token t)
        {
            return new RuntimeError(
                $"Unable to apply operator '{t.Lexeme}' to operands of type '{left.TypeName}' and '{right.TypeName}'",
                t);
        }

        public static RuntimeError BinaryOperatorErrorFirstOp(
            AilurusValue left,
            Token t)
        {
            return new RuntimeError(
                $"Unable to apply operator '{t.Lexeme}' to operand of type '{left.TypeName}",
                t);
        }

        public static RuntimeError UnaryError(AilurusValue inner, Token t)
        {
            return new RuntimeError(
                $"Unable to apply operator '{t.Lexeme}' to operand of type '{inner.TypeName}'", t);
        }

    }
}