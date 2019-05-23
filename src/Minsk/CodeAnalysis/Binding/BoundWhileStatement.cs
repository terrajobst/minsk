namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundWhileStatement : BoundLoopStatement
    {
        public BoundWhileStatement(BoundExpression condition, BoundStatement body, BoundLabel bodyLabel, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(body, bodyLabel, breakLabel, continueLabel)
        {
            Condition = condition;
        }

        public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
        public BoundExpression Condition { get; }
    }
}
