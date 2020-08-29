using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Minsk.CodeAnalysis.Symbols;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class ControlFlowGraph
    {
        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks, List<BasicBlockBranch> branches)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = branches;
        }

        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        public sealed class BasicBlock
        {
            public BasicBlock()
            {
            }

            public BasicBlock(bool isStart)
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }

            public bool IsStart { get; }
            public bool IsEnd { get; }
            public List<BoundStatement> Statements { get; } = new List<BoundStatement>();
            public List<BasicBlockBranch> Incoming { get; } = new List<BasicBlockBranch>();
            public List<BasicBlockBranch> Outgoing { get; } = new List<BasicBlockBranch>();

            public override string ToString()
            {
                if (IsStart)
                {
                    return "<Start>";
                }

                if (IsEnd)
                {
                    return "<End>";
                }

                using (StringWriter? writer = new StringWriter())
                using (IndentedTextWriter? indentedWriter = new IndentedTextWriter(writer))
                {
                    foreach (BoundStatement? statement in Statements)
                    {
                        statement.WriteTo(indentedWriter);
                    }

                    return writer.ToString();
                }
            }
        }

        public sealed class BasicBlockBranch
        {
            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpression? condition)
            {
                From = from;
                To = to;
                Condition = condition;
            }

            public BasicBlock From { get; }
            public BasicBlock To { get; }
            public BoundExpression? Condition { get; }

            public override string ToString()
            {
                if (Condition == null)
                {
                    return string.Empty;
                }

                return Condition.ToString();
            }
        }

        public sealed class BasicBlockBuilder
        {
            private List<BoundStatement> _statements = new List<BoundStatement>();
            private List<BasicBlock> _blocks = new List<BasicBlock>();

            public List<BasicBlock> Build(BoundBlockStatement block)
            {
                foreach (BoundStatement? statement in block.Statements)
                {
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.LabelStatement:
                            StartBlock();
                            _statements.Add(statement);
                            break;
                        case BoundNodeKind.GotoStatement:
                        case BoundNodeKind.ConditionalGotoStatement:
                        case BoundNodeKind.ReturnStatement:
                            _statements.Add(statement);
                            StartBlock();
                            break;
                        case BoundNodeKind.NopStatement:
                        case BoundNodeKind.VariableDeclaration:
                        case BoundNodeKind.ExpressionStatement:
                            _statements.Add(statement);
                            break;
                        default:
                            throw new Exception($"Unexpected statement: {statement.Kind}");
                    }
                }

                EndBlock();

                return _blocks.ToList();
            }

            private void StartBlock()
            {
                EndBlock();
            }

            private void EndBlock()
            {
                if (_statements.Count > 0)
                {
                    BasicBlock? block = new BasicBlock();
                    block.Statements.AddRange(_statements);
                    _blocks.Add(block);
                    _statements.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private Dictionary<BoundStatement, BasicBlock> _blockFromStatement = new Dictionary<BoundStatement, BasicBlock>();
            private Dictionary<BoundLabel, BasicBlock> _blockFromLabel = new Dictionary<BoundLabel, BasicBlock>();
            private List<BasicBlockBranch> _branches = new List<BasicBlockBranch>();
            private BasicBlock _start = new BasicBlock(isStart: true);
            private BasicBlock _end = new BasicBlock(isStart: false);

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                if (!blocks.Any())
                {
                    Connect(_start, _end);
                }
                else
                {
                    Connect(_start, blocks.First());
                }

                foreach (BasicBlock? block in blocks)
                {
                    foreach (BoundStatement? statement in block.Statements)
                    {
                        _blockFromStatement.Add(statement, block);
                        if (statement is BoundLabelStatement labelStatement)
                        {
                            _blockFromLabel.Add(labelStatement.Label, block);
                        }
                    }
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    BasicBlock? current = blocks[i];
                    BasicBlock? next = i == blocks.Count - 1 ? _end : blocks[i + 1];

                    foreach (BoundStatement? statement in current.Statements)
                    {
                        bool isLastStatementInBlock = statement == current.Statements.Last();
                        switch (statement.Kind)
                        {
                            case BoundNodeKind.GotoStatement:
                                BoundGotoStatement? gs = (BoundGotoStatement)statement;
                                BasicBlock? toBlock = _blockFromLabel[gs.Label];
                                Connect(current, toBlock);
                                break;
                            case BoundNodeKind.ConditionalGotoStatement:
                                BoundConditionalGotoStatement? cgs = (BoundConditionalGotoStatement)statement;
                                BasicBlock? thenBlock = _blockFromLabel[cgs.Label];
                                BasicBlock? elseBlock = next;
                                BoundExpression? negatedCondition = Negate(cgs.Condition);
                                BoundExpression? thenCondition = cgs.JumpIfTrue ? cgs.Condition : negatedCondition;
                                BoundExpression? elseCondition = cgs.JumpIfTrue ? negatedCondition : cgs.Condition;
                                Connect(current, thenBlock, thenCondition);
                                Connect(current, elseBlock, elseCondition);
                                break;
                            case BoundNodeKind.ReturnStatement:
                                Connect(current, _end);
                                break;
                            case BoundNodeKind.NopStatement:
                            case BoundNodeKind.VariableDeclaration:
                            case BoundNodeKind.LabelStatement:
                            case BoundNodeKind.ExpressionStatement:
                                if (isLastStatementInBlock)
                                {
                                    Connect(current, next);
                                }

                                break;
                            default:
                                throw new Exception($"Unexpected statement: {statement.Kind}");
                        }
                    }
                }

                ScanAgain:
                foreach (BasicBlock? block in blocks)
                {
                    if (!block.Incoming.Any())
                    {
                        RemoveBlock(blocks, block);
                        goto ScanAgain;
                    }
                }

                blocks.Insert(0, _start);
                blocks.Add(_end);

                return new ControlFlowGraph(_start, _end, blocks, _branches);
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpression? condition = null)
            {
                if (condition is BoundLiteralExpression l)
                {
                    bool value = (bool)l.Value;
                    if (value)
                    {
                        condition = null;
                    }
                    else
                    {
                        return;
                    }
                }

                BasicBlockBranch? branch = new BasicBlockBranch(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                _branches.Add(branch);
            }

            private void RemoveBlock(List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (BasicBlockBranch? branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    _branches.Remove(branch);
                }

                foreach (BasicBlockBranch? branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    _branches.Remove(branch);
                }

                blocks.Remove(block);
            }

            private static BoundExpression Negate(BoundExpression condition)
            {
                BoundUnaryExpression? negated = BoundNodeFactory.Not(condition.Syntax, condition);
                if (negated.ConstantValue != null)
                {
                    return new BoundLiteralExpression(condition.Syntax, negated.ConstantValue.Value);
                }

                return negated;
            }
        }

        public void WriteTo(TextWriter writer)
        {
            string Quote(string text)
            {
                return "\"" + text.TrimEnd().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\l") + "\"";
            }

            writer.WriteLine("digraph G {");

            Dictionary<BasicBlock, string>? blockIds = new Dictionary<BasicBlock, string>();

            for (int i = 0; i < Blocks.Count; i++)
            {
                string? id = $"N{i}";
                blockIds.Add(Blocks[i], id);
            }

            foreach (BasicBlock? block in Blocks)
            {
                string? id = blockIds[block];
                string? label = Quote(block.ToString());
                writer.WriteLine($"    {id} [label = {label}, shape = box]");
            }

            foreach (BasicBlockBranch? branch in Branches)
            {
                string? fromId = blockIds[branch.From];
                string? toId = blockIds[branch.To];
                string? label = Quote(branch.ToString());
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph Create(BoundBlockStatement body)
        {
            BasicBlockBuilder? basicBlockBuilder = new BasicBlockBuilder();
            List<BasicBlock>? blocks = basicBlockBuilder.Build(body);

            GraphBuilder? graphBuilder = new GraphBuilder();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStatement body)
        {
            ControlFlowGraph? graph = Create(body);

            foreach (BasicBlockBranch? branch in graph.End.Incoming)
            {
                BoundStatement? lastStatement = branch.From.Statements.LastOrDefault();
                if (lastStatement == null || lastStatement.Kind != BoundNodeKind.ReturnStatement)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
