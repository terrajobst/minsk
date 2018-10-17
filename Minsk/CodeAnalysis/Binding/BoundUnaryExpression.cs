using System;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundUnaryExpression(BoundUnaryOperator op, BoundExpression operand)
        {
            Op = op;
            Operand = operand;
        }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override Type Type => Op.Type;
        public BoundUnaryOperator Op { get; }
        public BoundExpression Operand { get; }
    }
}
