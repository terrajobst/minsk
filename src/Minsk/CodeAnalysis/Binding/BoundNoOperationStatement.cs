namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundNoOperationStatement : BoundStatement
    {
        private BoundNoOperationStatement()
        {
        }

        public static BoundNoOperationStatement Instance { get; } = new BoundNoOperationStatement();

        public override BoundNodeKind Kind => BoundNodeKind.NoOperationStatement;
    }
}
