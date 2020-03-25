namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class GlobalStatementSyntax : MemberSyntax
    {
        public GlobalStatementSyntax(SyntaxTree syntaxTree, StatementSyntax statement)
            : base(syntaxTree)
        {
            Statement = statement;
        }

        public override SyntaxKind Kind => SyntaxKind.GlobalStatement;
        public StatementSyntax Statement { get; }
    }
}