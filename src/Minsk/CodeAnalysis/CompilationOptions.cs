namespace Minsk.CodeAnalysis
{
    public sealed class CompilationOptions
    {
        public CompilationOptions(SourceCodeKind sourceCodeKind)
        {
            SourceCodeKind = sourceCodeKind;
        }

        public SourceCodeKind SourceCodeKind { get; }
    }
}
