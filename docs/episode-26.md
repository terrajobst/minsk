# Episode 26

[Video](https://www.youtube.com/watch?v=Y2Gn6qr_twA&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=26) |
[Pull Request](https://github.com/terrajobst/minsk/pull/148) |
[Previous](episode-26.md) |
[Next](episode-27.md)

## Completed items

* Enabled nullable in Minsk.Tests & Minsk.Generators
* Honored nullable annotations in source generator
* Filed and fixed various TODOs

## Interesting aspects

### Leveraging nullable annotations inside the source generator

We're now honoring nullable annotations when generating the
`SyntaxNode.GetChildren()` methods.

For example, consider `ReturnStatementSyntax`. The `Expression` property is
marked as being nullable (because `return` can be used without an expression).

```C#
partial class ReturnStatementSyntax : StatementSyntax
{
    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax? Expression { get; }
}
```

Our generator now uses the null annotation for the `Expression` property and
emits a `null` check:

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

This is simply done by [using Roslyn's `NullableAnnotation`
property][null-annotations]:

```C#
var canBeNull = property.NullableAnnotation == NullableAnnotation.Annotated;
if (canBeNull)
{
    writer.WriteLine($"if ({property.Name} != null)");
    writer.Indent++;
}

writer.WriteLine($"yield return {property.Name};");

if (canBeNull)
    writer.Indent--;
```

[null-annotations]: https://github.com/terrajobst/minsk/blob/877fefa36e184da125fd62942b5797328df79896/src/Minsk.Generators/SyntaxNodeGetChildrenGenerator.cs#L59-L69
