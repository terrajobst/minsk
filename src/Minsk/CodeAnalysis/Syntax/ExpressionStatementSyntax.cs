namespace Minsk.CodeAnalysis.Syntax
{
    public sealed partial class ExpressionStatementSyntax : StatementSyntax
    {
        internal ExpressionStatementSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;
        public ExpressionSyntax Expression { get; }
    }
}