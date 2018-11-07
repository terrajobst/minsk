using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope(BoundGlobalScope previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, BoundExpression expression)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Expression = expression;
        }

        public BoundGlobalScope Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public BoundExpression Expression { get; }
    }
}
