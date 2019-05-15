namespace Minsk.CodeAnalysis.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type)
            : base(name, isReadOnly, type)
        {
        }

        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}