using System;

namespace Minsk.CodeAnalysis.Symbols
{
    public sealed class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, bool isReadOnly, Type type)
            : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
        }

        public override SymbolKind Kind => SymbolKind.Variable;
        public bool IsReadOnly { get; }
        public Type Type { get; }
    }
}