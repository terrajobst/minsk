﻿using System;
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
            var text = "\"text";
            var tokens = SyntaxTree.ParseTokens(text, out var diagnostics);

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.StringToken, token.Kind);
            Assert.Equal(text, token.Text);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(new TextSpan(0, 1), diagnostic.Location.Span);
            Assert.Equal("Unterminated string literal.", diagnostic.Message);
        }

        [Fact]
        public void Lexer_Covers_AllTokens()
        {
            var tokenKinds = Enum.GetValues(typeof(SyntaxKind))
                                 .Cast<SyntaxKind>()
                                 .Where(k => k.ToString().EndsWith("Keyword") ||
                                             k.ToString().EndsWith("Token"));

            var testedTokenKinds = GetTokens().Select(t => t.kind);

            var untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);
            untestedTokenKinds.Remove(SyntaxKind.BadToken);
            untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);
            untestedTokenKinds.ExceptWith(testedTokenKinds);

            Assert.Empty(untestedTokenKinds);
        }

        [Theory]
        [MemberData(nameof(GetTokensData))]
        public void Lexer_Lexes_Token(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text);

            var token = Assert.Single(tokens);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }

        [Theory]
        [MemberData(nameof(GetTokenPairsData))]
        public void Lexer_Lexes_TokenPairs(SyntaxKind t1Kind, string t1Text,
                                           SyntaxKind t2Kind, string t2Text)
        {
            var text = t1Text + t2Text;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

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
            var text = t1Text + separatorText + t2Text;
            var tokens = SyntaxTree.ParseTokens(text).ToArray();

            Assert.Equal(2, tokens.Length);
            Assert.Equal(t1Kind, tokens[0].Kind);
            Assert.Equal(t1Text, tokens[0].Text);
            var trivia = Assert.Single(tokens[0].TrailingTrivia);
            Assert.Equal(separatorKind, trivia.Kind);
            Assert.Equal(separatorText, trivia.Text);
            Assert.Equal(t2Kind, tokens[1].Kind);
            Assert.Equal(t2Text, tokens[1].Text);
        }

        [Fact]
        public void Lexer_Covers_AllTrivia()
        {
            var triviaKinds = Enum.GetValues(typeof(SyntaxKind))
                                  .Cast<SyntaxKind>()
                                  .Where(k => k.ToString().EndsWith("Trivia"));

            var testedTriviaKinds = GetTrivia().Select(t => t.kind);

            var untestedTriviaKinds = new SortedSet<SyntaxKind>(triviaKinds);
            untestedTriviaKinds.ExceptWith(testedTriviaKinds);

            Assert.Empty(untestedTriviaKinds);
        }

        [Theory]
        [MemberData(nameof(GetTriviaData))]
        public void Lexer_Lexes_TrailingTrivia(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens("tokenWithTrailingTrivia" + text);

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
            Assert.Empty(token.LeadingTrivia);
            var trivia = Assert.Single(token.TrailingTrivia);
            Assert.Equal(kind, trivia.Kind);
            Assert.Equal(text, trivia.Text);
        }

        [Theory]
        [MemberData(nameof(GetTriviaData))]
        public void Lexer_Lexes_LeadingTrivia(SyntaxKind kind, string text)
        {
            if (kind == SyntaxKind.SingleLineCommentTrivia)
                return;

            var tokens = SyntaxTree.ParseTokens(text + "tokenWithLeadingTrivia");

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
            var trivia = Assert.Single(token.LeadingTrivia);
            Assert.Equal(kind, trivia.Kind);
            Assert.Equal(text, trivia.Text);
            Assert.Empty(token.TrailingTrivia);
        }

        [Theory]
        [MemberData(nameof(GetTriviaData))]
        public void Lexer_Lexes_EndOfFileLeadingTrivia(SyntaxKind kind, string text)
        {
            var tokens = SyntaxTree.ParseTokens(text, includeEndOfFile: true);

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind);
            var trivia = Assert.Single(token.LeadingTrivia);
            Assert.Empty(token.TrailingTrivia);
            Assert.Equal(kind, trivia.Kind);
            Assert.Equal(text, trivia.Text);
            Assert.Empty(token.TrailingTrivia);
        }

        [Theory]
        [MemberData(nameof(GetTriviaLineEndingPairsData))]
        public void Lexer_Lexes_LeadingTriviaFromPreviousLine(SyntaxKind kind, string text, string eolText)
        {
            var tokens = SyntaxTree.ParseTokens(text + eolText + "tokenWithLeadingTrivia");

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
            Assert.Collection(token.LeadingTrivia,
                trivia =>
                {
                    Assert.Equal(kind, trivia.Kind);
                    Assert.Equal(text, trivia.Text);
                },
                eolTrivia =>
                {
                    Assert.Equal(SyntaxKind.EndOfLineTrivia, eolTrivia.Kind);
                    Assert.Equal(eolText, eolTrivia.Text);
                }
            );
            Assert.Empty(token.TrailingTrivia);
        }

        [Theory]
        [MemberData(nameof(GetTriviaLineEndingPairsData))]
        public void Lexer_Lexes_EndOfFileLeadingTriviaFromPreviousLine(SyntaxKind kind, string text, string eolText)
        {
            var tokens = SyntaxTree.ParseTokens(text + eolText, includeEndOfFile: true);

            var token = Assert.Single(tokens);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind);
            Assert.Collection(token.LeadingTrivia,
                trivia =>
                {
                    Assert.Equal(kind, trivia.Kind);
                    Assert.Equal(text, trivia.Text);
                },
                eolTrivia =>
                {
                    Assert.Equal(SyntaxKind.EndOfLineTrivia, eolTrivia.Kind);
                    Assert.Equal(eolText, eolTrivia.Text);
                }
            );
            Assert.Empty(token.TrailingTrivia);
        }

        public static IEnumerable<object[]> GetTokensData()
        {
            foreach (var t in GetTokens())
                yield return new object[] { t.kind, t.text };
        }

        public static IEnumerable<object[]> GetTriviaData()
        {
            foreach (var t in GetTrivia())
                yield return new object[] { t.kind, t.text };
        }

        public static IEnumerable<object[]> GetTokenPairsData()
        {
            foreach (var t in GetTokenPairs())
                yield return new object[] { t.t1Kind, t.t1Text, t.t2Kind, t.t2Text };
        }

        public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
        {
            foreach (var t in GetTokenPairsWithSeparator())
                yield return new object[] { t.t1Kind, t.t1Text, t.separatorKind, t.separatorText, t.t2Kind, t.t2Text };
        }

        public static IEnumerable<object[]> GetTriviaLineEndingPairsData()
        {
            foreach (var t in GetTriviaLineEndingPairs())
                yield return new object[] { t.kind, t.text, t.eolText};
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
        {
            var fixedTokens = Enum.GetValues(typeof(SyntaxKind))
                                  .Cast<SyntaxKind>()
                                  .Select(k => (kind: k, text: SyntaxFacts.GetText(k)))
                                  .Where(t => t.text != null);


            var dynamicTokens = new[]
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

        private static IEnumerable<(SyntaxKind kind, string text)> GetLineEndings()
        {
            return new[]
            {
                (SyntaxKind.EndOfLineTrivia, "\r"),
                (SyntaxKind.EndOfLineTrivia, "\n"),
                (SyntaxKind.EndOfLineTrivia, "\r\n")
            };
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
        {
            var seperators = new[]
            {
                (SyntaxKind.WhitespaceTrivia, " "),
                (SyntaxKind.WhitespaceTrivia, "  "),
            };

            return seperators.Concat(GetLineEndings());
        }

        private static IEnumerable<(SyntaxKind kind, string text)> GetTrivia()
        {
            var trivia = new[]
            {
                (SyntaxKind.SingleLineCommentTrivia, "//"),
                (SyntaxKind.SingleLineCommentTrivia, "// "),
                (SyntaxKind.SingleLineCommentTrivia, "//a"),
                (SyntaxKind.SingleLineCommentTrivia, "///"),
            };

            return trivia.Concat(GetSeparators());
        }

        private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
        {
            var t1IsKeyword = t1Kind.ToString().EndsWith("Keyword");
            var t2IsKeyword = t2Kind.ToString().EndsWith("Keyword");

            if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1IsKeyword && t2IsKeyword)
                return true;

            if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
                return true;

            if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
                return true;

            if (t1Kind == SyntaxKind.NumberToken && t2Kind == SyntaxKind.NumberToken)
                return true;

            if (t1Kind == SyntaxKind.StringToken && t2Kind == SyntaxKind.StringToken)
                return true;

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsToken)
                return true;

            if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsEqualsToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandToken)
                return true;

            if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandAmpersandToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeToken)
                return true;

            if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipePipeToken)
                return true;

            if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashToken)
                return true;

            return false;
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
        {
            foreach (var t1 in GetTokens())
            {
                foreach (var t2 in GetTokens())
                {
                    if (!RequiresSeparator(t1.kind, t2.kind))
                        yield return (t1.kind, t1.text, t2.kind, t2.text);
                }
            }
        }

        private static IEnumerable<(SyntaxKind t1Kind, string t1Text,
                                    SyntaxKind separatorKind, string separatorText,
                                    SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
        {
            foreach (var t1 in GetTokens())
            {
                foreach (var t2 in GetTokens())
                {
                    if (RequiresSeparator(t1.kind, t2.kind))
                    {
                        foreach (var s in GetSeparators())
                            yield return (t1.kind, t1.text, s.kind, s.text, t2.kind, t2.text);
                    }
                }
            }
        }

        private static IEnumerable<(SyntaxKind kind, string text, string eolText)> GetTriviaLineEndingPairs()
        {
            foreach (var trivia in GetTrivia())
            {
                foreach (var lineEnding in GetLineEndings())
                {
                    if (trivia.kind != SyntaxKind.EndOfLineTrivia)
                        yield return (trivia.kind, trivia.text, lineEnding.text);
                }
            }
        }
    }
}
