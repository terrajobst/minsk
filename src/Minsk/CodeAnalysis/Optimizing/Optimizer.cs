using System;
using System.Collections.Immutable;
using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Optimizing
{
    internal class Optimizer : BoundTreeRewriter
    {
        private readonly Evaluator _evaluator;
        private Optimizer()
        {
            _evaluator = new Evaluator(null, null);
        }

        public static BoundStatement Optimize(BoundStatement statement)
        {
            var optimizer = new Optimizer();
            return optimizer.RewriteStatement(statement);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            var condition = RewriteExpression(node.Condition);

            if (condition.Kind == BoundNodeKind.LiteralExpression)
            {
                var evaluatedCondition = (bool)_evaluator.EvaluateExpression(condition);
                if (evaluatedCondition)
                    return RewriteStatement(node.ThenStatement);
                else if (node.ElseStatement != null)
                    return RewriteStatement(node.ElseStatement);
                else
                    return new BoundBlockStatement(ImmutableArray<BoundStatement>.Empty);
            }

            var thenStatement = RewriteStatement(node.ThenStatement);
            var elseStatement = node.ElseStatement == null ? null : RewriteStatement(node.ElseStatement);
            if (condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
                return node;

            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);

            if (condition.Kind == BoundNodeKind.LiteralExpression)
            {
                var evaluatedCondition = (bool)_evaluator.EvaluateExpression(condition);
                if (!evaluatedCondition)
                    return new BoundBlockStatement(ImmutableArray<BoundStatement>.Empty);
            }

            var body = RewriteStatement(node.Body);
            if (condition == node.Condition && body == node.Body)
                return node;

            return new BoundWhileStatement(condition, body);
        }

        protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            var body = RewriteStatement(node.Body);
            var condition = RewriteExpression(node.Condition);

            if (condition.Kind == BoundNodeKind.LiteralExpression)
            {
                var evaluatedCondition = (bool)_evaluator.EvaluateExpression(condition);
                if (!evaluatedCondition)
                    return body;
            }

            if (body == node.Body && condition == node.Condition)
                return node;

            return new BoundDoWhileStatement(body, condition);
        }

        protected override BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var operand = RewriteExpression(node.Operand);
            if (operand.Kind == BoundNodeKind.LiteralExpression)
            {
                BoundUnaryExpression evaluableNode = node;
                if (operand != node.Operand)
                    evaluableNode = new BoundUnaryExpression(node.Op, operand);

                try
                {
                    var result = _evaluator.EvaluateExpression(evaluableNode);
                    return new BoundLiteralExpression(result);
                }
                catch
                {
                    // Swallow evaluation exceptions, and let it fail at runtime
                }
            }
            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpression(node.Op, operand);
        }

        protected override BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            var leftIsLiteral = left.Kind == BoundNodeKind.LiteralExpression;
            var rightIsLiteral = right.Kind == BoundNodeKind.LiteralExpression;
            if (leftIsLiteral && rightIsLiteral)
            {
                BoundBinaryExpression evaluableNode = node;
                if (left != node.Left || right != node.Right)
                    evaluableNode = new BoundBinaryExpression(left, node.Op, right);

                try
                {
                    var result = _evaluator.EvaluateExpression(evaluableNode);
                    return new BoundLiteralExpression(result);
                }
                catch
                {
                    // Swallow evaluation exceptions, and let it fail at runtime
                }
            }
            else if (leftIsLiteral || rightIsLiteral)
            {
                var literal = leftIsLiteral ? left : right;
                var other = leftIsLiteral ? right : left;
                if (literal.Type == TypeSymbol.Bool)
                {
                    if (TryRewriteBoolPartiallyLiteralBinaryExpression(node.Op, literal, other) is BoundExpression rewritten)
                        return rewritten;
                }
                else if (literal.Type == TypeSymbol.Int)
                {
                    if (TryRewriteIntPartiallyLiteralBinaryExpression(node.Op, literal, other, leftIsLiteral) is BoundExpression rewritten)
                        return rewritten;
                }
            }

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Op, right);
        }

        private BoundExpression TryRewriteBoolPartiallyLiteralBinaryExpression(BoundBinaryOperator op, BoundExpression literal, BoundExpression other)
        {
            var evaluatedLiteral = (bool)_evaluator.EvaluateExpression(literal);
            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.BitwiseAnd:
                    return evaluatedLiteral ? other : null;
                case BoundBinaryOperatorKind.BitwiseOr:
                    return evaluatedLiteral ? null : other;
                case BoundBinaryOperatorKind.BitwiseXor:
                    return evaluatedLiteral ? LogicalNegateExpression(other) : other;
                case BoundBinaryOperatorKind.Equals:
                    return evaluatedLiteral ? other : LogicalNegateExpression(other);
                case BoundBinaryOperatorKind.LogicalAnd:
                    return evaluatedLiteral ? other : new BoundLiteralExpression(false);
                case BoundBinaryOperatorKind.LogicalOr:
                    return evaluatedLiteral ? literal : other;
                case BoundBinaryOperatorKind.NotEquals:
                    return evaluatedLiteral ? LogicalNegateExpression(other) : other;
                default:
                    return null;
            }
        }

        private static BoundExpression LogicalNegateExpression(BoundExpression node)
        {
            return new BoundUnaryExpression(BoundUnaryOperator.Bind(Syntax.SyntaxKind.BangToken, node.Type), node);
        }

        private BoundExpression TryRewriteIntPartiallyLiteralBinaryExpression(BoundBinaryOperator op, BoundExpression literal, BoundExpression other, bool literalIsLeft)
        {
            var evaluatedLiteral = (int)_evaluator.EvaluateExpression(literal);
            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return evaluatedLiteral == 0 ? other : null;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    return evaluatedLiteral == 0 ? literal : null;
                case BoundBinaryOperatorKind.BitwiseOr:
                    return evaluatedLiteral == 0 ? other : null;
                case BoundBinaryOperatorKind.BitwiseXor:
                    return evaluatedLiteral == 0 ? other : null;
                case BoundBinaryOperatorKind.Division:
                    switch (evaluatedLiteral)
                    {
                        case 0 when literalIsLeft:
                            return literal;
                        case 1 when !literalIsLeft:
                            return other;
                        default:
                            return null;
                    }
                case BoundBinaryOperatorKind.Multiplication:
                    switch (evaluatedLiteral)
                    {
                        case 0:
                            return literal;
                        case 1:
                            return other;
                        default:
                            return null;
                    }
                case BoundBinaryOperatorKind.Subtraction:
                    return evaluatedLiteral == 0 ? literalIsLeft ? NegateExpression(other) : other : null;
                default:
                    return null;
            }
        }

        private static BoundExpression NegateExpression(BoundExpression node)
        {
            return new BoundUnaryExpression(BoundUnaryOperator.Bind(Syntax.SyntaxKind.MinusToken, node.Type), node);
        }
    }
}