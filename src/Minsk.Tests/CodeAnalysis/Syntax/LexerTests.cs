using System;
using System.Collections.Generic;
using System.Linq;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;
using Xunit;

namespace Minsk.Tests.CodeAnalysis.Syntax
{
    public class LexerTests
    {
        [Fact]
        public void Lexer_Lexes_UnterminatedString()
        {
            string? text = "\"text";
            System.Collections.Immutable.ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text, out System.Collections.Immutable.ImmutableArray<Minsk.CodeAnalysis.Diagnostic> diagnostics);

            SyntaxToken? token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.StringToken, token.Kind);
            Assert.Equal(text, token.Text);

            Minsk.CodeAnalysis.Diagnostic? diagnostic = Assert.Single(diagnostics);
            Assert.Equal(new TextSpan(0, 1), diagnostic.Location.Span);
            Assert.Equal("Unterminated string literal.", diagnostic.Message);
        }

        [Fact]
        public void Lexer_Covers_AllTokens()
        {
            IEnumerable<SyntaxKind>? tokenKinds = Enum.GetValues(typeof(SyntaxKind))
                                 .Cast<SyntaxKind>()
                                 .Where(k => k.IsToken());

            IEnumerable<SyntaxKind>? testedTokenKinds = GetTokens().Concat(GetSeparators()).Select(t => t.kind);

            SortedSet<SyntaxKind>? untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);
            untestedTokenKinds.Remove(SyntaxKind.BadToken);
            untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);
            untestedTokenKinds.ExceptWith(testedTokenKinds);

            Assert.Empty(untestedTokenKinds);
        }

        [Theory]
        [MemberData(nameof(GetTokensData))]
        public void Lexer_Lexes_Token(SyntaxKind kind, string text)
        {
            System.Collections.Immutable.ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);

            SyntaxToken? token = Assert.Single(tokens);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetSeparatorsData))]
        public void Lexer_Lexes_Separator(SyntaxKind kind, string text)
        {
            System.Collections.Immutable.ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text, includeEndOfFile: true);

            SyntaxToken? token = Assert.Single(tokens);
            SyntaxTrivia? trivia = Assert.Single(token.LeadingTrivia);
            Assert.Equal(kind, trivia.Kind);
            Assert.Equal(text, trivia.Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsData))]
        public void Lexer_Lexes_TokenPairs(SyntaxKind t1Kind, string t1Text,
                                           SyntaxKind t2Kind, string t2Text)
        {
            string? text = t1Text + t2Text;
            SyntaxToken[]? tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);
            Assert.Equal(t1Kind, tokens[0].Kind);
            Assert.Equal(t1Text, tokens[0].Text);
            Assert.Equal(t2Kind, tokens[1].Kind);
            Assert.Equal(t2Text, tokens[1].Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsWithSeparatorData))]
        public void Lexer_Lexes_TokenPairs_WithSeparators(SyntaxKind t1Kind, string t1Text,
                                                          SyntaxKind separatorKind, string separatorText,
                                                          SyntaxKind t2Kind, string t2Text)
        {
            string? text = t1Text + separatorText + t2Text;
            SyntaxToken[]? tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);
            Assert.Equal(t1Kind, tokens[0].Kind);
            Assert.Equal(t1Text, tokens[0].Text);

            SyntaxTrivia? separator = Assert.Single(tokens[0].TrailingTrivia);
            Assert.Equal(separatorKind, separator.Kind);
            Assert.Equal(separatorText, separator.Text);

            Assert.Equal(t2Kind, tokens[1].Kind);
            Assert.Equal(t2Text, tokens[1].Text);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo42")]
        [InlineData("foo_42")]
        [InlineData("_foo")]
        public void Lexer_Lexes_Identifiers(string name)
        {
            SyntaxToken[]? tokens = SyntaxTree.ParseTokens(name).ToArray();

            Assert.Single(tokens);

            SyntaxToken? token = tokens[0];
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
            Assert.Equal(name, token.Text);
        }

        public static IEnumerable<object[]> GetTokensData()
        {
            foreach ((SyntaxKind kind, string text) t in GetTokens())
            {
                yield return new object[] { t.kind, t.text };
            }
        }

        public static IEnumerable<object[]> GetSeparatorsData()
        {
            foreach ((SyntaxKind kind, string text) t in GetSeparators())
            {
                yield return new object[] { t.kind, t.text };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            foreach ((SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text) t in GetTokenPairs())
            {
                yield return new object[] { t.t1Kind, t.t1Text, t.t2Kind, t.t2Text };
            }
        }

        public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
        {
            foreach ((SyntaxKind t1Kind, string t1Text, SyntaxKind separatorKind, string separatorText, SyntaxKind t2Kind, string t2Text) t in GetTokenPairsWithSeparator())
            {
                yield return new object[] { t.t1Kind, t.t1Text, t.separatorKind, t.separatorText, t.t2Kind, t.t2Text };
            }
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
        {
            IEnumerable<(SyntaxKind, string)>? fixedTokens = Enum.GetValues(typeof(SyntaxKind))
                                  .Cast<SyntaxKind>()
                                  .Select(k => (k, text: SyntaxFacts.GetText(k)))
                                  .Where(t => t.text != null)
                                  .Cast<(SyntaxKind, string)>();

            (SyntaxKind, string)[]? dynamicTokens = new[]
            {
                (SyntaxKind.NumberToken, "1"),
                (SyntaxKind.NumberToken, "123"),
                (SyntaxKind.IdentifierToken, "a"),
                (SyntaxKind.IdentifierToken, "abc"),
                (SyntaxKind.StringToken, "\"Test\""),
                (SyntaxKind.StringToken, "\"Te\"\"st\""),
            };

            return fixedTokens.Concat(dynamicTokens);
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
        {
            return new[]
            {
                (SyntaxKind.WhitespaceTrivia, " "),
                (SyntaxKind.WhitespaceTrivia, "  "),
                (SyntaxKind.LineBreakTrivia, "\r"),
                (SyntaxKind.LineBreakTrivia, "\n"),
                (SyntaxKind.LineBreakTrivia, "\r\n"),
                (SyntaxKind.MultiLineCommentTrivia, "/**/"),
            };
        }

        private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            bool t1IsKeyword = t1Kind.IsKeyword();
            bool t2IsKeyword = t2Kind.IsKeyword();

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
            {
                return true;
            }

            if (t1IsKeyword && t2IsKeyword)
            {
                return true;
            }

            if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.NumberToken)
            {
                return true;
            }

            if (t1IsKeyword && t2Kind == SyntaxKind.NumberToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.NumberToken && t2Kind == SyntaxKind.NumberToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.StringToken && t2Kind == SyntaxKind.StringToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandAmpersandToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipePipeToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.HatToken && t2Kind == SyntaxKind.EqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.HatToken && t2Kind == SyntaxKind.EqualsEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.StarToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.StarEqualsToken)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SingleLineCommentTrivia)
            {
                return true;
            }

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.MultiLineCommentTrivia)
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
        {
            foreach ((SyntaxKind kind, string text) t1 in GetTokens())
            {
                foreach ((SyntaxKind kind, string text) t2 in GetTokens())
                {
                    if (!RequiresSeparator(t1.kind, t2.kind))
                    {
                        yield return (t1.kind, t1.text, t2.kind, t2.text);
                    }
                }
            }
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text,
                                    SyntaxKind separatorKind, string separatorText,
                                    SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
        {
            foreach ((SyntaxKind kind, string text) t1 in GetTokens())
            {
                foreach ((SyntaxKind kind, string text) t2 in GetTokens())
                {
                    if (RequiresSeparator(t1.kind, t2.kind))
                    {
                        foreach ((SyntaxKind kind, string text) s in GetSeparators())
                        {
                            if (!RequiresSeparator(t1.kind, s.kind) && !RequiresSeparator(s.kind, t2.kind))
                            {
                                yield return (t1.kind, t1.text, s.kind, s.text, t2.kind, t2.text);
                            }
                        }
                    }
                }
            }
        }
    }
}
