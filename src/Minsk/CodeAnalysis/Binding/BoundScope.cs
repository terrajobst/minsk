using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        private Dictionary<string, Symbol> _symbols;

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
        }

        public BoundScope Parent { get; }

        public bool TryDeclareVariable(VariableSymbol variable)
            => TryDeclareSymbol(variable);

        public bool TryDeclareFunction(FunctionSymbol function)
            => TryDeclareSymbol(function);

        private bool TryDeclareSymbol<TSymbol>(TSymbol symbol)
            where TSymbol : Symbol
        {
            if (_symbols == null)
                _symbols = new Dictionary<string, Symbol>();
            else if (_symbols.ContainsKey(symbol.Name))
                return false;

            _symbols.Add(symbol.Name, symbol);
            return true;
        }

        public bool TryLookupVariable(string name, out VariableSymbol variable)
            => TryLookupSymbol(name, out variable);

        public bool TryLookupFunction(string name, out FunctionSymbol function)
            => TryLookupSymbol(name, out function);

        private bool TryLookupSymbol<TSymbol>(string name, out TSymbol symbol)
            where TSymbol : Symbol
        {
            symbol = null;

            if (_symbols != null && _symbols.TryGetValue(name, out var declaredSymbol))
            {
                if (declaredSymbol is TSymbol matchingSymbol)
                {
                    symbol = matchingSymbol;
                    return true;
                }

                return false;
            }

            if (Parent == null)
                return false;

            return Parent.TryLookupSymbol(name, out symbol);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
            => GetDeclaredSymbols<VariableSymbol>();

        public ImmutableArray<FunctionSymbol> GetDeclaredFunctions()
            => GetDeclaredSymbols<FunctionSymbol>();

        private ImmutableArray<TSymbol> GetDeclaredSymbols<TSymbol>()
            where TSymbol : Symbol
        {
            if (_symbols == null)
                return ImmutableArray<TSymbol>.Empty;

            return _symbols.Values.OfType<TSymbol>().ToImmutableArray();
        }
    }
}
