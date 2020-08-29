using System;
using System.Collections.Generic;
using System.Diagnostics;
using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Symbols;

namespace Minsk.CodeAnalysis
{
    // TODO: Get rid of evaluator in favor of Emitter (see #113)
    internal sealed class Evaluator
    {
        private readonly BoundProgram _program;
        private readonly Dictionary<VariableSymbol, object> _globals;
        private readonly Dictionary<FunctionSymbol, BoundBlockStatement> _functions = new Dictionary<FunctionSymbol, BoundBlockStatement>();
        private readonly Stack<Dictionary<VariableSymbol, object>> _locals = new Stack<Dictionary<VariableSymbol, object>>();
        private Random? _random;

        private object? _lastValue;

        public Evaluator(BoundProgram program, Dictionary<VariableSymbol, object> variables)
        {
            _program = program;
            _globals = variables;
            _locals.Push(new Dictionary<VariableSymbol, object>());

            BoundProgram? current = program;
            while (current != null)
            {
                foreach (KeyValuePair<FunctionSymbol, BoundBlockStatement> kv in current.Functions)
                {
                    FunctionSymbol? function = kv.Key;
                    BoundBlockStatement? body = kv.Value;
                    _functions.Add(function, body);
                }

                current = current.Previous;
            }
        }

        public object? Evaluate()
        {
            FunctionSymbol? function = _program.MainFunction ?? _program.ScriptFunction;
            if (function == null)
            {
                return null;
            }

            BoundBlockStatement? body = _functions[function];
            return EvaluateStatement(body);
        }

        private object? EvaluateStatement(BoundBlockStatement body)
        {
            Dictionary<BoundLabel, int>? labelToIndex = new Dictionary<BoundLabel, int>();

            for (int i = 0; i < body.Statements.Length; i++)
            {
                if (body.Statements[i] is BoundLabelStatement l)
                {
                    labelToIndex.Add(l.Label, i + 1);
                }
            }

            int index = 0;

            while (index < body.Statements.Length)
            {
                BoundStatement? s = body.Statements[index];

                switch (s.Kind)
                {
                    case BoundNodeKind.NopStatement:
                        index++;
                        break;
                    case BoundNodeKind.VariableDeclaration:
                        EvaluateVariableDeclaration((BoundVariableDeclaration)s);
                        index++;
                        break;
                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement)s);
                        index++;
                        break;
                    case BoundNodeKind.GotoStatement:
                        BoundGotoStatement? gs = (BoundGotoStatement)s;
                        index = labelToIndex[gs.Label];
                        break;
                    case BoundNodeKind.ConditionalGotoStatement:
                        BoundConditionalGotoStatement? cgs = (BoundConditionalGotoStatement)s;
                        bool condition = (bool)EvaluateExpression(cgs.Condition)!;
                        if (condition == cgs.JumpIfTrue)
                        {
                            index = labelToIndex[cgs.Label];
                        }
                        else
                        {
                            index++;
                        }

                        break;
                    case BoundNodeKind.LabelStatement:
                        index++;
                        break;
                    case BoundNodeKind.ReturnStatement:
                        BoundReturnStatement? rs = (BoundReturnStatement)s;
                        _lastValue = rs.Expression == null ? null : EvaluateExpression(rs.Expression);
                        return _lastValue;
                    default:
                        throw new Exception($"Unexpected node {s.Kind}");
                }
            }

