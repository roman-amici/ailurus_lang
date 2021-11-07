using System;
using System.IO;
using AilurusLang.Interpreter.TreeWalker;
using AilurusLang.Interpreter.TreeWalker.Evaluators;
using AilurusLang.Parsing.Parsers;
using AilurusLang.Scanning.BasicScanner;

namespace AilurusLang
{
    class Ailurus
    {
        static void Main(string[] args)
        {
            Console.WriteLine(args[0]);
            if (args.Length < 1)
            {
                Console.WriteLine("Usage [file]");
                return;
            }

            var filename = args[0];
            Lex(filename);
        }

        static void Lex(string fileName)
        {
            var scanner = new Scanner();
            var parser = new RecursiveDescentParser();
            var treeWalker = new TreeWalker(new DynamicValueEvaluator());

            var source = File.ReadAllText(fileName);

            var tokens = scanner.Scan(source, fileName);
            var expr = parser.ParseExpression(tokens);

            var value = treeWalker.EvalExpression(expr);

            Console.WriteLine(value);
        }
    }
}
