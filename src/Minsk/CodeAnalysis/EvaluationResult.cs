using System.Collections.Immutable;
using System.Linq;

namespace Minsk.CodeAnalysis
{
    public sealed class EvaluationResult
    {
        public EvaluationResult(ImmutableArray<Diagnostic> diagnostics, object? value)
        {
            Diagnostics = diagnostics;
            Value = value;
            ErrorDiagnostics = Diagnostics.Where(d => d.IsError).ToImmutableArray();
            WarningDiagnostics = Diagnostics.Where(d => d.IsWarning).ToImmutableArray();
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<Diagnostic> ErrorDiagnostics { get; }
        public ImmutableArray<Diagnostic> WarningDiagnostics { get; }
        public object? Value { get; }
    }
}