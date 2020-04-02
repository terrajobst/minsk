# Episode 15

[Video](https://www.youtube.com/watch?v=F0ZeU0aSkfQ&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=16) |
[Pull Request](https://github.com/terrajobst/minsk/pull/80) |
[Previous](episode-14.md) |
[Next](episode-16.md)

## Completed items

* Added support for multiple syntax trees in a single compilation
* Added a compiler project `mc` that accepts the program as paths & runs it
* Added support for running the compiler from inside VS Code, with diagnostics
  showing up in the Problems pane

## Interesting aspects

### Having nodes know the syntax tree they are contained in

In order to support multiple files, the parser and binder will need to report
diagnostics that knows which source file, or `SourceText`, they are for. Right
now, we only report diagnostics for a span, but we don't know which file that
span is in. Since we're taking the span usually from a given token or syntax
node, it's easiest if a given token or node would inherently know which
`SourceText` they belong to. In fact, it would even more more useful if they
would know which `SyntaxTree` they are contained in. However, given that tokens
and nodes are immutable, this isn't as straight forward as it seems: each node
wants its children in the constructor and the syntax tree wants its root in the
constructor. This means we can neither construct the tree first nor the nodes
first. However, we can cheat and have the `SyntaxTree` constructor run the parse
method and pass itself to it:

```C#
partial class SyntaxTree
{
    private SyntaxTree(SourceText text)
    {
        Text = text;

        var parser = new Parser(syntaxTree);
        Root = parser.ParseCompilationUnit();
        Diagnostics = parser.Diagnostics.ToImmutableArray();
    }
}
```

This way, the parser can pass the syntax tree to all nodes and the syntax tree
constructor can assign the root without anyone violating the immutability
guarantees.

Having the nodes know the syntax tree allows us to cheat in other areas as well:
eventually we'll want nodes to know their parent. This can be achieved by having
the syntax tree contain a lazily computed dictionary from child to parent which
it populates on first use by talking the root top-down. The syntax node would
use that to return the value for its parent property.

Knowing the parent node simplifies common operations in an IDE where a location
needs to be used to find nodes in the tree. It's much easier to have an API that
allows to find the token containing the position and then let the consumer walk
upwards in the tree to find what the are looking for, such as the first
containing expression, statement, or function declaration.

### Lexing individual tokens

Since `SyntaxToken` is derived from `SyntaxNode` they also know the syntax tree
they are contained in. This poses challenges when we need to produce standalone
tokens without parsing, for example when doing syntax highlighting or in our
unit tests. We need to decide what we want to happen in this case:

One option is to return `null` for the syntax tree but this would make
everything else a bit more complicated because now random parts of the compiler
API accepting tokens would now have to check for `null`.

It's easier to fabricate a fake syntax tree, that is a syntax tree whose root is
a compilation with not contents.

To achieve that, we generalize the `SyntaxTree` constructor by extracting the
parsing into a delegate that produces the root node and the diagnostics. When
lexing individual tokens, we only return lexer diagnostics and produce an empty
root:

```C#
partial class SyntaxTree
{
    private delegate void ParseHandler(SyntaxTree syntaxTree,
                                        out CompilationUnitSyntax root,
                                        out ImmutableArray<Diagnostic> diagnostics);

    private SyntaxTree(SourceText text, ParseHandler handler)
    {
        Text = text;

        handler(this, out var root, out var diagnostics);

        Diagnostics = diagnostics;
        Root = root;
    }

    public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text,
                                                          out ImmutableArray<Diagnostic> diagnostics)
    {
        var tokens = new List<SyntaxToken>();

        void ParseTokens(SyntaxTree st, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> d)
        {
            root = null;

            var lexer = new Lexer(st);
            while (true)
            {
                var token = lexer.Lex();
                if (token.Kind == SyntaxKind.EndOfFileToken)
                {
                    root = new CompilationUnitSyntax(st, ImmutableArray<MemberSyntax>.Empty, token);
                    break;
                }

                tokens.Add(token);
            }

            d = lexer.Diagnostics.ToImmutableArray();
        }

        var syntaxTree = new SyntaxTree(text, ParseTokens);
        diagnostics = syntaxTree.Diagnostics.ToImmutableArray();
        return tokens.ToImmutableArray();
    }
}
```
