namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundGotoStatement : BoundStatement
    {
        public BoundGotoStatement(BoundLabel label)
        {
            Label = label;
        }

        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
        public BoundLabel Label { get; }
    }
}
