using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis
{
    public sealed class Diagnostic
    {
        private Diagnostic(bool isError, TextLocation location, string message)
        {
            IsError = isError;
            Location = location;
            Message = message;
            IsWarning = !IsError;
        }

        public bool IsError { get; }
        public TextLocation Location { get; }
        public string Message { get; }
        public bool IsWarning { get; }

        public override string ToString() => Message;

        public static Diagnostic Error(TextLocation location, string message)
        {
            return new Diagnostic(isError: true, location, message);
        }

        public static Diagnostic Warning(TextLocation location, string message)
        {
            return new Diagnostic(isError: false, location, message);
        }
    }
}