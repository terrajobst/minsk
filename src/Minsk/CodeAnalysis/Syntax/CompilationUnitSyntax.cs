using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public CompilationUnitSyntax(ImmutableArray<StatementSyntax> statements, SyntaxToken endOfFileToken)
        {
            Statements = statements;
            EndOfFileToken = endOfFileToken;
        }

        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
        public ImmutableArray<StatementSyntax> Statements { get; }
        public SyntaxToken EndOfFileToken { get; }
    }
}