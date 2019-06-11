using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis
{
    public sealed class Diagnostic
    {
        public Diagnostic(SourceText text, TextSpan span, string message)
        {
            Text = text;
            Span = span;
            Message = message;
        }

        public SourceText Text { get; }
        public TextSpan Span { get; }
        public string Message { get; }

        public override string ToString() => Message;
    }
}