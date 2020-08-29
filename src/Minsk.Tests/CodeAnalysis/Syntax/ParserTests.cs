using System.Collections.Generic;
using System.Diagnostics;
using Minsk.CodeAnalysis.Syntax;
using Xunit;

namespace Minsk.Tests.CodeAnalysis.Syntax
{
    public class ParserTests
    {
        [Theory]
        [MemberData(nameof(GetBinaryOperatorPairsData))]
        public void Parser_BinaryExpression_HonorsPrecedences(SyntaxKind op1, SyntaxKind op2)
        {
            int op1Precedence = SyntaxFacts.GetBinaryOperatorPrecedence(op1);
            int op2Precedence = SyntaxFacts.GetBinaryOperatorPrecedence(op2);
            string? op1Text = SyntaxFacts.GetText(op1);
            string? op2Text = SyntaxFacts.GetText(op2);
            string? text = $"a {op1Text} b {op2Text} c";
            ExpressionSyntax? expression = ParseExpression(text);

            Debug.Assert(op1Text != null);
            Debug.Assert(op2Text != null);

            if (op1Precedence >= op2Precedence)
            {
                //     op2
                //    /   \
                //   op1   c
                //  /   \
                // a     b

                using (AssertingEnumerator? e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(op1, op1Text);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                    e.AssertToken(op2, op2Text);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "c");
                }
            }
            else
            {
                //   op1
                //  /   \
                // a    op2
                //     /   \
                //    b     c

                using (AssertingEnumerator? e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(op1, op1Text);
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                    e.AssertToken(op2, op2Text);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "c");
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetUnaryOperatorPairsData))]
        public void Parser_UnaryExpression_HonorsPrecedences(SyntaxKind unaryKind, SyntaxKind binaryKind)
        {
            int unaryPrecedence = SyntaxFacts.GetUnaryOperatorPrecedence(unaryKind);
            int binaryPrecedence = SyntaxFacts.GetBinaryOperatorPrecedence(binaryKind);
            string? unaryText = SyntaxFacts.GetText(unaryKind);
            string? binaryText = SyntaxFacts.GetText(binaryKind);
            string? text = $"{unaryText} a {binaryText} b";
            ExpressionSyntax? expression = ParseExpression(text);

            Debug.Assert(unaryText != null);
            Debug.Assert(binaryText != null);

            if (unaryPrecedence >= binaryPrecedence)
            {
                //   binary
                //   /    \
                // unary   b
                //   |
                //   a

                using (AssertingEnumerator? e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.UnaryExpression);
                    e.AssertToken(unaryKind, unaryText);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(binaryKind, binaryText);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                }
            }
            else
            {
                //  unary
                //    |
                //  binary
                //  /   \
                // a     b

                using (AssertingEnumerator? e = new AssertingEnumerator(expression))
                {
                    e.AssertNode(SyntaxKind.UnaryExpression);
                    e.AssertToken(unaryKind, unaryText);
                    e.AssertNode(SyntaxKind.BinaryExpression);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "a");
                    e.AssertToken(binaryKind, binaryText);
                    e.AssertNode(SyntaxKind.NameExpression);
                    e.AssertToken(SyntaxKind.IdentifierToken, "b");
                }
            }
        }

        private static ExpressionSyntax ParseExpression(string text)
        {
            SyntaxTree? syntaxTree = SyntaxTree.Parse(text);
            CompilationUnitSyntax? root = syntaxTree.Root;
            MemberSyntax? member = Assert.Single(root.Members);
            GlobalStatementSyntax? globalStatement = Assert.IsType<GlobalStatementSyntax>(member);
            return Assert.IsType<ExpressionStatementSyntax>(globalStatement.Statement).Expression;
        }

        public static IEnumerable<object[]> GetBinaryOperatorPairsData()
        {
            foreach (SyntaxKind op1 in SyntaxFacts.GetBinaryOperatorKinds())
            {
                foreach (SyntaxKind op2 in SyntaxFacts.GetBinaryOperatorKinds())
                {
                    yield return new object[] { op1, op2 };
                }
            }
        }

        public static IEnumerable<object[]> GetUnaryOperatorPairsData()
        {
            foreach (SyntaxKind unary in SyntaxFacts.GetUnaryOperatorKinds())
            {
                foreach (SyntaxKind binary in SyntaxFacts.GetBinaryOperatorKinds())
                {
                    yield return new object[] { unary, binary };
                }
            }
        }
    }
}
