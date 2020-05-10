using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public SyntaxTree SyntaxTree { get; }

        public abstract SyntaxKind Kind { get; }

        public virtual TextSpan Span
        {
            get
            {
                var first = GetChildren().First().Span;
                var last = GetChildren().Last().Span;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public TextLocation Location => new TextLocation(SyntaxTree.Text, Span);

        public abstract IEnumerable<SyntaxNode> GetChildren();

        public SyntaxToken GetLastToken()
        {
            if (this is SyntaxToken token)
                return token;

            // A syntax node should always contain at least 1 token.
            return GetChildren().Last().GetLastToken();
        }

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        private static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            var isToConsole = writer == Console.Out;
            var marker = isLast ? "└──" : "├──";

            if (isToConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(indent);
            writer.Write(marker);

            if (isToConsole)
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

            writer.Write(node.Kind);

            if (node is SyntaxToken t && t.Value != null)
            {
                writer.Write(" ");
                writer.Write(t.Value);
            }

            if (isToConsole)
                Console.ResetColor();

            writer.WriteLine();

            indent += isLast ? "   " : "│  ";

            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
                PrettyPrint(writer, child, indent, child == lastChild);
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}