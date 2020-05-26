using System;

namespace Minsk.CodeAnalysis.Text
{
    public struct TextSpan
    {
        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public static TextSpan FromBounds(int start, int end)
        {
            var length = end - start;
            return new TextSpan(start, length);
        }

        public bool OverlapsWith(TextSpan span)
        {
            return Start < span.End &&
                   End > span.Start;
        }

        public override string ToString() => $"{Start}..{End}";
    }
}