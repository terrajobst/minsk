using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.CodeAnalysis.Optimizing
{
    internal class Optimizer : BoundTreeRewriter
    {
        private readonly Evaluator _evaluator;
        private Optimizer()
        {
            _evaluator = new Evaluator(null, null);
        }

        public static BoundBlockStatement Optimize(BoundBlockStatement statements)
        {
            var optimizer = new Optimizer();
            var simplified = optimizer.RewriteBlockStatement(statements);
            var result = RemoveUnreachableCode(simplified);
            return RemoveNoOps(result);
        }

        private static BoundBlockStatement RemoveUnreachableCode(BoundBlockStatement block)
        {
            ImmutableArray<BoundStatement>.Builder builder = null;
            var skipUpToNextLabel = false;
            var definedLabels = new Dictionary<BoundLabel, int>();
            var targetedLabels = new Dictionary<BoundLabel, List<(int line, bool conditional)>>();
            for (int i = 0; i < block.Statements.Length; i++)
            {
                var statement = block.Statements[i];
                switch (statement.Kind)
                {
                    case BoundNodeKind.ReturnStatement:
                        addStatement();
                        skipUpToNextLabel = true;
                        break;
                    case BoundNodeKind.LabelStatement:
                        definedLabels.Add(((BoundLabelStatement)statement).Label, builder?.Count ?? i);
                        skipUpToNextLabel = false;
                        addStatement();
                        break;
                    case BoundNodeKind.ConditionalGotoStatement:
                        addTargetToLabel(((BoundConditionalGotoStatement)statement).Label, builder?.Count ?? i, true);
                        addStatement();
                        break;
                    case BoundNodeKind.GotoStatement:
                        addTargetToLabel(((BoundGotoStatement)statement).Label, builder?.Count ?? i, false);
                        addStatement();
                        skipUpToNextLabel = true;
                        break;
                    default:
                        addStatement();
                        break;
                }

                void addStatement()
                {
                    if (skipUpToNextLabel)
                        initBuilder(i);
                    else if (builder != null)
                        builder.Add(statement);
                }
            }

            bool checkPendingRemove = true;
            while (checkPendingRemove)
            {
                checkPendingRemove = false;
                foreach (var item in definedLabels.ToList())
                {
                    var label = item.Key;
                    var pos = item.Value;
                    if (!targetedLabels.ContainsKey(label))
                    {
                        initBuilder(block.Statements.Length);
                        builder[pos] = BoundNoOperationStatement.Instance;
                        definedLabels.Remove(label);
                    }
                }

                foreach (var item in targetedLabels.ToList())
                {
                    var label = item.Key;
                    var jumps = item.Value;
                    var lblPos = definedLabels[label];
                    foreach (var jump in jumps.ToList())
                    {
                        if (jump.conditional)
                        {
                            var next = nextStatement(jump.line);
                            if (next.statement.Kind == BoundNodeKind.GotoStatement)
                            {
                                var following = nextStatement(next.line);
                                if (following.statement.Kind == BoundNodeKind.LabelStatement && lblPos == following.line)
                                {
                                    var cndJumpStatement = (BoundConditionalGotoStatement) getStatement(jump.line);
                                    var jumpStatement = (BoundGotoStatement) next.statement;
                                    initBuilder(block.Statements.Length);
                                    removeStatement(jump.line);
                                    removeStatement(next.line);
                                    builder[next.line] = new BoundConditionalGotoStatement(jumpStatement.Label, cndJumpStatement.Condition, !cndJumpStatement.JumpIfTrue);
                                    addTargetToLabel(jumpStatement.Label, next.line, true);
                                    checkPendingRemove = true;
                                }
                            }
                        }
                        else
                        {
                            var intermediateLabelsWithOuterJumps =
                                from other in definedLabels
                                let otherLbl = (label: other.Key, line: other.Value)
                                where otherLbl.line > jump.line
                                where otherLbl.line < lblPos
                                let otherJumps = targetedLabels.TryGetValue(otherLbl.label, out var jmps) ? jmps : Enumerable.Empty<(int line, bool conditional)>()
                                where otherJumps.Any(otherJump => otherJump.line < jump.line || otherJump.line > lblPos)
                                select otherLbl.label;

                            if (lblPos > jump.line && !intermediateLabelsWithOuterJumps.Any())
                            {
                                initBuilder(block.Statements.Length);
                                for (int j = jump.line; j < lblPos; j++)
                                    removeStatement(j);
                                checkPendingRemove = true;
                            }
                            else
                            {
                                for (int j = jump.line + 1; j < (builder?.Count ?? block.Statements.Length); j++)
                                {
                                    var statement = getStatement(j);
                                    if (statement.Kind == BoundNodeKind.LabelStatement)
                                        break;
                                    if (statement.Kind != BoundNodeKind.NoOperationStatement)
                                    {
                                        initBuilder(block.Statements.Length);
                                        removeStatement(j);
                                        checkPendingRemove = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (builder == null)
                return block;
            else
                return new BoundBlockStatement(builder.ToImmutable());

            void initBuilder(int upperLimit)
            {
                if (builder == null)
                {
                    builder = ImmutableArray.CreateBuilder<BoundStatement>();
                    for (int j = 0; j < upperLimit; j++)
                        builder.Add(block.Statements[j]);
                }
            }

            void addTargetToLabel(BoundLabel label, int line, bool conditional)
            {
                if (!skipUpToNextLabel)
                {
                    if (!targetedLabels.TryGetValue(label, out var jumps))
                    {
                        jumps = new List<(int line, bool conditional)>();
                        targetedLabels.Add(label, jumps);
                    }
                    jumps.Add((line, conditional));
                }
            }

            void removeStatement(int line)
            {
                void removeTargetToLabel(BoundLabel label)
                {
                    var jumps = targetedLabels[label];
                    jumps.RemoveAll(jump => jump.line == line);
                    if (jumps.Count == 0)
                        targetedLabels.Remove(label);
                }

                var statement = builder[line];
                switch (statement.Kind)
                {
                    case BoundNodeKind.GotoStatement:
                        removeTargetToLabel(((BoundGotoStatement)statement).Label);
                        break;
                    case BoundNodeKind.ConditionalGotoStatement:
                        removeTargetToLabel(((BoundConditionalGotoStatement)statement).Label);
                        break;
                }
                builder[line] = BoundNoOperationStatement.Instance;
            }

            BoundStatement getStatement(int line) => builder?[line] ?? block.Statements[line];

            (BoundStatement statement, int line) nextStatement(int line)
            {
                for (int j = line + 1; j < (builder?.Count ?? block.Statements.Length); j++)
                {
                    var statement = getStatement(j);
                    if (statement.Kind != BoundNodeKind.NoOperationStatement)
                        return (statement, j);
                }
                return (BoundNoOperationStatement.Instance, -1);
            }
        }

        private static BoundBlockStatement RemoveNoOps(BoundBlockStatement block)
        {
            ImmutableArray<BoundStatement>.Builder builder = null;
            for (int i = 0; i < block.Statements.Length; i++)
            {
                var statement = block.Statements[i];
                if (statement.Kind == BoundNodeKind.NoOperationStatement)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStatement>(block.Statements.Length);

                        for (var j = 0; j < i; j++)
                            builder.Add(block.Statements[j]);
                    }
                }
                else
                {
                    if (builder != null)
                        builder.Add(statement);
                }
            }
            if (builder == null)
                return block;
            else
                return new BoundBlockStatement(builder.ToImmutable());
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var condition = RewriteExpression(node.Condition);

            if (condition.Kind == BoundNodeKind.LiteralExpression)
            {
                var evaluatedCondition = (bool)_evaluator.EvaluateExpression(condition);
                if (evaluatedCondition == node.JumpIfTrue)
                    return new BoundGotoStatement(node.Label);
                else
                    return BoundNoOperationStatement.Instance;
            }

            if (condition == node.Condition)
                return node;

            return new BoundConditionalGotoStatement(node.Label, condition, node.JumpIfTrue);
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
            else if (node.Op.Kind == BoundUnaryOperatorKind.Identity)
                return operand;
            else if (operand.Kind == BoundNodeKind.UnaryExpression)
            {
                switch  (node.Op.Kind)
                {
                    case BoundUnaryOperatorKind.LogicalNegation:
                    case BoundUnaryOperatorKind.Negation:
                    case BoundUnaryOperatorKind.OnesComplement:
                        var unaryOperand = (BoundUnaryExpression) operand;
                        if (unaryOperand.Op.Kind == node.Op.Kind)
                            return unaryOperand.Operand;
                        break;
                }
            }
            else if (node.Op.Kind == BoundUnaryOperatorKind.LogicalNegation && operand.Kind == BoundNodeKind.BinaryExpression)
            {
                var binaryOperand = (BoundBinaryExpression) operand;
                var negatedSyntax = TryLogicalNegateOperator(binaryOperand.Op.Kind);
                if (negatedSyntax != null)
                    return new BoundBinaryExpression(binaryOperand.Left, BoundBinaryOperator.Bind(negatedSyntax.Value, binaryOperand.Op.LeftType, binaryOperand.Op.RightType), binaryOperand.Right);
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
            else if (left.Kind == BoundNodeKind.VariableExpression && right.Kind == BoundNodeKind.VariableExpression)
            {
                var leftVarNode = (BoundVariableExpression)left;
                var rightVarNode = (BoundVariableExpression)right;
                if (leftVarNode.Variable.Kind == rightVarNode.Variable.Kind && leftVarNode.Variable.Name == rightVarNode.Variable.Name)
                {
                    if (TryRewriteSameVarBinaryExpression(leftVarNode, node.Op) is BoundExpression rewritten)
                        return rewritten;
                }
            }

            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpression(left, node.Op, right);
        }

        protected override BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression.Kind == BoundNodeKind.LiteralExpression)
            {
                var evaluableNode = node;
                if (expression != node.Expression)
                    evaluableNode = new BoundConversionExpression(node.Type, expression);

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

            if (expression == node.Expression)
                return node;

            return new BoundConversionExpression(node.Type, expression);
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

        private BoundExpression LogicalNegateExpression(BoundExpression node)
        {
            return RewriteUnaryExpression(new BoundUnaryExpression(BoundUnaryOperator.Bind(SyntaxKind.BangToken, node.Type), node));
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

        private BoundExpression TryRewriteSameVarBinaryExpression(BoundVariableExpression varNode, BoundBinaryOperator op)
        {
            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.BitwiseAnd:
                case BoundBinaryOperatorKind.BitwiseOr:
                case BoundBinaryOperatorKind.LogicalAnd:
                case BoundBinaryOperatorKind.LogicalOr:
                    return varNode;
                case BoundBinaryOperatorKind.BitwiseXor:
                    return new BoundLiteralExpression(varNode.Type == TypeSymbol.Int ? (object)0 : (object)false);
                case BoundBinaryOperatorKind.Equals:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                case BoundBinaryOperatorKind.LessOrEquals:
                    return new BoundLiteralExpression(true);
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.NotEquals:
                    return new BoundLiteralExpression(false);
            }
            return null;
        }

        private BoundExpression NegateExpression(BoundExpression node)
        {
            return RewriteUnaryExpression(new BoundUnaryExpression(BoundUnaryOperator.Bind(SyntaxKind.MinusToken, node.Type), node));
        }

        private static SyntaxKind? TryLogicalNegateOperator(BoundBinaryOperatorKind opKind)
        {
            switch (opKind)
            {
                case BoundBinaryOperatorKind.Equals:
                    return SyntaxKind.BangEqualsToken;
                case BoundBinaryOperatorKind.Greater:
                    return SyntaxKind.LessOrEqualsToken;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return SyntaxKind.LessToken;
                case BoundBinaryOperatorKind.Less:
                    return SyntaxKind.GreaterOrEqualsToken;
                case BoundBinaryOperatorKind.LessOrEquals:
                    return SyntaxKind.GreaterToken;
                case BoundBinaryOperatorKind.NotEquals:
                    return SyntaxKind.EqualsEqualsToken;
            }

            return null;
        }
    }
}