            return _lastValue;
        }

        private void EvaluateVariableDeclaration(BoundVariableDeclaration node)
        {
            object? value = EvaluateExpression(node.Initializer);
            Debug.Assert(value != null);

            _lastValue = value;
            Assign(node.Variable, value);
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node)
        {
            _lastValue = EvaluateExpression(node.Expression);
        }

        private object? EvaluateExpression(BoundExpression node)
        {
            if (node.ConstantValue != null)
            {
                return EvaluateConstantExpression(node);
            }

            return node.Kind switch
            {
                BoundNodeKind.VariableExpression => EvaluateVariableExpression((BoundVariableExpression)node),
                BoundNodeKind.AssignmentExpression => EvaluateAssignmentExpression((BoundAssignmentExpression)node),
                BoundNodeKind.UnaryExpression => EvaluateUnaryExpression((BoundUnaryExpression)node),
                BoundNodeKind.BinaryExpression => EvaluateBinaryExpression((BoundBinaryExpression)node),
                BoundNodeKind.CallExpression => EvaluateCallExpression((BoundCallExpression)node),
                BoundNodeKind.ConversionExpression => EvaluateConversionExpression((BoundConversionExpression)node),
                _ => throw new Exception($"Unexpected node {node.Kind}"),
            };
        }

        private static object EvaluateConstantExpression(BoundExpression n)
        {
            Debug.Assert(n.ConstantValue != null);

            return n.ConstantValue.Value;
        }

        private object EvaluateVariableExpression(BoundVariableExpression v)
        {
            if (v.Variable.Kind == SymbolKind.GlobalVariable)
            {
                return _globals[v.Variable];
            }
            else
            {
                Dictionary<VariableSymbol, object>? locals = _locals.Peek();
                return locals[v.Variable];
            }
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression a)
        {
            object? value = EvaluateExpression(a.Expression);
            Debug.Assert(value != null);

            Assign(a.Variable, value);
            return value;
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression u)
        {
            object? operand = EvaluateExpression(u.Operand);

            Debug.Assert(operand != null);

            return u.Op.Kind switch
            {
                BoundUnaryOperatorKind.Identity => (int)operand,
                BoundUnaryOperatorKind.Negation => -(int)operand,
                BoundUnaryOperatorKind.LogicalNegation => !(bool)operand,
                BoundUnaryOperatorKind.OnesComplement => ~(int)operand,
                _ => throw new Exception($"Unexpected unary operator {u.Op}"),
            };
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression b)
        {
            object? left = EvaluateExpression(b.Left);
            object? right = EvaluateExpression(b.Right);

            Debug.Assert(left != null && right != null);

            switch (b.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (b.Type == TypeSymbol.Int)
                    {
                        return (int)left + (int)right;
                    }
                    else
                    {
                        return (string)left + (string)right;
                    }

                case BoundBinaryOperatorKind.Subtraction:
                    return (int)left - (int)right;
                case BoundBinaryOperatorKind.Multiplication:
                    return (int)left * (int)right;
                case BoundBinaryOperatorKind.Division:
                    return (int)left / (int)right;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    if (b.Type == TypeSymbol.Int)
                    {
                        return (int)left & (int)right;
                    }
                    else
                    {
                        return (bool)left & (bool)right;
                    }

                case BoundBinaryOperatorKind.BitwiseOr:
                    if (b.Type == TypeSymbol.Int)
                    {
                        return (int)left | (int)right;
                    }
                    else
                    {
                        return (bool)left | (bool)right;
                    }

                case BoundBinaryOperatorKind.BitwiseXor:
                    if (b.Type == TypeSymbol.Int)
                    {
                        return (int)left ^ (int)right;
                    }
                    else
                    {
                        return (bool)left ^ (bool)right;
                    }

                case BoundBinaryOperatorKind.LogicalAnd:
                    return (bool)left && (bool)right;
                case BoundBinaryOperatorKind.LogicalOr:
                    return (bool)left || (bool)right;
                case BoundBinaryOperatorKind.Equals:
                    return Equals(left, right);
                case BoundBinaryOperatorKind.NotEquals:
                    return !Equals(left, right);
                case BoundBinaryOperatorKind.Less:
                    return (int)left < (int)right;
                case BoundBinaryOperatorKind.LessOrEquals:
                    return (int)left <= (int)right;
                case BoundBinaryOperatorKind.Greater:
                    return (int)left > (int)right;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return (int)left >= (int)right;
                default:
                    throw new Exception($"Unexpected binary operator {b.Op}");
            }
        }

        private object? EvaluateCallExpression(BoundCallExpression node)
        {
            if (node.Function == BuiltinFunctions.Input)
            {
                return Console.ReadLine();
            }
            else if (node.Function == BuiltinFunctions.Print)
            {
                object? value = EvaluateExpression(node.Arguments[0]);
                Console.WriteLine(value);
                return null;
            }
            else if (node.Function == BuiltinFunctions.Rnd)
            {
                int max = (int)EvaluateExpression(node.Arguments[0])!;
                if (_random == null)
                {
                    _random = new Random();
                }

                return _random.Next(max);
            }
            else
            {
                Dictionary<VariableSymbol, object>? locals = new Dictionary<VariableSymbol, object>();
                for (int i = 0; i < node.Arguments.Length; i++)
                {
                    ParameterSymbol? parameter = node.Function.Parameters[i];
                    object? value = EvaluateExpression(node.Arguments[i]);
                    Debug.Assert(value != null);
                    locals.Add(parameter, value);
                }

                _locals.Push(locals);

                BoundBlockStatement? statement = _functions[node.Function];
                object? result = EvaluateStatement(statement);

                _locals.Pop();

                return result;
            }
        }

        private object? EvaluateConversionExpression(BoundConversionExpression node)
        {
            object? value = EvaluateExpression(node.Expression);
            if (node.Type == TypeSymbol.Any)
            {
                return value;
            }
            else if (node.Type == TypeSymbol.Bool)
            {
                return Convert.ToBoolean(value);
            }
            else if (node.Type == TypeSymbol.Int)
            {
                return Convert.ToInt32(value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                return Convert.ToString(value);
            }
            else
            {
                throw new Exception($"Unexpected type {node.Type}");
            }
        }

        private void Assign(VariableSymbol variable, object value)
        {
            if (variable.Kind == SymbolKind.GlobalVariable)
            {
                _globals[variable] = value;
            }
            else
            {
                Dictionary<VariableSymbol, object>? locals = _locals.Peek();
                locals[variable] = value;
            }
        }
    }
}