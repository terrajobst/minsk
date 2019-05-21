using System.Collections.Immutable;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(ImmutableArray<(FunctionSymbol function, BoundBlockStatement body)> functionBodies, BoundGlobalScope previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<FunctionSymbol> functions, ImmutableArray<VariableSymbol> variables, ImmutableArray<BoundStatement> statements)
        {
            FunctionBodies = functionBodies;
            Previous = previous;
            Diagnostics = diagnostics;
            Functions = functions;
            Variables = variables;
            Statements = statements;
        }

        public ImmutableArray<(FunctionSymbol function, BoundBlockStatement body)> FunctionBodies { get; }
        public BoundGlobalScope Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<BoundStatement> Statements { get; }
    }
}
