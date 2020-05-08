namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundNopStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
    }
}
