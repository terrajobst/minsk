# Episode 25

[Video](https://www.youtube.com/watch?v=SlHnM3aQfW0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=25) |
[Pull Request](https://github.com/terrajobst/minsk/pull/144) |
[Previous](episode-24.md) |
[Next](episode-26.md)

## Completed items

* Use C# 8's nullable reference types in Minsk, msc, and msi
    - Remaining: Minks.Generators and Minsk.Tests

## Interesting aspects

### Nullable reference types

[Nullable reference types][post] is a feature that enables us to express which
reference types are supposed to be `null`. It's beyond the scope of these notes
to fully describe this feature, so please check out the [blog post][post] and
the [documentation][docs].

This feature is off by default (because it produces additional warnings), so we
need to turn it on. The best approach for moving existing code to nullable
reference types is as follows:

1. Enable it in the project file via `<Nullable>Enable</Nullable>`
2. Mark all files as `#nullable disable`
3. Go file by file and remove `#nullable disable`, ideally walking from your
   lowest layer to your highest layer to avoid having to go back to files you
   have already touched. After each file, fix all warnings.

The nice thing about this approach is that

1. You don't have to tackle the entire code base in one step. You can check in
   between files and still get a build without warnings.
2. New code files will be nullable enabled by default, thus not accruing debt.

In our case I cheated and did all files in one session, thus skipping the
`#nullable disable` step. That works here because the code base is somewhat
small (less than 10K LOC).

Generally speaking, nullable reference types is a feature that also involves
taste. For practical reasons, you can't physically ban `null`. The trick is
making sure that things that aren't supposed to be `null` cannot be observed to
be `null`. There are cases where cooperation from your code is required.
Consider this:

```C#
SyntaxNode[] GetNodes()
{
    var result = new SyntaxNode[Count];
    for (var i = 0; i < result.Length; i++)
        result[i] = GetNode(i);
    return result;
}
```

The first line allocates an array of `SyntaxNode`. If nullable is enabled for
this method, `SyntaxNode[]` means "array of non-null syntax nodes". Well,
clearly you can't create an array and fill it in one operation. However, your
code can make sure that there are no null values once you hand the array to
other code.

The type system won't always be able to tell that things aren't null while you
statically know that to be true. In those case you can use the `!` suffix
operator to tell the compiler "trust me, this can't be null here".

For example, consider this code:

```C#
private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
{
    var expression = syntax.Expression == null ? null : BindExpression(syntax.Expression);

    if (_function == null)
    {
        if (expression != null)
        {
            // Main does not support return values.
            _diagnostics.ReportInvalidReturnWithValueInGlobalStatements(syntax.Expression!.Location);
        }
    }

    // ...
}
```

When reporting the error we know that `syntax.Expression` can't be `null`,
otherwise `expression` would have been `null` too. However, the compiler can't
know that so we helped by adding the `!` operator.

If you're wrong, you will get a `NullReferenceException` or
`ArgumentNullException` at runtime. While this might sound bad at first ("wait,
isn't this feature supposed to get rid of all null references?") it's not that
bad in practice. I found that in my own code this feature makes it much easier
to reason about `null` values and greatly reduces accidental null references,
although it doesn't eliminate them entirely.

[post]: https://devblogs.microsoft.com/dotnet/try-out-nullable-reference-types
[docs]: https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references

### Using in code generator

Null annotations are persisted in metadata and are exposed by the Roslyn APIs.
We currently don't utilize them but it's quite simple and will be tackled in
one of the upcoming episodes ([#141]).

The basic issue goes like this: `SyntaxNode.GetChildren()` shouldn't return
`null` nodes. However, the generator currently doesn't know which nodes can be
`null` because both properties look the same:

```C#
partial class ReturnStatementSyntax : StatementSyntax
{
    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax Expression { get; }
}
```

Thus, this is what the generated code for `ReturnStatementSyntax` looks like:

```C#
partial class ReturnStatementSyntax
{
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ReturnKeyword;
        yield return Expression;
    }
}
```

However, we know that `Expression` might be `null`. So we added a `null`
annotation:

```C#
partial class ReturnStatementSyntax : StatementSyntax
{
    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax? Expression { get; }
}
```

Our generator should use this annotation to emit a `null` check:

```C#
partial class ReturnStatementSyntax
{
    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ReturnKeyword;
        if (Expression != null)
            yield return Expression;
    }
}
```

[#141]: https://github.com/terrajobst/minsk/issues/141