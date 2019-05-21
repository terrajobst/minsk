using System.Collections.Immutable;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundScope scope, ImmutableArray<(FunctionSymbol function, BoundBlockStatement body)> functionBodies, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<BoundStatement> statements)
        {
            Scope = scope;
            FunctionBodies = functionBodies;
            Diagnostics = diagnostics;
            Statements = statements;
        }

        public BoundScope Scope { get; }
        public ImmutableArray<(FunctionSymbol function, BoundBlockStatement body)> FunctionBodies { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<BoundStatement> Statements { get; }
    }
}
