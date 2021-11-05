using System;
using System.IO;
using AilurusLang.Scanning.BasicScanner;

namespace AilurusLang
{
    class Ailurus
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage [file]");
                return;
            }

            var filename = args[1];
            Lex(filename);
        }

        static void Lex(string fileName)
        {
            var scanner = new Scanner();

            var source = File.ReadAllText(fileName);

            var tokens = scanner.Scan(source, fileName);
            foreach (var token in tokens)
            {
                Console.WriteLine($"{token}");
            }
        }
    }
}
