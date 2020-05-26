namespace Minsk.CodeAnalysis.Syntax
{
    internal sealed partial class ContinueStatementSyntax : StatementSyntax
    {
        public ContinueStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
            : base(syntaxTree)
        {
            Keyword = keyword;
        }

        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;
        public SyntaxToken Keyword { get; }
    }
}