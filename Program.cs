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
            var statements = parser.Parse(tokens);
            if (parser.IsValid)
            {
                resolver.ResolveStatements(statements);
                if (!resolver.HadError)
                {
                    foreach (var stmt in statements)
                    {
                        if (stmt is ExpressionStatement e)
                        {
                            var value = treeWalker.EvalExpression(e.Expr);
                            Console.WriteLine($"{value} : {e.Expr.DataType.DataTypeName}");
                        }
                    }
                }
            }
        }
    }
}
