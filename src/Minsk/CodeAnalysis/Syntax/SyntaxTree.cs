using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        private delegate void ParseHandler(SyntaxTree syntaxTree,
                                           out CompilationUnitSyntax root,
                                           out ImmutableArray<Diagnostic> diagnostics);

        private SyntaxTree(SourceText text, ParseHandler handler)
        {
            Text = text;

            handler(this, out var root, out var diagnostics);

            Diagnostics = diagnostics;
            Root = root;
        }

        public SourceText Text { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        public static SyntaxTree Load(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var sourceText = SourceText.From(text, fileName);
            return Parse(sourceText);
        }

        private static void Parse(SyntaxTree syntaxTree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics)
        {
            var parser = new Parser(syntaxTree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }

        public static SyntaxTree Parse(string text)
        {
            var sourceText = SourceText.From(text);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text, Parse);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, bool includeEndOfFile = false)
        {
            var sourceText = SourceText.From(text);
            return ParseTokens(sourceText, includeEndOfFile);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
        {
            var sourceText = SourceText.From(text);
            return ParseTokens(sourceText, out diagnostics, includeEndOfFile);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, bool includeEndOfFile = false)
        {
            return ParseTokens(text, out _, includeEndOfFile);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
        {
            var tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree st, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> d)
            {
                var l = new Lexer(st);
                while (true)
                {
                    var token = l.Lex();

                    if (token.Kind != SyntaxKind.EndOfFileToken || includeEndOfFile)
                        tokens.Add(token);

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        root = new CompilationUnitSyntax(st, ImmutableArray<MemberSyntax>.Empty, token);
                        break;
                    }
                }

                d = l.Diagnostics.ToImmutableArray();
            }

            var syntaxTree = new SyntaxTree(text, ParseTokens);
            diagnostics = syntaxTree.Diagnostics.ToImmutableArray();
            return tokens.ToImmutableArray();
        }
    }
}