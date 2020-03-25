namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class ElseClauseSyntax : SyntaxNode
    {
        public ElseClauseSyntax(SyntaxTree syntaxTree, SyntaxToken elseKeyword, StatementSyntax elseStatement)
            : base(syntaxTree)
        {
            ElseKeyword = elseKeyword;
            ElseStatement = elseStatement;
        }

        public override SyntaxKind Kind => SyntaxKind.ElseClause;
        public SyntaxToken ElseKeyword { get; }
        public StatementSyntax ElseStatement { get; }
    }
}