using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis
{
    public sealed class Diagnostic
    {
        private Diagnostic(TextLocation location, string message, bool isError)
        {
            Location = location;
            Message = message;
            IsError = isError;
            IsWarning = !IsError;
        }

        public TextLocation Location { get; }
        public string Message { get; }
        public bool IsError { get; }
        public bool IsWarning { get; }

        public override string ToString() => Message;

        public static Diagnostic Error(TextLocation location, string message)
        {
            return new Diagnostic(location, message, isError: true);
        }

        public static Diagnostic Warning(TextLocation location, string message)
        {
            return new Diagnostic(location, message, isError: false);
        }
    }
}