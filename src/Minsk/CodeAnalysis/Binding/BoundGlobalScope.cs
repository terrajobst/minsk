using System.Collections.Immutable;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope previous, BoundScope scope, ImmutableArray<(FunctionSymbol function, BoundBlockStatement body)> functionBodies, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<BoundStatement> statements)
        {
            Previous = previous;
            Scope = scope;
            FunctionBodies = functionBodies;
            Diagnostics = diagnostics;
            Statements = statements;
        }

        public BoundGlobalScope Previous { get; }
        public BoundScope Scope { get; }
        public ImmutableArray<(FunctionSymbol function, BoundBlockStatement body)> FunctionBodies { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<BoundStatement> Statements { get; }
    }
}
