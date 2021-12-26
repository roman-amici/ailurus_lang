using System;
using System.Collections.Generic;
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
        public static Scanner Scanner { get; set; }
        public static RecursiveDescentParser Parser { get; set; }
        public static Resolver Resolver { get; set; }
        public static TreeWalker TreeWalker { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine(args[0]);
            if (args.Length < 1)
            {
                Console.WriteLine("Usage [file]");
                return;
            }

            var filename = args[0];
            Run(filename);
        }

        static Module ParseModule(string fileName, List<string> modulePath)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Could not find file {fileName}.");
                Environment.Exit(1);
            }

            var source = File.ReadAllText(fileName);

            var tokens = Scanner.Scan(source, fileName);
            var module = Parser.Parse(tokens);

            module.Path = modulePath;

            if (!Parser.IsValid)
            {
                return null;
            }

            var submodules = new List<Module>();
            foreach (var submoduleDeclaration in module.SubmoduleDeclarations)
            {
                var submoduleFileName = submoduleDeclaration.FilePath(fileName);

                // Append the subpath to the path list
                var subpath = new List<string>(modulePath)
                {
                    submoduleDeclaration.Name.Identifier
                };

                var submodule = ParseModule(submoduleFileName, subpath);
                if (submodule == null)
                {
                    return null;
                }
            }

            module.Submodules = submodules;
            return module;
        }

        static void Run(string rootFileName)
        {
            Scanner = new Scanner();
            Parser = new RecursiveDescentParser();
            Resolver = new Resolver();
            TreeWalker = new TreeWalker(new DynamicValueEvaluator());

            Module rootModule = ParseModule(rootFileName, new List<string>());

            int exitCode = 0;
            if (rootModule != null)
            {
                Resolver.ResolveRootModule(rootModule);
                if (!Resolver.HadError)
                {
                    exitCode = TreeWalker.EvalRootModule(rootModule);
                }
            }

            Environment.Exit(exitCode);
        }
    }
}
