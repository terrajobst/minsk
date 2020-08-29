using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax
{
    public abstract class SeparatedSyntaxList
    {
        private protected SeparatedSyntaxList()
        {
        }

        public abstract ImmutableArray<SyntaxNode> GetWithSeparators();
    }

    public sealed class SeparatedSyntaxList<T> : SeparatedSyntaxList, IEnumerable<T>
        where T : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxNode> _nodesAndSeparators;

        internal SeparatedSyntaxList(ImmutableArray<SyntaxNode> nodesAndSeparators)
        {
            _nodesAndSeparators = nodesAndSeparators;
        }

        public int Count => (_nodesAndSeparators.Length + 1) / 2;

        public T this[int index] => (T)_nodesAndSeparators[index * 2];

        public SyntaxToken GetSeparator(int index)
        {
            if (index < 0 || index >= Count - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (SyntaxToken)_nodesAndSeparators[index * 2 + 1];
        }

        public override ImmutableArray<SyntaxNode> GetWithSeparators() => _nodesAndSeparators;

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}