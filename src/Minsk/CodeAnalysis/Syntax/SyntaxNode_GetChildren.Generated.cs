using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax
{
    partial class AssignmentExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
            yield return EqualsToken;
            yield return Expression;
        }
    }
    partial class BinaryExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }
    partial class BlockStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;
            foreach (var child in Statements)
                yield return child;
            yield return CloseBraceToken;
        }
    }
    partial class BreakStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
        }
    }
    partial class CallExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return OpenParenthesisToken;
            foreach (var child in Arguments.GetWithSeparators())
                yield return child;
            yield return CloseParenthesisToken;
        }
    }
    partial class CompilationUnitSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var child in Members)
                yield return child;
            yield return EndOfFileToken;
        }
    }
    partial class ContinueStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
        }
    }
    partial class DoWhileStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DoKeyword;
            yield return Body;
            yield return WhileKeyword;
            yield return Condition;
        }
    }
    partial class ElseClauseSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ElseKeyword;
            yield return ElseStatement;
        }
    }
    partial class ExpressionStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }
    }
    partial class ForStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            yield return Identifier;
            yield return EqualsToken;
            yield return LowerBound;
            yield return ToKeyword;
            yield return UpperBound;
            yield return Body;
        }
    }
    partial class FunctionDeclarationSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return FunctionKeyword;
            yield return Identifier;
            yield return OpenParenthesisToken;
            foreach (var child in Parameters.GetWithSeparators())
                yield return child;
            yield return CloseParenthesisToken;
            yield return Type;
            yield return Body;
        }
    }
    partial class GlobalStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Statement;
        }
    }
    partial class IfStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IfKeyword;
            yield return Condition;
            yield return ThenStatement;
            yield return ElseClause;
        }
    }
    partial class LiteralExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
        }
    }
    partial class NameExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
        }
    }
    partial class ParameterSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return Type;
        }
    }
    partial class ParenthesizedExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenthesisToken;
            yield return Expression;
            yield return CloseParenthesisToken;
        }
    }
    partial class ReturnStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ReturnKeyword;
            yield return Expression;
        }
    }
    partial class TypeClauseSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ColonToken;
            yield return Identifier;
        }
    }
    partial class UnaryExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OperatorToken;
            yield return Operand;
        }
    }
    partial class VariableDeclarationSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
            yield return Identifier;
            yield return TypeClause;
            yield return EqualsToken;
            yield return Initializer;
        }
    }
    partial class WhileStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WhileKeyword;
            yield return Condition;
            yield return Body;
        }
    }
}
