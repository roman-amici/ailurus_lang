using System;
using System.IO;
using AilurusLang.Interpreter.TreeWalker;
using AilurusLang.Interpreter.TreeWalker.Evaluators;
using AilurusLang.Parsing.AST;
using AilurusLang.Parsing.Parsers;
using AilurusLang.Scanning.BasicScanner;
using AilurusLang.StaticAnalysis.TypeChecking;

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
            var resolver = new Resolver();
            var treeWalker = new TreeWalker(new DynamicValueEvaluator());

            var source = File.ReadAllText(fileName);

            var tokens = scanner.Scan(source, fileName);
            var module = parser.Parse(tokens);
            if (parser.IsValid && module != null)
            {
                resolver.ResolveModule(module);
                if (!resolver.HadError)
                {
                    treeWalker.EvalModule(module);
                }
            }
        }
    }
}
