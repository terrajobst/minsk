# Episode 10

[Video](https://www.youtube.com/watch?v=VkFNanrdvLc&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=10) |
[Pull Request](https://github.com/terrajobst/minsk/pull/49) |
[Previous](episode-09.md) |
[Next](episode-11.md)

## Completed items

We added support for string literals and type symbols.

## Interesting aspects

### String literals

We now [support strings][ReadString] like so:

```
let hello = "Hello"
```

Strings need to be terminated on the same line (in other words, we don't support
line breaks in them). We also don't support any escape sequences yet (such `\n`
or `\t`). We do, however, support quotes which are escaped by [doubling them]:

```
let message = "Hello, ""World""!"
```

I did forget to add an operator for joining string literals, but we'll add that in
the next episode :-)

[ReadString]: https://github.com/terrajobst/minsk/blob/ad8a988567021330da9b5b4d26518cee6595055b/src/Minsk/CodeAnalysis/Syntax/Lexer.cs#L201-L241
[doubling them]: https://github.com/terrajobst/minsk/blob/ad8a988567021330da9b5b4d26518cee6595055b/src/Minsk/CodeAnalysis/Syntax/Lexer.cs#L221-L225

## Type symbols

In the past, we've used .NET's `System.Type` class to represent type information
in the binder. This is inconvenient because most languages have their own notion
of types, so we replaced this with a new [`TypeSymbol`] class. To make symbols
first class, we also added an abstract [`Symbol`] class and a [`SymbolKind`].

[`TypeSymbol`]: https://github.com/terrajobst/minsk/blob/cb03bb0f7277097e1507868ef722c16bdc22006a/src/Minsk/CodeAnalysis/Symbols/TypeSymbol.cs
[`Symbol`]: https://github.com/terrajobst/minsk/blob/cb03bb0f7277097e1507868ef722c16bdc22006a/src/Minsk/CodeAnalysis/Symbols/Symbol.cs
[`SymbolKind`]: https://github.com/terrajobst/minsk/blob/cb03bb0f7277097e1507868ef722c16bdc22006a/src/Minsk/CodeAnalysis/Symbols/SymbolKind.cs

## Cascading errors

Expressions are generally bound inside-out, for example, in order to bind a
binary expression one first binds the left hand side and right hand side in
order to know their types so that the operator can be resolved. This can lead to
cascading errors, like in this case:

```
(10 * false) - 10
```

There is no `*` operator defined for `int` and `bool`, so the left hand side
cannot be bound. This makes it impossible to bind the `-` operator as well. In
general, you don't want to drown the developer in error messages so a good
compiler will try to avoid generating cascading errors. For example, you don't
don't want to generators two errors here but only one -- namely that the `*`
cannot be bound because that's the root cause. Maybe you didn't mean to type
`false` but `faults` which would actually resolve to a variable of type `int`,
in which case the `-` operator could be bound.

In the past, we've either returned the left hand side when a binary expression
cannot be bound or fabricated a fake literal expression with a value of `0`,
both of which can lead to cascading errors. To fix this, we've introduced an
[*error type*][ErrorType] to indicate the absense of type information. We also
added a [`BoundErrorExpression`] that is returned whenever we cannot resolve an
expression. This allows to handle the binary expression as follows:

```C#
private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
{
    var boundLeft = BindExpression(syntax.Left);
    var boundRight = BindExpression(syntax.Right);

    // If either left or right couldn't be bound, let's bail to avoid cascading
    // errors.
    if (boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
        return new BoundErrorExpression();

    var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

    if (boundOperator == null)
    {
        _diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
        return new BoundErrorExpression();
    }

    return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
}
```

We also could have decided that the operator of `BoundBinaryExpression` is
`null` when it can't be bound but this would mean that all later phases have to
be prepared to handle a `null` operator, which seems more prone to errors.

[ErrorType]: https://github.com/terrajobst/minsk/blob/6365e15151ad5e59d3d8ac3c6a703b0d5fd7fb63/src/Minsk/CodeAnalysis/Symbols/TypeSymbol.cs#L5
[`BoundErrorExpression`]: https://github.com/terrajobst/minsk/blob/6365e15151ad5e59d3d8ac3c6a703b0d5fd7fb63/src/Minsk/CodeAnalysis/Binding/BoundErrorExpression.cs

## Dealing with inserted tokens

Peter [filed an issue][#30] noticing that we crash when declaring variables
whose identifier was inserted by the parser (because its text is `null`).

The general philosophy is that the binder needs to be able to work on any trees,
even if the parser reported diagnostics. Otherwise, it's basically impossible to
develop something like an IDE where code is virtually always in a state of being
halfway written but semantic questions still need to be answered (for example,
when showing tool tips or providing code completion).

To address the particular issue, we've extracted a [`BindVariable()`] method
that will not add variables to the scope when their name was inserted by the
parser. However, it will still construct a variable. This might bite us later
because we can now find variables in the bound tree that don't exist in the
scope but if it turns out that this approach doesn't work, we can choose a
different one.

[#30]: https://github.com/terrajobst/minsk/issues/30
[`BindVariable()`]: https://github.com/terrajobst/minsk/blob/ad8a988567021330da9b5b4d26518cee6595055b/src/Minsk/CodeAnalysis/Binding/Binder.cs#L267-L277