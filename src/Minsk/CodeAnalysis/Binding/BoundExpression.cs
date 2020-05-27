using System;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.CodeAnalysis.Binding
{
    internal abstract class BoundExpression : BoundNode
    {
        protected BoundExpression(SyntaxNode syntax)
            : base(syntax)
        {
        }

        public abstract TypeSymbol Type { get; }
        public virtual BoundConstant? ConstantValue => null;
    }
}
