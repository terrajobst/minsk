namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class CompoundAssignmentExpressionSyntax : ExpressionSyntax
    {
        public CompoundAssignmentExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken operatorToken, ExpressionSyntax expression)
            : base(syntaxTree)
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