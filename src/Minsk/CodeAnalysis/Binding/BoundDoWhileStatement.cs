namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundDoWhileStatement : BoundLoopStatement
    {
        public BoundDoWhileStatement(BoundStatement body, BoundExpression condition, BoundLabel bodyLabel, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(body, bodyLabel, breakLabel, continueLabel)
        {
            Condition = condition;
        }

        public override BoundNodeKind Kind => BoundNodeKind.DoWhileStatement;
        public BoundExpression Condition { get; }
    }
}
