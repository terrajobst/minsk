using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax
{
    internal sealed class Parser
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

        public Parser(SyntaxTree syntaxTree)
        {
            var tokens = new List<SyntaxToken>();
            var badTokens = new List<SyntaxToken>();

            var lexer = new Lexer(syntaxTree);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind == SyntaxKind.BadToken)
                {
                    badTokens.Add(token);
                }
                else
                {
                    if (badTokens.Count > 0)
                    {
                        var leadingTrivia = token.LeadingTrivia.ToBuilder();
                        var index = 0;

                        foreach (var badToken in badTokens)
                        {
                            foreach (var lt in badToken.LeadingTrivia)
                                leadingTrivia.Insert(index++, lt);

                            var trivia = new SyntaxTrivia(syntaxTree, SyntaxKind.SkippedTextTrivia, badToken.Position, badToken.Text);
                            leadingTrivia.Insert(index++, trivia);

                            foreach (var tt in badToken.TrailingTrivia)
                                leadingTrivia.Insert(index++, tt);
                        }

                        badTokens.Clear();
                        token =  new SyntaxToken(token.SyntaxTree, token.Kind, token.Position, token.Text, token.Value, leadingTrivia.ToImmutable(), token.TrailingTrivia);
                    }

                    tokens.Add(token);
                }
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private SyntaxToken Peek(int offset)
        {
            var index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];

            return _tokens[index];
        }

        private SyntaxToken Current => Peek(0);

        private SyntaxToken NextToken()
        {
            var current = Current;
            _position++;
            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kind);
            return new SyntaxToken(_syntaxTree, kind, Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var members = ParseMembers();
            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, members, endOfFileToken);
        }

        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            var members = ImmutableArray.CreateBuilder<MemberSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var startToken = Current;

                var member = ParseMember();
                members.Add(member);

                // If ParseMember() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                    NextToken();
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
        {
            if (Current.Kind == SyntaxKind.FunctionKeyword)
                return ParseFunctionDeclaration();

            return ParseGlobalStatement();
        }

        private MemberSyntax ParseFunctionDeclaration()
        {
            var functionKeyword = MatchToken(SyntaxKind.FunctionKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var parameters = ParseParameterList();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            var type = ParseOptionalTypeClause();
            var body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, functionKeyword, identifier, openParenthesisToken, parameters, closeParenthesisToken, type, body);
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            var parseNextParameter = true;
            while (parseNextParameter &&
                   Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var parameter = ParseParameter();
                nodesAndSeparators.Add(parameter);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextParameter = false;
                }
            }

            return new SeparatedSyntaxList<ParameterSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ParameterSyntax ParseParameter()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var type = ParseTypeClause();
            return new ParameterSyntax(_syntaxTree, identifier, type);
        }

        private MemberSyntax ParseGlobalStatement()
        {
            var statement = ParseStatement();
            return new GlobalStatementSyntax(_syntaxTree, statement);
        }

        private StatementSyntax ParseStatement()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.LetKeyword:
                case SyntaxKind.VarKeyword:
                    return ParseVariableDeclaration();
                case SyntaxKind.IfKeyword:
                    return ParseIfStatement();
                case SyntaxKind.WhileKeyword:
                    return ParseWhileStatement();
                case SyntaxKind.DoKeyword:
                    return ParseDoWhileStatement();
                case SyntaxKind.ForKeyword:
                    return ParseForStatement();
                case SyntaxKind.BreakKeyword:
                    return ParseBreakStatement();
                case SyntaxKind.ContinueKeyword:
                    return ParseContinueStatement();
                case SyntaxKind.ReturnKeyword:
                    return ParseReturnStatement();
                default:
                    return ParseExpressionStatement();
            }
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                   Current.Kind != SyntaxKind.CloseBraceToken)
            {
                var startToken = Current;

                var statement = ParseStatement();
                statements.Add(statement);

                // If ParseStatement() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                    NextToken();
            }

            var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private StatementSyntax ParseVariableDeclaration()
        {
            var expected = Current.Kind == SyntaxKind.LetKeyword ? SyntaxKind.LetKeyword : SyntaxKind.VarKeyword;
            var keyword = MatchToken(expected);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var typeClause = ParseOptionalTypeClause();
            var equals = MatchToken(SyntaxKind.EqualsToken);
            var initializer = ParseExpression();
            return new VariableDeclarationSyntax(_syntaxTree, keyword, identifier, typeClause, equals, initializer);
        }

        private TypeClauseSyntax? ParseOptionalTypeClause()
        {
            if (Current.Kind != SyntaxKind.ColonToken)
                return null;

            return ParseTypeClause();
        }

        private TypeClauseSyntax ParseTypeClause()
        {
            var colonToken = MatchToken(SyntaxKind.ColonToken);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            return new TypeClauseSyntax(_syntaxTree, colonToken, identifier);
        }

        private StatementSyntax ParseIfStatement()
        {
            var keyword = MatchToken(SyntaxKind.IfKeyword);
            var condition = ParseExpression();
            var statement = ParseStatement();
            var elseClause = ParseOptionalElseClause();
            return new IfStatementSyntax(_syntaxTree, keyword, condition, statement, elseClause);
        }

        private ElseClauseSyntax? ParseOptionalElseClause()
        {
            if (Current.Kind != SyntaxKind.ElseKeyword)
                return null;

            var keyword = NextToken();
            var statement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, statement);
        }

        private StatementSyntax ParseWhileStatement()
        {
            var keyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseExpression();
            var body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree, keyword, condition, body);
        }

        private StatementSyntax ParseDoWhileStatement()
        {
            var doKeyword = MatchToken(SyntaxKind.DoKeyword);
            var body = ParseStatement();
            var whileKeyword = MatchToken(SyntaxKind.WhileKeyword);
            var condition = ParseExpression();
            return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, condition);
        }

        private StatementSyntax ParseForStatement()
        {
            var keyword = MatchToken(SyntaxKind.ForKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var equalsToken = MatchToken(SyntaxKind.EqualsToken);
            var lowerBound = ParseExpression();
            var toKeyword = MatchToken(SyntaxKind.ToKeyword);
            var upperBound = ParseExpression();
            var body = ParseStatement();
            return new ForStatementSyntax(_syntaxTree, keyword, identifier, equalsToken, lowerBound, toKeyword, upperBound, body);
        }

        private StatementSyntax ParseBreakStatement()
        {
            var keyword = MatchToken(SyntaxKind.BreakKeyword);
            return new BreakStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseContinueStatement()
        {
            var keyword = MatchToken(SyntaxKind.ContinueKeyword);
            return new ContinueStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseReturnStatement()
        {
            var keyword = MatchToken(SyntaxKind.ReturnKeyword);
            var keywordLine = _text.GetLineIndex(keyword.Span.Start);
            var currentLine = _text.GetLineIndex(Current.Span.Start);
            var isEof = Current.Kind == SyntaxKind.EndOfFileToken;
            var sameLine = !isEof && keywordLine == currentLine;
            var expression = sameLine ? ParseExpression() : null;
            return new ReturnStatementSyntax(_syntaxTree, keyword, expression);
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            var expression = ParseExpression();
            return new ExpressionStatementSyntax(_syntaxTree, expression);
        }

        private ExpressionSyntax ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        private ExpressionSyntax ParseAssignmentExpression()
        {
            if (Peek(0).Kind == SyntaxKind.IdentifierToken)
            {
                switch (Peek(1).Kind)
                {
                    case SyntaxKind.PlusEqualsToken:
                    case SyntaxKind.MinusEqualsToken:
                    case SyntaxKind.StarEqualsToken:
                    case SyntaxKind.SlashEqualsToken:
                    case SyntaxKind.AmpersandEqualsToken:
                    case SyntaxKind.PipeEqualsToken:
                    case SyntaxKind.HatEqualsToken:
                    case SyntaxKind.EqualsToken:
                        var identifierToken = NextToken();
                        var operatorToken = NextToken();
                        var right = ParseAssignmentExpression();
                        return new AssignmentExpressionSyntax(_syntaxTree, identifierToken, operatorToken, right);
                }

            }
            return ParseBinaryExpression();
        }

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseBinaryExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                var operatorToken = NextToken();
                var right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();

                case SyntaxKind.FalseKeyword:
                case SyntaxKind.TrueKeyword:
                    return ParseBooleanLiteral();

                case SyntaxKind.NumberToken:
                    return ParseNumberLiteral();

                case SyntaxKind.StringToken:
                    return ParseStringLiteral();

                case SyntaxKind.IdentifierToken:
                default:
                    return ParseNameOrCallExpression();
            }
        }

        private ExpressionSyntax ParseParenthesizedExpression()
        {
            var left = MatchToken(SyntaxKind.OpenParenthesisToken);
            var expression = ParseExpression();
            var right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(_syntaxTree, left, expression, right);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            var isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            var keywordToken = isTrue ? MatchToken(SyntaxKind.TrueKeyword) : MatchToken(SyntaxKind.FalseKeyword);
            return new LiteralExpressionSyntax(_syntaxTree, keywordToken, isTrue);
        }

        private ExpressionSyntax ParseNumberLiteral()
        {
            var numberToken = MatchToken(SyntaxKind.NumberToken);
            return new LiteralExpressionSyntax(_syntaxTree, numberToken);
        }

        private ExpressionSyntax ParseStringLiteral()
        {
            var stringToken = MatchToken(SyntaxKind.StringToken);
            return new LiteralExpressionSyntax(_syntaxTree, stringToken);
        }

        private ExpressionSyntax ParseNameOrCallExpression()
        {
            if (Peek(0).Kind == SyntaxKind.IdentifierToken && Peek(1).Kind == SyntaxKind.OpenParenthesisToken)
                return ParseCallExpression();

            return ParseNameExpression();
        }

        private ExpressionSyntax ParseCallExpression()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseArguments();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new CallExpressionSyntax(_syntaxTree, identifier, openParenthesisToken, arguments, closeParenthesisToken);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            var parseNextArgument = true;
            while (parseNextArgument &&
                   Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var expression = ParseExpression();
                nodesAndSeparators.Add(expression);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextArgument = false;
                }
            }

            return new SeparatedSyntaxList<ExpressionSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ExpressionSyntax ParseNameExpression()
        {
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(_syntaxTree, identifierToken);
        }
    }
}