# Episode 24

[Video](https://www.youtube.com/watch?v=-rNCfZyDzNw&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=25) |
[Pull Request](https://github.com/terrajobst/minsk/pull/142) |
[Previous](episode-23.md) |
[Next](episode-25.md)

## Completed items

* Add trivia, which allows storing whitespace and comments in the syntax tree
* Add classification, which performs syntax highlighting by using the syntax
  tree

## Interesting aspects

### Syntax tree as the source of truth (pun intended)

Most text book compilers use [abstract syntax trees (AST)][ast] to represent the
source code. They are deliberately not a truthful representation (hence the term
*abstract*). For example, they won't contain whitespace nor comments. Often,
they also don't contain things like parenthesized expressions as the grouping is
evident by the structure of the tree already. Usually, the semantic analysis
doesn't produce a new tree but will instead fill in things like type information
into the nodes directly. In some compilers lowering (or de-sugaring) will also
happen during parsing or binding.

This design makes sense when memory and processing power is limited and/or the
compiler is only used for batch compilation (i.e. the compiler is only a command
line tool). If you want to build a compiler that can also provide an API for an
IDE then this design isn't ideal.

Modern IDEs have many facilities that deal with the syntax itself. The most
obvious one is syntax highlighting, but this also extends to code folding, a
document outline, code completion, and, of course, refactorings. It's much
easier to implement all of these when the syntax representation produced by the
compiler is a truthful representation of the input file. This allows us to use
the source code (or more accurately the syntax nodes) as the API of the
language.

Later, we'll expose APIs that allows asking questions about specific syntax
nodes, such as "which type does this `ExpressionSyntax` resolve to" or "which
`FunctionSymbol` does this `CallExpressionSyntax` bind do".

Right now, the `SyntaxTree` is lossy representation of the input because we
don't retain:

1. Whitespace
2. Comments
3. Malformed tokens
4. Tokens skipped by the parser

[ast]: https://en.wikipedia.org/wiki/Abstract_syntax_tree

### Enter trivia

Instead of modeling whitespace and comments as tokens we'll model them as
*trivia*. Trivia is a concept that I've only seen in Roslyn. Trivia is
associated with a token. A token can have leading and trailing trivia. Trailing
trivia only goes until the end of line, all subsequent trivia is considered
leading trivia for the following token. Therefore, only the first token on a
line can have leading trivia. Comments at the bottom of a file are leading
trivia of the synthetic end-of-file token.

This design doesn't complicate the syntax representation in a sense that the
parser doesn't have to deal with them when looking at tokens. And the structure
of syntax tree is largely unchanged, thus not complicating navigating it either.

But one running joke on the Roslyn team is that *trivia isn't trivial*. Once you
start modifying syntax it's often complicated to make sure that trivia is
retained in such a way that developers will find the end result appealing.
However, I don't believe that to be an issue with the design of trivia but
rather an inherent complexity of languages with syntactical elements that aren't
relevant for semantic analysis.

### Leading vs. trailing trivia

Earlier I wrote that a token can have leading and trailing trivia. You might
wonder why that is. We could get away with only having leading trivia. All
trivia that follows the last token in a file will be leading trivia for the
end-of-file token.

Having both leading & trailing trivia allows us to associate them with the token
that makes more sense for the developer:

* If a comment is on a line by itself, it usually refers to the following code.
* If a comment is on a line with code, it usually refers to the code before it.

Let's look at a few examples:

```JS
// Comment 1
function Foo(a: int, // Comment 2
             b: int) // Comment 3
{
    let x = a /* Comment 4 */ + /* Comment 5 */ b
    /* Comment 6 */ let y = x // Comment 7
    // Comment 8
}
```

According to the rules stated above the comments are associated with tokens as
follows:

Comment   | Leading/Trailing | Token
----------|------------------|-----------
Comment 1 | Leading          | `function`
Comment 2 | Trailing         | `,`
Comment 3 | Trailing         | `)`
Comment 4 | Trailing         | `a`
Comment 5 | Trailing         | `+`
Comment 6 | Leading          | `let`
Comment 7 | Trailing         | `x`
Comment 8 | Leading          | `}`

## Span and FullSpan

Syntax nodes have a property `Span` that returns the span the node is covering.
It ranges from the first token's start to the last token's end.

With trivia the question is whether that start position includes trivia or not.
Roslyn went down the path of having two properties `Span` and `FullSpan`. `Span`
matches our current behavior.

Now [let's add a `FullSpan` property][syntaxnode-fullspan] that includes trivia:

```C#
partial class SyntaxNode
{
    // Existing:
    public virtual TextSpan Span
    {
        get
        {
            var first = GetChildren().First().Span;
            var last = GetChildren().Last().Span;
            return TextSpan.FromBounds(first.Start, last.End);
        }
    }

    // New:
    public virtual TextSpan FullSpan
    {
        get
        {
            var first = GetChildren().First().FullSpan;
            var last = GetChildren().Last().FullSpan;
            return TextSpan.FromBounds(first.Start, last.End);
        }
    }
}
```

As you can see, the only difference is using the token's [`FullSpan`
property][syntaxtoken-fullspan]:

```C#
partial class SyntaxToken
{
    // Existing:
    public override TextSpan Span => new TextSpan(Position, Text?.Length ?? 0);

    // New:
    public override TextSpan FullSpan
    {
        get
        {
            var start = LeadingTrivia.Length == 0
                            ? Span.Start
                            : LeadingTrivia.First().Span.Start;
            var end = TrailingTrivia.Length == 0
                            ? Span.End
                            : TrailingTrivia.Last().Span.End;
            return TextSpan.FromBounds(start, end);
        }
    }
}
```

[syntaxnode-fullspan]: https://github.com/terrajobst/minsk/blob/2093b98c6f131214fa5fc7448c5f3803f5ebc185/src/Minsk/CodeAnalysis/Syntax/SyntaxNode.cs#L20-L38
[syntaxtoken-fullspan]: https://github.com/terrajobst/minsk/blob/2093b98c6f131214fa5fc7448c5f3803f5ebc185/src/Minsk/CodeAnalysis/Syntax/SyntaxToken.cs#L26-L39

### Syntax highlighting by using the syntax tree

Originally our syntax highlighter was line based, that is we would tokenize one
line at a time. As of the [last episode][line-independent-highlighting] we're no
longer able to do that because we now have tokens that can span multiple lines
(specifically multi-line comments). There are other cases that can complicate
syntax highlighting without parsing, for example, a pre-processor or contextual
keywords.

In this episode, we've moved a way from a the previous approach of using tokens
and instead use [the syntax tree for syntax highlighting][syntaxtree-state].

To do that, we introduce an [API called `Classifier`][classifier] that provides
support for classifying regions of text:

```C#
public enum Classification
{
    Text,
    Keyword,
    Identifier,
    Number,
    String,
    Comment
}
public sealed class ClassifiedSpan
{
    public ClassifiedSpan(TextSpan span, Classification classification)
    {
        Span = span;
        Classification = classification;
    }

    public TextSpan Span { get; }
    public Classification Classification { get; }
}
public static class Classifier
{
    public static ImmutableArray<ClassifiedSpan> Classify(SyntaxTree syntaxTree, TextSpan span);
}
```

The classifier will only return spans that are contained in the provided span.
Tokens that are overlapping with the provide span's start or end are clipped to
fall within the span. And since the syntax is in tree form we can even leverage
the node's `FullSpan` property to only visit nodes that are overlapping with the
provided span.

This makes it really easy to do perform [line-based syntax highlighting in the
REPL][highlighting]:

```C#
var classifiedSpans = Classifier.Classify(syntaxTree, lineSpan);

foreach (var classifiedSpan in classifiedSpans)
{
    var classifiedText = syntaxTree.Text.ToString(classifiedSpan.Span);

    switch (classifiedSpan.Classification)
    {
        // Set foreground color based on classification
    }

    Console.Write(classifiedText);
    Console.ResetColor();
}
```

[line-independent-highlighting]: episode-23.md#line-independent-syntax-highlighting
[syntaxtree-state]: https://github.com/terrajobst/minsk/blob/2093b98c6f131214fa5fc7448c5f3803f5ebc185/src/msi/MinskRepl.cs#L30-L40
[classifier]: https://github.com/terrajobst/minsk/blob/2093b98c6f131214fa5fc7448c5f3803f5ebc185/src/msi/Authoring/Classifier.cs#L11
[highlighting]: https://github.com/terrajobst/minsk/blob/2093b98c6f131214fa5fc7448c5f3803f5ebc185/src/msi/MinskRepl.cs#L43-L74
