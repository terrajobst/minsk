using System;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal static class ConstantFolding
    {
        public static BoundConstant? Fold(BoundUnaryOperator op, BoundExpression operand)
        {
            if (operand.ConstantValue != null)
            {
                switch (op.Kind)
                {
                    case BoundUnaryOperatorKind.Identity:
                        return new BoundConstant((int)operand.ConstantValue.Value);
                    case BoundUnaryOperatorKind.Negation:
                        return new BoundConstant(-(int)operand.ConstantValue.Value);
                    case BoundUnaryOperatorKind.LogicalNegation:
                        return new BoundConstant(!(bool)operand.ConstantValue.Value);
                    case BoundUnaryOperatorKind.OnesComplement:
                        return new BoundConstant(~(int)operand.ConstantValue.Value);
                    default:
                        throw new Exception($"Unexpected unary operator {op.Kind}");
                }
            }

            return null;
        }

        public static BoundConstant? Fold(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            var leftConstant = left.ConstantValue;
            var rightConstant = right.ConstantValue;

            // Special case && and || because there are cases where only one
            // side needs to be known.

            if (op.Kind == BoundBinaryOperatorKind.LogicalAnd)
            {
                if (leftConstant != null && !(bool)leftConstant.Value ||
                    rightConstant != null && !(bool)rightConstant.Value)
                {
                    return new BoundConstant(false);
                }
            }

            if (op.Kind == BoundBinaryOperatorKind.LogicalOr)
            {
                if (leftConstant != null && (bool)leftConstant.Value ||
                    rightConstant != null && (bool)rightConstant.Value)
                {
                    return new BoundConstant(true);
                }
            }

            if (leftConstant == null || rightConstant == null)
                return null;

            var l = leftConstant.Value;
            var r = rightConstant.Value;

            switch (op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)l + (int)r);
                    else
                        return new BoundConstant((string)l + (string)r);
                case BoundBinaryOperatorKind.Subtraction:
                    return new BoundConstant((int)l - (int)r);
                case BoundBinaryOperatorKind.Multiplication:
                    return new BoundConstant((int)l * (int)r);
                case BoundBinaryOperatorKind.Division:
                    return new BoundConstant((int)l / (int)r);
                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)l & (int)r);
                    else
                        return new BoundConstant((bool)l & (bool)r);
                case BoundBinaryOperatorKind.BitwiseOr:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)l | (int)r);
                    else
                        return new BoundConstant((bool)l | (bool)r);
                case BoundBinaryOperatorKind.BitwiseXor:
                    if (left.Type == TypeSymbol.Int)
                        return new BoundConstant((int)l ^ (int)r);
                    else
                        return new BoundConstant((bool)l ^ (bool)r);
                case BoundBinaryOperatorKind.LogicalAnd:
                    return new BoundConstant((bool)l && (bool)r);
                case BoundBinaryOperatorKind.LogicalOr:
                    return new BoundConstant((bool)l || (bool)r);
                case BoundBinaryOperatorKind.Equals:
                    return new BoundConstant(Equals(l, r));
                case BoundBinaryOperatorKind.NotEquals:
                    return new BoundConstant(!Equals(l, r));
                case BoundBinaryOperatorKind.Less:
                    return new BoundConstant((int)l < (int)r);
                case BoundBinaryOperatorKind.LessOrEquals:
                    return new BoundConstant((int)l <= (int)r);
                case BoundBinaryOperatorKind.Greater:
                    return new BoundConstant((int)l > (int)r);
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return new BoundConstant((int)l >= (int)r);
                default:
                    throw new Exception($"Unexpected binary operator {op.Kind}");
            }
        }
    }
}
