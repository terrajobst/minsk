using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        private Dictionary<SyntaxNode, SyntaxNode?>? _parents;

        private delegate void ParseHandler(SyntaxTree syntaxTree,
                                           out CompilationUnitSyntax root,
                                           out ImmutableArray<Diagnostic> diagnostics);

        private SyntaxTree(SourceText text, ParseHandler handler)
        {
            Text = text;

            handler(this, out CompilationUnitSyntax? root, out ImmutableArray<Diagnostic> diagnostics);

            Diagnostics = diagnostics;
            Root = root;
        }

        public SourceText Text { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        public static SyntaxTree Load(string fileName)
        {
            string? text = File.ReadAllText(fileName);
            SourceText? sourceText = SourceText.From(text, fileName);
            return Parse(sourceText);
        }

        private static void Parse(SyntaxTree syntaxTree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics)
        {
            Parser? parser = new Parser(syntaxTree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }

        public static SyntaxTree Parse(string text)
        {
            SourceText? sourceText = SourceText.From(text);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text, Parse);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, bool includeEndOfFile = false)
        {
            SourceText? sourceText = SourceText.From(text);
            return ParseTokens(sourceText, includeEndOfFile);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
        {
            SourceText? sourceText = SourceText.From(text);
            return ParseTokens(sourceText, out diagnostics, includeEndOfFile);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, bool includeEndOfFile = false)
        {
            return ParseTokens(text, out _, includeEndOfFile);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
        {
            List<SyntaxToken>? tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree st, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> d)
            {
                Lexer? l = new Lexer(st);
                while (true)
                {
                    SyntaxToken? token = l.Lex();

                    if (token.Kind != SyntaxKind.EndOfFileToken || includeEndOfFile)
                    {
                        tokens.Add(token);
                    }

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        root = new CompilationUnitSyntax(st, ImmutableArray<MemberSyntax>.Empty, token);
                        break;
                    }
                }

                d = l.Diagnostics.ToImmutableArray();
            }

            SyntaxTree? syntaxTree = new SyntaxTree(text, ParseTokens);
            diagnostics = syntaxTree.Diagnostics;
            return tokens.ToImmutableArray();
        }

        internal SyntaxNode? GetParent(SyntaxNode syntaxNode)
        {
            if (_parents == null)
            {
                Dictionary<SyntaxNode, SyntaxNode?>? parents = CreateParentsDictionary(Root);
                Interlocked.CompareExchange(ref _parents, parents, null);
            }

            return _parents[syntaxNode];
        }

        private Dictionary<SyntaxNode, SyntaxNode?> CreateParentsDictionary(CompilationUnitSyntax root)
        {
            var result = new Dictionary<SyntaxNode, SyntaxNode?>();
            result.Add(root, null);
            CreateParentsDictionary(result, root);
            return result;
        }

        private void CreateParentsDictionary(Dictionary<SyntaxNode, SyntaxNode?> result, SyntaxNode node)
        {
            foreach (SyntaxNode? child in node.GetChildren())
            {
                result.Add(child, node);
                CreateParentsDictionary(result, child);
            }
        }
    }
}