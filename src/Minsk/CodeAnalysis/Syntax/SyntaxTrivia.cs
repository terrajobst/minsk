namespace Minsk.CodeAnalysis.Syntax
{
    public sealed class SyntaxTrivia
    {
        public SyntaxTrivia(SyntaxKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public SyntaxKind Kind { get; }
        public string Text { get; }
    }
}
