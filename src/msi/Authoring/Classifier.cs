using System;
using System.Collections.Immutable;

using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Authoring
{
    public static class Classifier
    {
        public static ImmutableArray<ClassifiedSpan> Classify(SyntaxTree syntaxTree, TextSpan span)
        {
            var result = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            ClassifyNode(syntaxTree.Root, span, result);
            return result.ToImmutable();
        }

        private static void ClassifyNode(SyntaxNode node, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (!node.FullSpan.OverlapsWith(span))
                return;

            if (node is SyntaxToken token)
                ClassifyToken(token, span, result);

            foreach (var child in node.GetChildren())
                ClassifyNode(child, span, result);
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            foreach (var leadingTrivia in token.LeadingTrivia)
                ClassifyTrivia(leadingTrivia, span, result);

            AddClassification(token.Kind, token.Span, span, result);

            foreach (var trailingTrivia in token.TrailingTrivia)
                ClassifyTrivia(trailingTrivia, span, result);
        }

        private static void ClassifyTrivia(SyntaxTrivia trivia, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            AddClassification(trivia.Kind, trivia.Span, span, result);
        }

        private static void AddClassification(SyntaxKind elementKind, TextSpan elementSpan, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (!elementSpan.OverlapsWith(span))
                return;

            var adjustedStart = Math.Max(elementSpan.Start, span.Start);
            var adjustedEnd = Math.Min(elementSpan.End, span.End);
            var adjustedSpan = TextSpan.FromBounds(adjustedStart, adjustedEnd);
            var classification = GetClassification(elementKind);

            var classifiedSpan = new ClassifiedSpan(adjustedSpan, classification);
            result.Add(classifiedSpan);
        }

        private static Classification GetClassification(SyntaxKind kind)
        {
            var isKeyword = kind.IsKeyword();
            var isIdentifier = kind == SyntaxKind.IdentifierToken;
            var isNumber = kind == SyntaxKind.NumberToken;
            var isString = kind == SyntaxKind.StringToken;
            var isComment = kind.IsComment();

            if (isKeyword)
                return Classification.Keyword;
            else if (isIdentifier)
                return Classification.Identifier;
            else if (isNumber)
                return Classification.Number;
            else if (isString)
                return Classification.String;
            else if (isComment)
                return Classification.Comment;
            else
                return Classification.Text;
        }
    }
}
