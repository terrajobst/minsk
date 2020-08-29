using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

namespace Minsk.IO
{
    public static class TextWriterExtensions
    {
        private static bool IsConsole(this TextWriter writer)
        {
            if (writer == Console.Out)
            {
                return !Console.IsOutputRedirected;
            }

            if (writer == Console.Error)
            {
                return !Console.IsErrorRedirected && !Console.IsOutputRedirected; // Color codes are always output to Console.Out
            }

            if (writer is IndentedTextWriter iw && iw.InnerWriter.IsConsole())
            {
                return true;
            }

            return false;
        }

        private static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsole())
            {
                Console.ForegroundColor = color;
            }
        }

        private static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsole())
            {
                Console.ResetColor();
            }
        }

        public static void WriteKeyword(this TextWriter writer, SyntaxKind kind)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            string? text = SyntaxFacts.GetText(kind);
            Debug.Assert(kind.IsKeyword() && text != null);

            writer.WriteKeyword(text);
        }

        public static void WriteKeyword(this TextWriter writer, string text)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.SetForeground(ConsoleColor.Blue);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteIdentifier(this TextWriter writer, string text)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.SetForeground(ConsoleColor.DarkYellow);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteNumber(this TextWriter writer, string text)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.SetForeground(ConsoleColor.Cyan);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteString(this TextWriter writer, string text)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.SetForeground(ConsoleColor.Magenta);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteSpace(this TextWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WritePunctuation(" ");
        }

        public static void WritePunctuation(this TextWriter writer, SyntaxKind kind)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            string? text = SyntaxFacts.GetText(kind);
            Debug.Assert(text != null);

            writer.WritePunctuation(text);
        }

        public static void WritePunctuation(this TextWriter writer, string text)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.SetForeground(ConsoleColor.DarkGray);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            foreach (Diagnostic? diagnostic in diagnostics.Where(d => d.Location.Text == null))
            {
                ConsoleColor messageColor = diagnostic.IsWarning ? ConsoleColor.DarkYellow : ConsoleColor.DarkRed;
                writer.SetForeground(messageColor);
                writer.WriteLine(diagnostic.Message);
                writer.ResetColor();
            }

            foreach (Diagnostic? diagnostic in diagnostics.Where(d => d.Location.Text != null)
                                                  .OrderBy(d => d.Location.FileName)
                                                  .ThenBy(d => d.Location.Span.Start)
                                                  .ThenBy(d => d.Location.Span.Length))
            {
                SourceText? text = diagnostic.Location.Text;
                string? fileName = diagnostic.Location.FileName;
                int startLine = diagnostic.Location.StartLine + 1;
                int startCharacter = diagnostic.Location.StartCharacter + 1;
                int endLine = diagnostic.Location.EndLine + 1;
                int endCharacter = diagnostic.Location.EndCharacter + 1;

                TextSpan span = diagnostic.Location.Span;
                int lineIndex = text.GetLineIndex(span.Start);
                TextLine? line = text.Lines[lineIndex];

                writer.WriteLine();

                ConsoleColor messageColor = diagnostic.IsWarning ? ConsoleColor.DarkYellow : ConsoleColor.DarkRed;
                writer.SetForeground(messageColor);
                writer.Write($"{fileName}({startLine},{startCharacter},{endLine},{endCharacter}): ");
                writer.WriteLine(diagnostic);
                writer.ResetColor();

                TextSpan prefixSpan = TextSpan.FromBounds(line.Start, span.Start);
                TextSpan suffixSpan = TextSpan.FromBounds(span.End, line.End);

                string? prefix = text.ToString(prefixSpan);
                string? error = text.ToString(span);
                string? suffix = text.ToString(suffixSpan);

                writer.Write("    ");
                writer.Write(prefix);

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write(error);
                writer.ResetColor();

                writer.Write(suffix);

                writer.WriteLine();
            }

            writer.WriteLine();
        }
    }
}
