using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using Minsk.IO;

namespace Minsk
{
    internal abstract class Repl
    {
        private readonly List<MetaCommand> _metaCommands = new List<MetaCommand>();
        private readonly List<string> _submissionHistory = new List<string>();
        private int _submissionHistoryIndex;

        private bool _done;

        protected Repl()
        {
            InitializeMetaCommands();
        }

        private void InitializeMetaCommands()
        {
            MethodInfo[]? methods = GetType().GetMethods(BindingFlags.Public |
                                               BindingFlags.NonPublic |
                                               BindingFlags.Static |
                                               BindingFlags.Instance |
                                               BindingFlags.FlattenHierarchy);
            foreach (MethodInfo? method in methods)
            {
                MetaCommandAttribute? attribute = method.GetCustomAttribute<MetaCommandAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                MetaCommand? metaCommand = new MetaCommand(attribute.Name, attribute.Description, method);
                _metaCommands.Add(metaCommand);
            }
        }

        public void Run()
        {
            while (true)
            {
                string? text = EditSubmission();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (!text.Contains(Environment.NewLine) && text.StartsWith("#"))
                {
                    EvaluateMetaCommand(text);
                }
                else
                {
                    EvaluateSubmission(text);
                }

                _submissionHistory.Add(text);
                _submissionHistoryIndex = 0;
            }
        }

        private delegate object? LineRenderHandler(IReadOnlyList<string> lines, int lineIndex, object? state);

        private sealed class SubmissionView
        {
            private readonly LineRenderHandler _lineRenderer;
            private readonly ObservableCollection<string> _submissionDocument;
            private int _cursorTop;
            private int _renderedLineCount;
            private int _currentLine;
            private int _currentCharacter;

            public SubmissionView(LineRenderHandler lineRenderer, ObservableCollection<string> submissionDocument)
            {
                _lineRenderer = lineRenderer;
                _submissionDocument = submissionDocument;
                _submissionDocument.CollectionChanged += SubmissionDocumentChanged;
                _cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                Render();
            }

            private void Render()
            {
                Console.CursorVisible = false;

                int lineCount = 0;
                object? state = (object?)null;

                foreach (string? line in _submissionDocument)
                {
                    if (_cursorTop + lineCount >= Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, Console.WindowHeight - 1);
                        Console.WriteLine();
                        if (_cursorTop > 0)
                        {
                            _cursorTop--;
                        }
                    }

                    Console.SetCursorPosition(0, _cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.Green;

                    if (lineCount == 0)
                    {
                        Console.Write("» ");
                    }
                    else
                    {
                        Console.Write("· ");
                    }

                    Console.ResetColor();
                    state = _lineRenderer(_submissionDocument, lineCount, state);
                    Console.Write(new string(' ', Console.WindowWidth - line.Length - 2));
                    lineCount++;
                }

                int numberOfBlankLines = _renderedLineCount - lineCount;
                if (numberOfBlankLines > 0)
                {
                    string? blankLine = new string(' ', Console.WindowWidth);
                    for (int i = 0; i < numberOfBlankLines; i++)
                    {
                        Console.SetCursorPosition(0, _cursorTop + lineCount + i);
                        Console.WriteLine(blankLine);
                    }
                }

                _renderedLineCount = lineCount;

                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition()
            {
                Console.CursorTop = _cursorTop + _currentLine;
                Console.CursorLeft = 2 + _currentCharacter;
            }

            public int CurrentLine
            {
                get => _currentLine;
                set
                {
                    if (_currentLine != value)
                    {
                        _currentLine = value;
                        _currentCharacter = Math.Min(_submissionDocument[_currentLine].Length, _currentCharacter);

                        UpdateCursorPosition();
                    }
                }
            }

            public int CurrentCharacter
            {
                get => _currentCharacter;
                set
                {
                    if (_currentCharacter != value)
                    {
                        _currentCharacter = value;
                        UpdateCursorPosition();
                    }
                }
            }
        }

