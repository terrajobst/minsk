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
            _ = syntaxTree ?? throw new ArgumentNullException(nameof(syntaxTree));
            ImmutableArray<ClassifiedSpan>.Builder? result = ImmutableArray.CreateBuilder<ClassifiedSpan>();
            ClassifyNode(syntaxTree.Root, span, result);
            return result.ToImmutable();
        }

        private static void ClassifyNode(SyntaxNode node, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (!node.FullSpan.OverlapsWith(span))
            {
                return;
            }

            if (node is SyntaxToken token)
            {
                ClassifyToken(token, span, result);
            }

            foreach (SyntaxNode? child in node.GetChildren())
            {
                ClassifyNode(child, span, result);
            }
        }

        private static void ClassifyToken(SyntaxToken token, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            foreach (SyntaxTrivia? leadingTrivia in token.LeadingTrivia)
            {
                ClassifyTrivia(leadingTrivia, span, result);
            }

            AddClassification(token.Kind, token.Span, span, result);

            foreach (SyntaxTrivia? trailingTrivia in token.TrailingTrivia)
            {
                ClassifyTrivia(trailingTrivia, span, result);
            }
        }

        private static void ClassifyTrivia(SyntaxTrivia trivia, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            AddClassification(trivia.Kind, trivia.Span, span, result);
        }

        private static void AddClassification(SyntaxKind elementKind, TextSpan elementSpan, TextSpan span, ImmutableArray<ClassifiedSpan>.Builder result)
        {
            if (!elementSpan.OverlapsWith(span))
            {
                return;
            }

            int adjustedStart = Math.Max(elementSpan.Start, span.Start);
            int adjustedEnd = Math.Min(elementSpan.End, span.End);
            TextSpan adjustedSpan = TextSpan.FromBounds(adjustedStart, adjustedEnd);
            Classification classification = GetClassification(elementKind);

            ClassifiedSpan? classifiedSpan = new ClassifiedSpan(adjustedSpan, classification);
            result.Add(classifiedSpan);
        }

        private static Classification GetClassification(SyntaxKind kind)
        {
            bool isKeyword = kind.IsKeyword();
            bool isIdentifier = kind == SyntaxKind.IdentifierToken;
            bool isNumber = kind == SyntaxKind.NumberToken;
            bool isString = kind == SyntaxKind.StringToken;
            bool isComment = kind.IsComment();

            if (isKeyword)
            {
                return Classification.Keyword;
            }
            else if (isIdentifier)
            {
                return Classification.Identifier;
            }
            else if (isNumber)
            {
                return Classification.Number;
            }
            else if (isString)
            {
                return Classification.String;
            }
            else if (isComment)
            {
                return Classification.Comment;
            }
            else
            {
                return Classification.Text;
            }
        }
    }
}
