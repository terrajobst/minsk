namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundTryCatchStatement : BoundStatement
    {

        public BoundTryCatchStatement(BoundStatement tryBody, BoundStatement catchBody)
        {
            TryBody = tryBody;
            CatchBody = catchBody;
        }

        public override BoundNodeKind Kind => BoundNodeKind.TryCatchStatement;
        public BoundStatement TryBody { get; }
        public BoundStatement CatchBody { get; }
    }
}