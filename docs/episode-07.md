# Episode 7

[Video](https://www.youtube.com/watch?v=SJE_gUnJl2Y&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=7) |
[Pull Request](https://github.com/terrajobst/minsk/pull/28) |
[Previous](episode-06.md) |
[Next](episode-08.md)

## Completed items

* Make evaluation tests more declarative, especially for diagnostics
* Add support for `<,` `<=`, `>=`, and `>`
* Add support for if-statements
* Add support for while-statements
* Add support for for-statements
* Ensure parser doesn't loop infinitely on malformed block
* Ensure binder doesn't crash when binding fabricated identifiers

## Interesting aspects

### Declarative testing

Writing and maintaining tests is overhead. Sure, you get value out of it in the
sense that it's minimizing risk when you're changing your code. And having good
coverage helps you to define (and maintain) a certain quality bar. But the
reality is that time spent writing tests will always compete with building
features. What I found works well for me is that I try to make writing tests
very easy and cheap. Usually that requires some sort of test infrastructure
which can also be fun to build.

Long story short, in this episode we're developing a way to express test cases
for source that we expect to have diagnostics. We generally want to validate
three things:

1. The given snippet of source has exactly a specific set of diagnostics, no
   more, no less.
2. The diagnostics occur at a particular location in source code.
3. The diagnostics have a specific text.

An [example of such a test][test-example] looks like this:

```C#
[Fact]
public void Evaluator_AssignmentExpression_Reports_CannotAssign()
{
    var text = @"
        {
            let x = 10
            x [=] 0
        }
    ";

    var diagnostics = @"
        Variable 'x' is read-only and cannot be assigned to.
    ";

    AssertDiagnostics(text, diagnostics);
}
```

The idea is that the `text` represents the source snippet. All regions that are
supposed to have diagnostics are marked with `[` and `]`. In this case, it's
just the equals token `=`. In general, the `text` property can contain multiple
marked spans. The string can also cover multiple lines. For readability of the
test itself, we want to indent the source code so that it looks logically nested
inside the C# test definition.

Before the text is parsed, it's cleaned up, which includes removal of the marker
symbols but also of the extra indentation. This helps when inspecting the text
(or parts of the syntax tree) in the debugger. The preprocessing of the text is
handled by an internal helper type [AnnotatedText][annotated-text].

The `diagnostics` string contains multiple lines, one per expected diagnostic,
in the order they are occurring in the source text.

[test-example]:
https://github.com/terrajobst/minsk/blob/d30d79b24863bf4f8de21e7af5890eb1f9b07689/src/Minsk.Tests/CodeAnalysis/EvaluationTests.cs#L223-L238
[annotated-text]:
https://github.com/terrajobst/minsk/blob/d30d79b24863bf4f8de21e7af5890eb1f9b07689/src/Minsk.Tests/CodeAnalysis/AnnotatedText.cs

### Dangling-else

Adding `if`-statements is fairly trivial. One interesting aspect when designing
the language and the parser is the [dangling-else] problem. Given this source
code, the question is which `if` the `else` is associated with, the first or the
last:

```
if some_condition
    if another_condition
       x = 10
    else
       x = 20
```

Most C-based languages do what's simplest for the parser, which is to associate
the `else` with the closest `if`. But there are other choices too, such as
generally requiring braces for the body of an `if`-statement or disallowing an
`if`-statement as a direct child of another `if`-statement.

We (well, I) chose to follow the C heritage and allow it but associate it with
the closest `if`.

[dangling-else]: https://en.wikipedia.org/wiki/Dangling_else

### Infinite loops in parser

In general, parsers can't just give up when they encounter input that they don't
understand. In the early days of compilers that was because parsing took a while
so developers want to catch as many issues in one pass as possible (ideally with
minimal cascading errors to reduce noise). In modern IDEs, parsing is often
essential to drive other features, such as syntax highlighting, code folding,
and code completion, so it's generally not desirable to have the parser give up
at the first location where an unexpected token occurs.

Thus, error recovery is a major design aspect for all parsers. It usually needs
to be tweaked to accommodate the programming style most people use so the parser
can successfully recover from common mistakes and interpret the code as a human
would likely expect. Of course, recovering from errors is generally not well
defined and thus is basically a best effort.

A parser has two options when encountering tokens that it didn't expect:

1. It can skip them
2. It can insert new ones

Both approaches are useful. The major downside of fabricating tokens is that one
has to be very careful that the parser doesn't run in an infinite loop.

In our case, the `ParseBlockStatement` looks as follows:

```C#
private BlockStatementSyntax ParseBlockStatement()
{
    var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

    var openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

    while (Current.Kind != SyntaxKind.EndOfFileToken &&
           Current.Kind != SyntaxKind.CloseBraceToken)
    {
        var statement = ParseStatement();
        statements.Add(statement);
    }

    var closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

    return new BlockStatementSyntax(openBraceToken, statements.ToImmutable(), closeBraceToken);
}
```

Basically, there is a `while` loop that will only terminate when the current
token is `}` or when we reached the end of the file. The assumption here is that
`ParseStatement()` will always consume at least one token, because if it doesn't
consume any tokens if the current token doesn't start a statement, it would
result in an infinite loop -- and that's precisely what happens here.

The reason being that `ParseStatement()` checks for known keywords to parse
specific statements. If it's not any of the statement keywords, it just falls
back to parsing an expression. If the current token isn't a valid starting token
for an expression, it will eventually call `ParseNameExpression()` which asserts
the current token is an identifier. That will report an error and also fabricate
an empty name expression. In other words, no token will be consumed, causing the
infinite loop.

There are various approaches to address this problem. One option is to know
up-front which tokens can start a statement or expression. Generated parsers can
do this, but in hand-written parsers this can be fragile. I just went with a
simple approach of remembering the current token before calling
`ParseStatement()`. If the token hasn't changed, [we skip it][parse-block]:

```diff
 while (Current.Kind != SyntaxKind.EndOfFileToken &&
        Current.Kind != SyntaxKind.CloseBraceToken)
 {
+     var startToken = Current;
+
     var statement = ParseStatement();
     statements.Add(statement);
+
+     if (Current == startToken)
+         NextToken();
 }
```

[parse-block]: https://github.com/terrajobst/minsk/blob/d30d79b24863bf4f8de21e7af5890eb1f9b07689/src/Minsk/CodeAnalysis/Syntax/Parser.cs#L102-L115