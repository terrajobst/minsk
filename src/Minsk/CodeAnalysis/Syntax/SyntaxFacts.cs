using System;
using System.Collections.Generic;

namespace Minsk.CodeAnalysis.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                case SyntaxKind.TildeToken:
                    return 6;

                default:
                    return 0;
            }
        }

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return 5;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 4;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
                    return 3;

                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AmpersandAmpersandToken:
                    return 2;

                case SyntaxKind.PipeToken:
                case SyntaxKind.PipePipeToken:
                case SyntaxKind.HatToken:
                    return 1;

                default:
                    return 0;
            }
        }

        public static bool IsComment(this SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineCommentTrivia ||
                   kind == SyntaxKind.MultiLineCommentTrivia;
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            switch (text)
            {
                case "break":
                    return SyntaxKind.BreakKeyword;
                case "continue":
                    return SyntaxKind.ContinueKeyword;
                case "else":
                    return SyntaxKind.ElseKeyword;
                case "false":
                    return SyntaxKind.FalseKeyword;
                case "for":
                    return SyntaxKind.ForKeyword;
                case "function":
                    return SyntaxKind.FunctionKeyword;
                case "if":
                    return SyntaxKind.IfKeyword;
                case "let":
                    return SyntaxKind.LetKeyword;
                case "return":
                    return SyntaxKind.ReturnKeyword;
                case "to":
                    return SyntaxKind.ToKeyword;
                case "true":
                    return SyntaxKind.TrueKeyword;
                case "var":
                    return SyntaxKind.VarKeyword;
                case "while":
                    return SyntaxKind.WhileKeyword;
                case "do":
                    return SyntaxKind.DoKeyword;
                default:
                    return SyntaxKind.IdentifierToken;
            }
        }

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds()
        {
            SyntaxKind[]? kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (SyntaxKind kind in kinds)
            {
                if (GetUnaryOperatorPrecedence(kind) > 0)
                {
                    yield return kind;
                }
            }
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds()
        {
            SyntaxKind[]? kinds = (SyntaxKind[])Enum.GetValues(typeof(SyntaxKind));
            foreach (SyntaxKind kind in kinds)
            {
                if (GetBinaryOperatorPrecedence(kind) > 0)
                {
                    yield return kind;
                }
            }
        }

        public static string? GetText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.PlusEqualsToken:
                    return "+=";
                case SyntaxKind.MinusToken:
                    return "-";
                case SyntaxKind.MinusEqualsToken:
                    return "-=";
                case SyntaxKind.StarToken:
                    return "*";
                case SyntaxKind.StarEqualsToken:
                    return "*=";
                case SyntaxKind.SlashToken:
                    return "/";
                case SyntaxKind.SlashEqualsToken:
                    return "/=";
                case SyntaxKind.BangToken:
                    return "!";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.TildeToken:
                    return "~";
                case SyntaxKind.LessToken:
                    return "<";
                case SyntaxKind.LessOrEqualsToken:
                    return "<=";
                case SyntaxKind.GreaterToken:
                    return ">";
                case SyntaxKind.GreaterOrEqualsToken:
                    return ">=";
                case SyntaxKind.AmpersandToken:
                    return "&";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.AmpersandEqualsToken:
                    return "&=";
                case SyntaxKind.PipeToken:
                    return "|";
                case SyntaxKind.PipeEqualsToken:
                    return "|=";
                case SyntaxKind.PipePipeToken:
                    return "||";
                case SyntaxKind.HatToken:
                    return "^";
                case SyntaxKind.HatEqualsToken:
                    return "^=";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.BangEqualsToken:
                    return "!=";
                case SyntaxKind.OpenParenthesisToken:
                    return "(";
                case SyntaxKind.CloseParenthesisToken:
                    return ")";
                case SyntaxKind.OpenBraceToken:
                    return "{";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.ColonToken:
                    return ":";
                case SyntaxKind.CommaToken:
                    return ",";
                case SyntaxKind.BreakKeyword:
                    return "break";
                case SyntaxKind.ContinueKeyword:
                    return "continue";
                case SyntaxKind.ElseKeyword:
                    return "else";
                case SyntaxKind.FalseKeyword:
                    return "false";
                case SyntaxKind.ForKeyword:
                    return "for";
                case SyntaxKind.FunctionKeyword:
                    return "function";
                case SyntaxKind.IfKeyword:
                    return "if";
                case SyntaxKind.LetKeyword:
                    return "let";
                case SyntaxKind.ReturnKeyword:
                    return "return";
                case SyntaxKind.ToKeyword:
                    return "to";
                case SyntaxKind.TrueKeyword:
                    return "true";
                case SyntaxKind.VarKeyword:
                    return "var";
                case SyntaxKind.WhileKeyword:
                    return "while";
                case SyntaxKind.DoKeyword:
                    return "do";
                default:
                    return null;
            }
        }

        public static bool IsTrivia(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.SkippedTextTrivia:
                case SyntaxKind.LineBreakTrivia:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsKeyword(this SyntaxKind kind)
        {
            return kind.ToString().EndsWith("Keyword");
        }

        public static bool IsToken(this SyntaxKind kind)
        {
            return !kind.IsTrivia() &&
                   (kind.IsKeyword() || kind.ToString().EndsWith("Token"));
        }
        public static SyntaxKind GetBinaryOperatorOfAssignmentOperator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusEqualsToken:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.MinusEqualsToken:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.StarEqualsToken:
                    return SyntaxKind.StarToken;
                case SyntaxKind.SlashEqualsToken:
                    return SyntaxKind.SlashToken;
                case SyntaxKind.AmpersandEqualsToken:
                    return SyntaxKind.AmpersandToken;
                case SyntaxKind.PipeEqualsToken:
                    return SyntaxKind.PipeToken;
                case SyntaxKind.HatEqualsToken:
                    return SyntaxKind.HatToken;
                default:
                    throw new Exception($"Unexpected syntax: '{kind}'");
            }
        }
    }
}