using System.Collections.Generic;
using System.Collections.Immutable;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class SyntaxTree
    {
        private SyntaxTree(SourceText text)
        {
            var parser = new Parser(text);
            var root = parser.ParseCompilationUnit();

            Text = text;
            Diagnostics = parser.Diagnostics.ToImmutableArray();
            Root = root;
        }

        public SourceText Text { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        public static SyntaxTree Parse(string text)
        {
            var sourceText = SourceText.From(text);
            return Parse(sourceText);
        }

        public static SyntaxTree Parse(SourceText text)
        {
            return new SyntaxTree(text);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text)
        {
            var sourceText = SourceText.From(text);
            return ParseTokens(sourceText);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics)
        {
            var sourceText = SourceText.From(text);
            return ParseTokens(sourceText, out diagnostics);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text)
        {
            return ParseTokens(text, out _);
        }

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics)
        {
            IEnumerable<SyntaxToken> LexTokens(Lexer lexer)
            {
                while (true)
                {
                    var token = lexer.Lex();
                    if (token.Kind == SyntaxKind.EndOfFileToken)
                        break;

                    yield return token;
                }
            }
            var l = new Lexer(text);
            var result = LexTokens(l).ToImmutableArray();
            diagnostics = l.Diagnostics.ToImmutableArray();
            return result;
        }
    }
}