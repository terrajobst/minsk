using System;
using System.Diagnostics.CodeAnalysis;

namespace Minsk.CodeAnalysis.Text
{
    public struct TextLocation : IEquatable<TextLocation>
    {
        public TextLocation(SourceText text, TextSpan span)
        {
            Text = text;
            Span = span;
        }

        public SourceText Text { get; }
        public TextSpan Span { get; }

        public string FileName => Text.FileName;
        public int StartLine => Text.GetLineIndex(Span.Start);
        public int StartCharacter => Span.Start - Text.Lines[StartLine].Start;
        public int EndLine => Text.GetLineIndex(Span.End);
        public int EndCharacter => Span.End - Text.Lines[EndLine].Start;

        public override bool Equals(object? obj) =>
            Equals(obj as TextLocation?);

        public bool Equals([AllowNull] TextLocation other) =>
            other.Text.Equals(Text) && other.Span.Equals(Span);

        public override int GetHashCode() =>
            HashCode.Combine(Text, Span);

        public static bool operator ==(TextLocation left, TextLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextLocation left, TextLocation right)
        {
            return !(left == right);
        }
    }
}