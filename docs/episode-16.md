# Episode 16

[Video](https://www.youtube.com/watch?v=gXrMmaEebOI&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=16) |
[Pull Request](https://github.com/terrajobst/minsk/pull/83) |
[Previous](episode-15.md) |
[Next](episode-17.md)

## Completed items

* Rename compiler projects to avoid name collisions
* Make meta commands attribute-driven
* Add `#help` that shows list of available meta commands
* Add `#load` that loads a script file into the REPL
* Add `#ls` that shows visible symbols
* Add `#dump` that shows the bound tree of a given function
* Persist submissions between runs

## Interesting aspects

### Meta commands

Right now, we evaluate meta commands by using a switch statement:

```C#
protected override void EvaluateMetaCommand(string input)
{
    switch (input)
    {
        case "#showTree":
            _showTree = !_showTree;
            Console.WriteLine(_showTree ? "Showing parse trees." : "Not showing parse trees.");
            break;
        case "#showProgram":
            _showProgram = !_showProgram;
            Console.WriteLine(_showProgram ? "Showing bound tree." : "Not showing bound tree.");
            break;
        case "#cls":
            Console.Clear();
            break;
        case "#reset":
            _previous = null;
            _variables.Clear();
            break;
        default:
            base.EvaluateMetaCommand(input);
            break;
    }
}
```

While that's perfectly serviceable it makes it somewhat tedious to support a
meta command like `#help` that would show the list of available commands. Of
course you can do it, but then you'd duplicate information. It's easier if use a
scheme where meta commands are data-driven, which in C# is very by using
attributes:

```C#
[MetaCommand("cls", "Clears the screen")]
private void EvaluateCls()
{
    Console.Clear();
}

[MetaCommand("reset", "Clears all previous submissions")]
private void EvaluateReset()
{
    _previous = null;
    _variables.Clear();
    ClearSubmissions();
}
```

During startup of the REPL we simply look for methods with that attribute and
record them in a list.

This mechanism also allows us to support arguments (simply by having the method
accept arguments):

```C#
[MetaCommand("load", "Loads a script file")]
private void EvaluateLoad(string path)
{
    path = Path.GetFullPath(path);

    if (!File.Exists(path))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"error: file does not exist '{path}'");
        Console.ResetColor();
        return;
    }

    var text = File.ReadAllText(path);
    EvaluateSubmission(text);
}
```

The only tricky thing here are handling of quotes, because we want this to work
in the RPL as well:

```C++
#load "samples/hello world/hello.ms"
```

We're doing this with a very simple loop that doesn't look unlike our lexer:

```C#
var args = new List<string>();
var inQuotes = false;
var position = 1;
var sb = new StringBuilder();
while (position < input.Length)
{
    var c = input[position];
    var l = position + 1>= input.Length ? '\0' : input[position + 1];

    if (char.IsWhiteSpace(c))
    {
        if (!inQuotes)
            CommitPendingArgument();
        else
            sb.Append(c);
    }
    else if (c == '\"')
    {
        if (!inQuotes)
            inQuotes = true;
        else if (l == '\"')
        {
            sb.Append(c);
            position++;
        }
        else
            inQuotes = false;
    }
    else
    {
        sb.Append(c);
    }

    position++;
}

CommitPendingArgument();
```

Now that I'm writing these notes I'm wondering if we can modify our lexer to
support this scenario as well. We could introduce the notion of a meta command
token (which would just be the `#` followed by an identifier). The parser would
ignore them. Basically when it sees a meta command token it would skip all
tokens until it sees and end of line token). This way, we can trivially support
any syntax for tokens, which includes strings, numbers, and eventually also
comments.

Future versions could support conversions and default values as well.

But for now, this simple approach will serve us well enough.

### Reachable symbols

We introduced an `#ls` command which allows us to show which symbols are
available in the REPL:

```JavaScript
» let x = 10
10
» let y = 20
20
» function add(a: int, b: int): int
· {
·     return a + b
· }
» #ls
function add(a: int, b: int): int
let x: int
let y: int
```

We implement this by just walking the symbols from the current submission
backwards. But we need to be careful to support shadowing. Shadowing occurs
when a new symbols is created that has the same name as an existing symbol:

```JavaScript
» #ls
function add(a: int, b: int): int
let x: int
let y: int
» var y = "Test"
Test
» #ls
function add(a: int, b: int): int
let x: int
var y: string
```

In this example, the new `y` symbol is a writable variable of type `string`
while the previous one is an init-only variable of type `int`.

We implemented this by adding a `GetSymbols()` method on the compilation that
implements the shadowing semantics. In our current case, that's simply by name.
But we could imagine a more elaborate strategy if, for example, functions can be
overloaded by arity (that is number of arguments). In this case it would be a
bit more complex.

```C#
public partial class Compilation
{
    public IEnumerable<Symbol> GetSymbols()
    {
        var submission = this;
        var seenSymbolNames = new HashSet<string>();

        while (submission != null)
        {
            foreach (var function in submission.Functions)
                if (seenSymbolNames.Add(function.Name))
                    yield return function;

            foreach (var variable in submission.Variables)
                if (seenSymbolNames.Add(variable.Name))
                    yield return variable;

            submission = submission.Previous;
        }
    }
}
```
