using System;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement)node);
                case BoundNodeKind.NopStatement:
                    return RewriteNopStatement((BoundNopStatement)node);
                case BoundNodeKind.VariableDeclaration:
                    return RewriteVariableDeclaration((BoundVariableDeclaration)node);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement)node);
                case BoundNodeKind.WhileStatement:
                    return RewriteWhileStatement((BoundWhileStatement)node);
                case BoundNodeKind.DoWhileStatement:
                    return RewriteDoWhileStatement((BoundDoWhileStatement)node);
                case BoundNodeKind.ForStatement:
                    return RewriteForStatement((BoundForStatement)node);
                case BoundNodeKind.LabelStatement:
                    return RewriteLabelStatement((BoundLabelStatement)node);
                case BoundNodeKind.GotoStatement:
                    return RewriteGotoStatement((BoundGotoStatement)node);
                case BoundNodeKind.ConditionalGotoStatement:
                    return RewriteConditionalGotoStatement((BoundConditionalGotoStatement)node);
                case BoundNodeKind.ReturnStatement:
                    return RewriteReturnStatement((BoundReturnStatement)node);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            ImmutableArray<BoundStatement>.Builder? builder = null;

            for (int i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement? oldStatement = node.Statements[i];
                BoundStatement? newStatement = RewriteStatement(oldStatement);
                if (newStatement != oldStatement)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStatement>(node.Statements.Length);

                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(node.Statements[j]);
                        }
                    }
                }

                if (builder != null)
                {
                    builder.Add(newStatement);
                }
            }

            if (builder == null)
            {
                return node;
            }

            return new BoundBlockStatement(node.Syntax, builder.MoveToImmutable());
        }

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteVariableDeclaration(BoundVariableDeclaration node)
        {
            BoundExpression? initializer = RewriteExpression(node.Initializer);
            if (initializer == node.Initializer)
            {
                return node;
            }

            return new BoundVariableDeclaration(node.Syntax, node.Variable, initializer);
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            BoundExpression? condition = RewriteExpression(node.Condition);
            BoundStatement? thenStatement = RewriteStatement(node.ThenStatement);
            BoundStatement? elseStatement = node.ElseStatement == null ? null : RewriteStatement(node.ElseStatement);
            if (condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
            {
                return node;
            }

            return new BoundIfStatement(node.Syntax, condition, thenStatement, elseStatement);
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            BoundExpression? condition = RewriteExpression(node.Condition);
            BoundStatement? body = RewriteStatement(node.Body);
            if (condition == node.Condition && body == node.Body)
            {
                return node;
            }

            return new BoundWhileStatement(node.Syntax, condition, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            BoundStatement? body = RewriteStatement(node.Body);
            BoundExpression? condition = RewriteExpression(node.Condition);
            if (body == node.Body && condition == node.Condition)
            {
                return node;
            }

            return new BoundDoWhileStatement(node.Syntax, body, condition, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            BoundExpression? lowerBound = RewriteExpression(node.LowerBound);
            BoundExpression? upperBound = RewriteExpression(node.UpperBound);
            BoundStatement? body = RewriteStatement(node.Body);
            if (lowerBound == node.LowerBound && upperBound == node.UpperBound && body == node.Body)
            {
                return node;
            }

            return new BoundForStatement(node.Syntax, node.Variable, lowerBound, upperBound, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            BoundExpression? condition = RewriteExpression(node.Condition);
            if (condition == node.Condition)
            {
                return node;
            }

            return new BoundConditionalGotoStatement(node.Syntax, node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            BoundExpression? expression = node.Expression == null ? null : RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundReturnStatement(node.Syntax, expression);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression? expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundExpressionStatement(node.Syntax, expression);
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    return RewriteErrorExpression((BoundErrorExpression)node);
                case BoundNodeKind.LiteralExpression:
                    return RewriteLiteralExpression((BoundLiteralExpression)node);
                case BoundNodeKind.VariableExpression:
                    return RewriteVariableExpression((BoundVariableExpression)node);
                case BoundNodeKind.AssignmentExpression:
                    return RewriteAssignmentExpression((BoundAssignmentExpression)node);
                case BoundNodeKind.CompoundAssignmentExpression:
                    return RewriteCompoundAssignmentExpression((BoundCompoundAssignmentExpression)node);
                case BoundNodeKind.UnaryExpression:
                    return RewriteUnaryExpression((BoundUnaryExpression)node);
                case BoundNodeKind.BinaryExpression:
                    return RewriteBinaryExpression((BoundBinaryExpression)node);
                case BoundNodeKind.CallExpression:
                    return RewriteCallExpression((BoundCallExpression)node);
                case BoundNodeKind.ConversionExpression:
                    return RewriteConversionExpression((BoundConversionExpression)node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            BoundExpression? expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundAssignmentExpression(node.Syntax, node.Variable, expression);
        }

        protected virtual BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            BoundExpression? expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundCompoundAssignmentExpression(node.Syntax, node.Variable, node.Op, expression);
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            BoundExpression? operand = RewriteExpression(node.Operand);
            if (operand == node.Operand)
            {
                return node;
            }

            return new BoundUnaryExpression(node.Syntax, node.Op, operand);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            BoundExpression? left = RewriteExpression(node.Left);
            BoundExpression? right = RewriteExpression(node.Right);
            if (left == node.Left && right == node.Right)
            {
                return node;
            }

            return new BoundBinaryExpression(node.Syntax, left, node.Op, right);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            ImmutableArray<BoundExpression>.Builder? builder = null;

            for (int i = 0; i < node.Arguments.Length; i++)
            {
                BoundExpression? oldArgument = node.Arguments[i];
                BoundExpression? newArgument = RewriteExpression(oldArgument);
                if (newArgument != oldArgument)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpression>(node.Arguments.Length);

                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(node.Arguments[j]);
                        }
                    }
                }

                if (builder != null)
                {
                    builder.Add(newArgument);
                }
            }

            if (builder == null)
            {
                return node;
            }

            return new BoundCallExpression(node.Syntax, node.Function, builder.MoveToImmutable());
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            BoundExpression? expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundConversionExpression(node.Syntax, node.Type, expression);
        }
    }
}
