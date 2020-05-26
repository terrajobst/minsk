using System.Collections.Immutable;
using System.Linq;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram(BoundProgram? previous,
                            ImmutableArray<Diagnostic> diagnostics,
                            FunctionSymbol? mainFunction,
                            FunctionSymbol? scriptFunction,
                            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Functions = functions;
            ErrorDiagnostics = Diagnostics.Where(d => d.IsError).ToImmutableArray();
            WarningDiagnostics = Diagnostics.Where(d => d.IsWarning).ToImmutableArray();
        }

        public BoundProgram? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<Diagnostic> ErrorDiagnostics { get; }
        public ImmutableArray<Diagnostic> WarningDiagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }
    }
}
