using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Minsk.CodeAnalysis.Lowering;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Binding
{
    internal static class BoundNodeFactory
    {

        public static BoundNopStatement Nop()
        {
            return new BoundNopStatement();
        }

        public static BoundLabelStatement Label(BoundLabel label)
        {
            return new BoundLabelStatement(label);
        }

        public static BoundLiteralExpression Literal(object literal)
        {
            Debug.Assert(literal is string || literal is bool || literal is int);

            return new BoundLiteralExpression(literal);
        }

        public static BoundBlockStatement Block(params BoundStatement[] statements)
        {
            return new BoundBlockStatement(ImmutableArray.Create(statements));
        }

        public static BoundGotoStatement Goto(BoundLabelStatement label)
        {
            return new BoundGotoStatement(label.Label);
        }

        public static BoundGotoStatement Goto(BoundLabel label)
        {
            return new BoundGotoStatement(label);
        }

        public static BoundConditionalGotoStatement GotoIf(BoundLabelStatement label, BoundExpression condition, bool jumpIfTrue)
        {
            return new BoundConditionalGotoStatement(label.Label, condition, jumpIfTrue);
        }

        public static BoundConditionalGotoStatement GotoTrue(BoundLabelStatement label, BoundExpression condition)
            => GotoIf(label, condition, jumpIfTrue: true);

        public static BoundConditionalGotoStatement GotoFalse(BoundLabelStatement label, BoundExpression condition)
            => GotoIf(label, condition, jumpIfTrue: false);

        public static BoundVariableDeclaration VariableDeclaration(VariableSymbol symbol, BoundExpression initializer)
        {
            return new BoundVariableDeclaration(symbol, initializer);
        }

        public static BoundVariableExpression Variable(BoundVariableDeclaration variable)
        {
            return new BoundVariableExpression(variable.Variable);
        }

        public static BoundVariableDeclaration ConstantDeclaration(string name, BoundExpression initializer)
            => VariableDeclarationInternal(name, initializer, initializer.Type, isReadOnly: true);

        public static BoundVariableDeclaration VariableDeclaration(string name, BoundExpression initializer)
            => VariableDeclarationInternal(name, initializer, initializer.Type, isReadOnly: false);

        public static BoundVariableDeclaration VariableDeclarationInternal(string name, BoundExpression initializer, TypeSymbol? type, bool isReadOnly)
        {
            var symbol = Local(name, type ?? initializer.Type, isReadOnly, initializer.ConstantValue);
            return new BoundVariableDeclaration(symbol, initializer);
        }

        public static LocalVariableSymbol Local(string name, TypeSymbol type, bool isReadOnly = true, BoundConstant? constant = null)
        {
            return new LocalVariableSymbol(name, isReadOnly, type, constant);
        }

        public static BoundBinaryExpression Binary(BoundExpression left, SyntaxKind kind, BoundExpression right)
        {
            var op = BoundBinaryOperator.Bind(kind, left.Type, right.Type)!;
            return new BoundBinaryExpression(left, op, right);
        }

        public static BoundBinaryExpression Add(BoundExpression left, BoundExpression right)
            => Binary(left, SyntaxKind.PlusToken, right);
        public static BoundBinaryExpression LessOrEqual(BoundExpression left, BoundExpression right)
            => Binary(left, SyntaxKind.LessOrEqualsToken, right);

        public static BoundWhileStatement While(BoundExpression condition, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
        {
            return new BoundWhileStatement(condition, body, breakLabel, continueLabel);
        }

        public static BoundExpressionStatement Increment(BoundVariableExpression variable)
        {
            var increment = Add(variable, Literal(1));
            var incrementAssign = new BoundAssignmentExpression(variable.Variable, increment);
            return new BoundExpressionStatement(incrementAssign);
        }
    }
}
