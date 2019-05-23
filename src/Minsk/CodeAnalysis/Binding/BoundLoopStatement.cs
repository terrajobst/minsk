namespace Minsk.CodeAnalysis.Binding
{
    internal abstract class BoundLoopStatement : BoundStatement
    {
        protected BoundLoopStatement(BoundStatement body, BoundLabel bodyLabel, BoundLabel breakLabel, BoundLabel continueLabel)
        {
            Body = body;
            BodyLabel = bodyLabel;
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }

        public BoundStatement Body { get; }
        public BoundLabel BodyLabel { get; }
        public BoundLabel BreakLabel { get; }
        public BoundLabel ContinueLabel { get; }
    }
}
