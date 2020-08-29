using System;

namespace Minsk.CodeAnalysis.Text
{
    public struct TextSpan : IEquatable<TextSpan>
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
            int length = end - start;
            return new TextSpan(start, length);
        }

        public bool OverlapsWith(TextSpan span)
        {
            return Start < span.End &&
                   End > span.Start;
        }

        public override string ToString() => $"{Start}..{End}";

        public override bool Equals(object? obj) => Equals(obj as TextSpan?);

        public override int GetHashCode() => HashCode.Combine(Start, Length);

        public static bool operator ==(TextSpan left, TextSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextSpan left, TextSpan right)
        {
            return !(left == right);
        }

        public bool Equals(TextSpan other) => other.Start == Start && other.Length == Length;
    }
}