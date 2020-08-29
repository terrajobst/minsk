using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Syntax;
using Minsk.IO;
using Mono.Options;

namespace Minsk
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string? outputPath = (string?)null;
            string? moduleName = (string?)null;
            List<string>? referencePaths = new List<string>();
            List<string>? sourcePaths = new List<string>();
            bool helpRequested = false;

            OptionSet? options = new OptionSet
            {
                "usage: msc <source-paths> [options]",
                { "r=", "The {path} of an assembly to reference", v => referencePaths.Add(v) },
                { "o=", "The output {path} of the assembly to create", v => outputPath = v },
                { "m=", "The {name} of the module", v => moduleName = v },
                { "?|h|help", "Prints help", v => helpRequested = true },
                { "<>", v => sourcePaths.Add(v) }
            };

            options.Parse(args);

            if (helpRequested)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (sourcePaths.Count == 0)
            {
                Console.Error.WriteLine("error: need at least one source file");
                return 1;
            }

            if (outputPath == null)
            {
                outputPath = Path.ChangeExtension(sourcePaths[0], ".exe");
            }

            if (moduleName == null)
            {
                moduleName = Path.GetFileNameWithoutExtension(outputPath);
            }

            List<SyntaxTree>? syntaxTrees = new List<SyntaxTree>();
            bool hasErrors = false;

            foreach (string? path in sourcePaths)
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: file '{path}' doesn't exist");
                    hasErrors = true;
                    continue;
                }

                SyntaxTree? syntaxTree = SyntaxTree.Load(path);
                syntaxTrees.Add(syntaxTree);
            }

            foreach (string? path in referencePaths)
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: file '{path}' doesn't exist");
                    hasErrors = true;
                    continue;
                }
            }

            if (hasErrors)
            {
                return 1;
            }

            Compilation? compilation = Compilation.Create(syntaxTrees.ToArray());
            System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = compilation.Emit(moduleName, referencePaths.ToArray(), outputPath);

            if (diagnostics.Any())
            {
                Console.Error.WriteDiagnostics(diagnostics);
                return 1;
            }

            return 0;
        }
    }
}
