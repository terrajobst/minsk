namespace Minsk.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Trivia
        WhitespaceTrivia,
        EndOfLineTrivia,
        SingleLineCommentTrivia,

        // Tokens
        BadToken,
        EndOfFileToken,
        NumberToken,
        StringToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        BangToken,
        EqualsToken,
        TildeToken,
        HatToken,
        AmpersandToken,
        AmpersandAmpersandToken,
        PipeToken,
        PipePipeToken,
        EqualsEqualsToken,
        BangEqualsToken,
        LessToken,
        LessOrEqualsToken,
        GreaterToken,
        GreaterOrEqualsToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        ColonToken,
        CommaToken,
        IdentifierToken,

        // Keywords
        BreakKeyword,
        ContinueKeyword,
        ElseKeyword,
        FalseKeyword,
        ForKeyword,
        FunctionKeyword,
        IfKeyword,
        LetKeyword,
        ReturnKeyword,
        ToKeyword,
        TrueKeyword,
        VarKeyword,
        WhileKeyword,
        DoKeyword,

        // Nodes
        CompilationUnit,
        FunctionDeclaration,
        GlobalStatement,
        Parameter,
        TypeClause,
        ElseClause,

        // Statements
        BlockStatement,
        VariableDeclaration,
        IfStatement,
        WhileStatement,
        DoWhileStatement,
        ForStatement,
        BreakStatement,
        ContinueStatement,
        ReturnStatement,
        ExpressionStatement,

        // Expressions
        LiteralExpression,
        NameExpression,
        UnaryExpression,
        BinaryExpression,
        ParenthesizedExpression,
        AssignmentExpression,
        CallExpression,
    }
}
