namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundEndTryStatement : BoundStatement
    {
        public BoundEndTryStatement()
        {
        }

        public override BoundNodeKind Kind => BoundNodeKind.EndTryStatement;
    }
}
