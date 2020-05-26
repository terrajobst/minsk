namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class CompoundAssignmentExpressionSyntax : ExpressionSyntax
    {
        public CompoundAssignmentExpressionSyntax(SyntaxToken identifierToken, SyntaxToken operatorToken, ExpressionSyntax expression)
        {
            IdentifierToken = identifierToken;
            OperatorToken = operatorToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.CompoundAssignmentExpression;
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken OperatorToken { get; }
        public ExpressionSyntax Expression { get; }
    }
}