using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.CodeAnalysis.Lowering
{
    // TODO: Consider creating a BoundNodeFactory to construct nodes to make lowering easier to read.
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;

        private Lowerer()
        {
        }

        private BoundLabel GenerateLabel()
        {
            var name = $"Label{++_labelCount}";
            return new BoundLabel(name);
        }

        public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return RemoveDeadCode(Flatten(function, result));
        }

        private static BoundBlockStatement Flatten(FunctionSymbol function, BoundStatement statement)
        {
            var builder = ImmutableArray.CreateBuilder<BoundStatement>();
            var stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is BoundBlockStatement block)
                {
                    foreach (var s in block.Statements.Reverse())
                        stack.Push(s);
                }
                else
                {
                    builder.Add(current);
                }
            }

            if (function.Type == TypeSymbol.Void)
            {
                if (builder.Count == 0 || CanFallThrough(builder.Last()))
                {
                    builder.Add(new BoundReturnStatement(null));
                }
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }

        private static bool CanFallThrough(BoundStatement boundStatement)
        {
            return boundStatement.Kind != BoundNodeKind.ReturnStatement &&
                   boundStatement.Kind != BoundNodeKind.GotoStatement;
        }

        private static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
        {
            var controlFlow = ControlFlowGraph.Create(node);
            var reachableStatements = new HashSet<BoundStatement>(
                controlFlow.Blocks.SelectMany(b => b.Statements));

            var builder = node.Statements.ToBuilder();
            for (int i = builder.Count - 1; i >= 0; i--)
            {
                if (!reachableStatements.Contains(builder[i]))
                    builder.RemoveAt(i);
            }

            return new BoundBlockStatement(builder.ToImmutable());
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseStatement == null)
            {
                // if <condition>
                //      <then>
                //
                // ---->
                //
                // gotoFalse <condition> end
                // <then>
                // end:

                var endLabel = Label();
                var result = Block(GotoFalse(endLabel, node.Condition),
                                   node.ThenStatement,
                                   endLabel);

                return RewriteStatement(result);
            }
            else
            {
                // if <condition>
                //      <then>
                // else
                //      <else>
                //
                // ---->
                //
                // gotoFalse <condition> else
                // <then>
                // goto end
                // else:
                // <else>
                // end:

                var elseLabel = Label();
                var endLabel = Label();
                var result = Block(GotoFalse(elseLabel, node.Condition),
                                   node.ThenStatement,
                                   Goto(endLabel),
                                   elseLabel,
                                   node.ElseStatement,
                                   endLabel);

                return RewriteStatement(result);
            }
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            // while <condition>
            //      <body>
            //
            // ----->
            //
            // goto continue
            // body:
            // <body>
            // continue:
            // gotoTrue <condition> body
            // break:

            var bodyLabel = Label();
            var result = Block(Goto(node.ContinueLabel),
                               bodyLabel,
                               node.Body,
                               Label(node.ContinueLabel),
                               GotoTrue(bodyLabel, node.Condition),
                               Label(node.BreakLabel));

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteDoWhileStatement(BoundDoWhileStatement node)
        {
            // do
            //      <body>
            // while <condition>
            //
            // ----->
            //
            // body:
            // <body>
            // continue:
            // gotoTrue <condition> body
            // break:

            var bodyLabel = Label();
            var result = Block(bodyLabel,
                               node.Body,
                               Label(node.ContinueLabel),
                               GotoTrue(bodyLabel, node.Condition),
                               Label(node.BreakLabel));

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            // for <var> = <lower> to <upper>
            //      <body>
            //
            // ---->
            //
            // {
            //      var <var> = <lower>
            //      let upperBound = <upper>
            //      while (<var> <= upperBound)
            //      {
            //          <body>
            //          continue:
            //          <var> = <var> + 1
            //      }
            // }

            var lowerBound = VariableDeclaration(node.Variable, node.LowerBound);
            var upperBound = ConstantDeclaration("upperBound", node.UpperBound, TypeSymbol.Int);
            var result = Block(lowerBound,
                               upperBound,
                               While(CompareLessOrEqual(Variable(lowerBound), Variable(upperBound)),
                               Block(node.Body,
                                     Label(node.ContinueLabel),
                                     Increment(Variable(lowerBound))
                               ),
                               node.BreakLabel));

            return RewriteStatement(result);
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition.ConstantValue != null)
            {
                var condition = (bool)node.Condition.ConstantValue.Value;
                condition = node.JumpIfTrue ? condition : !condition;
                if (condition)
                    return RewriteStatement(Goto(node.Label));
                else
                    return RewriteStatement(Nop());
            }

            return base.RewriteConditionalGotoStatement(node);
        }

        #region BoundNodeFactory

        private BoundNopStatement Nop()
        {
            return new BoundNopStatement();
        }

        private BoundLabelStatement Label()
        {
            var label = GenerateLabel();
            return new BoundLabelStatement(label);
        }

        private BoundLabelStatement Label(BoundLabel label)
        {
            return new BoundLabelStatement(label);
        }

        private BoundLiteralExpression Literal(object literal)
        {
            Debug.Assert(literal is string || literal is bool || literal is int);

            return new BoundLiteralExpression(literal);
        }

        private BoundBlockStatement Block(params BoundStatement[] stmts)
        {
            return new BoundBlockStatement(ImmutableArray.Create(stmts));
        }

        private BoundGotoStatement Goto(BoundLabelStatement label)
        {
            return new BoundGotoStatement(label.Label);
        }

        private BoundGotoStatement Goto(BoundLabel label)
        {
            return new BoundGotoStatement(label);
        }

        private BoundConditionalGotoStatement Goto(BoundLabelStatement label, BoundExpression condition, bool jumpIfTrue)
        {
            return new BoundConditionalGotoStatement(label.Label, condition, jumpIfTrue);
        }

        private BoundConditionalGotoStatement GotoTrue(BoundLabelStatement label, BoundExpression condition)
            => Goto(label, condition, jumpIfTrue: true);

        private BoundConditionalGotoStatement GotoFalse(BoundLabelStatement label, BoundExpression condition)
            => Goto(label, condition, jumpIfTrue: false);

        private BoundVariableDeclaration VariableDeclaration(VariableSymbol symbol, BoundExpression initExpr)
        {
            return new BoundVariableDeclaration(symbol, initExpr);
        }

        private BoundVariableExpression Variable(VariableSymbol symbol)
        {
            return new BoundVariableExpression(symbol);
        }

        private BoundVariableExpression Variable(BoundVariableDeclaration varDecl)
        {
            return new BoundVariableExpression(varDecl.Variable);
        }

        private BoundVariableDeclaration ConstantDeclaration(string name, BoundExpression initExpr, TypeSymbol? type = null)
            => VariableDeclarationInternal(name, initExpr, type, isReadOnly: true);

        private BoundVariableDeclaration VariableDeclaration(string name, BoundExpression initExpr, TypeSymbol? type = null)
            => VariableDeclarationInternal(name, initExpr, type, isReadOnly: false);

        private BoundVariableDeclaration VariableDeclarationInternal(string name, BoundExpression initExpr, TypeSymbol? type, bool isReadOnly)
        {
            var symbol = Symbol(name, type ?? initExpr.Type, isReadOnly, initExpr.ConstantValue);
            return new BoundVariableDeclaration(symbol, initExpr);
        }

        private LocalVariableSymbol Symbol(string name, TypeSymbol type, bool isReadOnly = true, BoundConstant? constant = null)
        {
            return new LocalVariableSymbol(name, isReadOnly, type, constant);
        }

        private BoundBinaryExpression BinaryExpr(BoundExpression lhs, SyntaxKind kind, BoundExpression rhs)
        {
            var op = BoundBinaryOperator.Bind(kind, lhs.Type, rhs.Type)!;
            return new BoundBinaryExpression(lhs, op, rhs);
        }

        private BoundUnaryExpression Not(BoundExpression expr) => UnaryExpr(SyntaxKind.BangToken, expr);
        private BoundUnaryExpression Negation(BoundExpression expr) => UnaryExpr(SyntaxKind.MinusToken, expr);

        private BoundUnaryExpression UnaryExpr(SyntaxKind kind, BoundExpression expr)
        {
            var op = BoundUnaryOperator.Bind(kind, expr.Type)!;
            return new BoundUnaryExpression(op, expr);
        }

        private BoundBinaryExpression CompareEqual(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.EqualsEqualsToken, rhs);
        private BoundBinaryExpression CompareNotEqual(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.BangEqualsToken, rhs);
        private BoundBinaryExpression Add(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.PlusToken, rhs);
        private BoundBinaryExpression Sub(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.MinusToken, rhs);
        private BoundBinaryExpression Mul(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.StarToken, rhs);
        private BoundBinaryExpression Div(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.SlashToken, rhs);
        private BoundBinaryExpression CompareGreater(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.GreaterToken, rhs);
        private BoundBinaryExpression CompareGreaterOrEqual(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.GreaterOrEqualsToken, rhs);
        private BoundBinaryExpression CompareLess(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.LessToken, rhs);
        private BoundBinaryExpression CompareLessOrEqual(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.LessOrEqualsToken, rhs);
        private BoundBinaryExpression Or(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.PipePipeToken, rhs);
        private BoundBinaryExpression And(BoundExpression lhs, BoundExpression rhs) => BinaryExpr(lhs, SyntaxKind.AmpersandAmpersandToken, rhs);

        private BoundWhileStatement While(BoundExpression condition, BoundStatement body, BoundLabel breakLabel)
        {
            var continueLabel = this.GenerateLabel();
            return new BoundWhileStatement(condition, body, breakLabel, continueLabel);
        }

        private BoundExpressionStatement Increment(BoundVariableExpression varExpr)
        {
            var incrByOne = Add(varExpr, Literal(1));
            var incrAssign = new BoundAssignmentExpression(varExpr.Variable, incrByOne);
            return new BoundExpressionStatement(incrAssign);
        }

        private BoundExpressionStatement Decrement(BoundVariableExpression varExpr)
        {
            var decrByOne = Sub(varExpr, Literal(1));
            var decrAssign = new BoundAssignmentExpression(varExpr.Variable, decrByOne);
            return new BoundExpressionStatement(decrAssign);
        }

        #endregion
    }
}