        private string EditSubmission()
        {
            _done = false;

            ObservableCollection<string>? document = new ObservableCollection<string>() { "" };
            SubmissionView? view = new SubmissionView(RenderLine, document);

            while (!_done)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                HandleKey(key, document, view);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
            Console.WriteLine();

            return string.Join(Environment.NewLine, document);
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default(ConsoleModifiers))
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;
                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow(view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;
                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;
                    case ConsoleKey.Home:
                        HandleHome(view);
                        break;
                    case ConsoleKey.End:
                        HandleEnd(document, view);
                        break;
                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;
                    case ConsoleKey.PageUp:
                        HandlePageUp(document, view);
                        break;
                    case ConsoleKey.PageDown:
                        HandlePageDown(document, view);
                        break;
                }
            }
            else if (key.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleControlEnter(document, view);
                        break;
                }
            }

            if (key.Key != ConsoleKey.Backspace && key.KeyChar >= ' ')
            {
                HandleTyping(document, view, key.KeyChar.ToString());
            }
        }

        private static void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentLine = 0;
            view.CurrentCharacter = 0;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            string? submissionText = string.Join(Environment.NewLine, document);
            if (submissionText.StartsWith("#") || IsCompleteSubmission(submissionText))
            {
                _done = true;
                return;
            }

            InsertLine(document, view);
        }

        private static void HandleControlEnter(ObservableCollection<string> document, SubmissionView view)
        {
            InsertLine(document, view);
        }

        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            string? remainder = document[view.CurrentLine].Substring(view.CurrentCharacter);
            document[view.CurrentLine] = document[view.CurrentLine].Substring(0, view.CurrentCharacter);

            int lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remainder);
            view.CurrentCharacter = 0;
            view.CurrentLine = lineIndex;
        }

        private static void HandleLeftArrow(SubmissionView view)
        {
            if (view.CurrentCharacter > 0)
            {
                view.CurrentCharacter--;
            }
        }

        private static void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            string? line = document[view.CurrentLine];
            if (view.CurrentCharacter <= line.Length - 1)
            {
                view.CurrentCharacter++;
            }
        }

        private static void HandleUpArrow(SubmissionView view)
        {
            if (view.CurrentLine > 0)
            {
                view.CurrentLine--;
            }
        }

        private static void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
            {
                view.CurrentLine++;
            }
        }

        private static void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            int start = view.CurrentCharacter;
            if (start == 0)
            {
                if (view.CurrentLine == 0)
                {
                    return;
                }

                string? currentLine = document[view.CurrentLine];
                string? previousLine = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                view.CurrentLine--;
                document[view.CurrentLine] = previousLine + currentLine;
                view.CurrentCharacter = previousLine.Length;
            }
            else
            {
                int lineIndex = view.CurrentLine;
                string? line = document[lineIndex];
                string? before = line.Substring(0, start - 1);
                string? after = line.Substring(start);
                document[lineIndex] = before + after;
                view.CurrentCharacter--;
            }
        }

        private static void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            int lineIndex = view.CurrentLine;
            string? line = document[lineIndex];
            int start = view.CurrentCharacter;
            if (start >= line.Length)
            {
                if (view.CurrentLine == document.Count - 1)
                {
                    return;
                }

                string? nextLine = document[view.CurrentLine + 1];
                document[view.CurrentLine] += nextLine;
                document.RemoveAt(view.CurrentLine + 1);
                return;
            }

            string? before = line.Substring(0, start);
            string? after = line.Substring(start + 1);
            document[lineIndex] = before + after;
        }

        private static void HandleHome(SubmissionView view)
        {
            view.CurrentCharacter = 0;
        }

        private static void HandleEnd(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private static void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int TabWidth = 4;
            int start = view.CurrentCharacter;
            int remainingSpaces = TabWidth - start % TabWidth;
            string? line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remainingSpaces));
            view.CurrentCharacter += remainingSpaces;
        }

        private void HandlePageUp(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex--;
            if (_submissionHistoryIndex < 0)
            {
                _submissionHistoryIndex = _submissionHistory.Count - 1;
            }

            UpdateDocumentFromHistory(document, view);
        }

        private void HandlePageDown(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex++;
            if (_submissionHistoryIndex > _submissionHistory.Count - 1)
            {
                _submissionHistoryIndex = 0;
            }

            UpdateDocumentFromHistory(document, view);
        }

        private void UpdateDocumentFromHistory(ObservableCollection<string> document, SubmissionView view)
        {
            if (_submissionHistory.Count == 0)
            {
                return;
            }

            document.Clear();

            string? historyItem = _submissionHistory[_submissionHistoryIndex];
            string[]? lines = historyItem.Split(Environment.NewLine);
            foreach (string? line in lines)
            {
                document.Add(line);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private static void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            int lineIndex = view.CurrentLine;
            int start = view.CurrentCharacter;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentCharacter += text.Length;
        }

        protected void ClearHistory()
        {
            _submissionHistory.Clear();
        }

        protected virtual object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
        {
            Console.Write(lines[lineIndex]);
            return state;
        }

        private void EvaluateMetaCommand(string input)
        {
            // Parse arguments

            List<string>? args = new List<string>();
            bool inQuotes = false;
            int position = 1;
            StringBuilder? sb = new StringBuilder();
            while (position < input.Length)
            {
                char c = input[position];
                char l = position + 1 >= input.Length ? '\0' : input[position + 1];

                if (char.IsWhiteSpace(c))
                {
                    if (!inQuotes)
                    {
                        CommitPendingArgument();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else if (c == '\"')
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                    }
                    else if (l == '\"')
                    {
                        sb.Append(c);
                        position++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                position++;
            }

            CommitPendingArgument();

            void CommitPendingArgument()
            {
                string? arg = sb.ToString();
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    args.Add(arg);
                }

                sb.Clear();
            }

            string? commandName = args.FirstOrDefault();
            if (args.Count > 0)
            {
                args.RemoveAt(0);
            }

            MetaCommand? command = _metaCommands.SingleOrDefault(mc => mc.Name == commandName);
            if (command == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid command {input}.");
                Console.ResetColor();
                return;
            }

            ParameterInfo[]? parameters = command.Method.GetParameters();

            if (args.Count != parameters.Length)
            {
                string? parameterNames = string.Join(" ", parameters.Select(p => $"<{p.Name}>"));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"error: invalid number of arguments");
                Console.WriteLine($"usage: #{command.Name} {parameterNames}");
                Console.ResetColor();
                return;
            }

            Repl? instance = command.Method.IsStatic ? null : this;
            command.Method.Invoke(instance, args.ToArray());
        }

        protected abstract bool IsCompleteSubmission(string text);

        protected abstract void EvaluateSubmission(string text);

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        protected sealed class MetaCommandAttribute : Attribute
        {
            public MetaCommandAttribute(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public string Name { get; }
            public string Description { get; }
        }

        private sealed class MetaCommand
        {
            public MetaCommand(string name, string description, MethodInfo method)
            {
                Name = name;
                Description = description;
                Method = method;
            }

            public string Name { get; }
            public string Description { get; }
            public MethodInfo Method { get; }
        }

        [MetaCommand("help", "Shows help")]
        protected void EvaluateHelp()
        {
            int maxNameLength = _metaCommands.Max(mc => mc.Name.Length);

            foreach (MetaCommand? metaCommand in _metaCommands.OrderBy(mc => mc.Name))
            {
                ParameterInfo[]? metaParams = metaCommand.Method.GetParameters();
                if (metaParams.Length == 0)
                {
                    string? paddedName = metaCommand.Name.PadRight(maxNameLength);

                    Console.Out.WritePunctuation("#");
                    Console.Out.WriteIdentifier(paddedName);
                }
                else
                {
                    Console.Out.WritePunctuation("#");
                    Console.Out.WriteIdentifier(metaCommand.Name);
                    foreach (ParameterInfo? pi in metaParams)
                    {
                        Console.Out.WriteSpace();
                        Console.Out.WritePunctuation("<");
                        Console.Out.WriteIdentifier(pi.Name!);
                        Console.Out.WritePunctuation(">");
                    }
                    Console.Out.WriteLine();
                    Console.Out.WriteSpace();
                    for (int _ = 0; _ < maxNameLength; _++)
                    {
                        Console.Out.WriteSpace();
                    }
                }
                Console.Out.WriteSpace();
                Console.Out.WriteSpace();
                Console.Out.WriteSpace();
                Console.Out.WritePunctuation(metaCommand.Description);
                Console.Out.WriteLine();
            }
        }
    }
}
