# Episode 11

[Video](https://www.youtube.com/watch?v=98AI4XJA7Xk&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=11) |
[Pull Request](https://github.com/terrajobst/minsk/pull/50) |
[Previous](episode-10.md) |
[Next](episode-12.md)

## Completed items

We added support for calling built-in functions and convert between types.

## Interesting aspects

### Separated syntax lists

When parsing call expressions, we need to represent the list of arguments. One
might be tempted to just use `ImmutableArray<ExpressionSyntax>` but this begs
the question where the comma between the arguments would go in the resulting
syntax tree. One could say that they aren't recorded but for IDE-like
experiences we generally want to make sure that the syntax tree can be
serialized back to a text document at full fidelity. This enables refactoring by
modifying the syntax tree but it also makes navigating the tree easier if we can
assume that locations can always be mapped to a token and thus a containing
node.

We could decide to introduce a new node like `ArgumentSyntax` that allows us to
store the comma as an optional token. However, this also seems odd because
trailing commas would be illegal as well as missing intermediary commas. Also,
it easily breaks if we later support, say, reordering of arguments because we'd
also have to move the comma between nodes. In short, this structure simply
violates the mental model we have.

So instead of doing any of that, we're introducing a special kind of list we
call [`SeparatedSyntaxList<T>`][SeparatedSyntaxList] where `T` is a
`SyntaxNode`. The idea is that the list is constructed from a list of nodes and
tokens, so for code like

```
add(1, 2)
```

the separated syntax list would contain the expression `1`, the comma and the
expression `2`. Enumerating and indexing the list will generally skip the
separators (so that `Arguments[1]` would give you the second argument rather
than the first comma), however, we have a method `GetSeparator(int index)` that
returns the associated separator, which we define as the next token. For the
last node it will return `null` because the last node doesn't have a trailing
comma.

[SeparatedSyntaxList]: https://github.com/terrajobst/minsk/blob/b7f24a0c2536570681923709e0516626f87d3a57/src/Minsk/CodeAnalysis/Syntax/SeparatedSyntaxList.cs

### Built-in functions

We cannot declare functions yet so we added a set of [built-in functions] that
we [special case in the evaluator][func-eval]. We currently support:

* `print(message: string)`. Writes to the console.
* `input(): string`. Reads from the console.
* `rnd(max: int)`. Returns a random number between `0` and `max - 1`.

[built-in functions]: https://github.com/terrajobst/minsk/blob/53d729f2bca858b510b6c8bc3ed9a200cbd62099/src/Minsk/CodeAnalysis/Symbols/BuiltinFunctions.cs#L11-L13
[func-eval]: https://github.com/terrajobst/minsk/blob/53d729f2bca858b510b6c8bc3ed9a200cbd62099/src/Minsk/CodeAnalysis/Evaluator.cs#L195-L219

### Scoping

When we start to compile actual source files, we'll only allow declaring
functions in the global scope, i.e. you won't be able to declare functions
inside of functions.

However, in a REPL experience we often want to declare a function again so that
we can fix bugs. The same applies to variables. We handled this by making new
submissions logically nested inside the previous submission. This gives us the
ability to see all previously declared functions and variables but also allows
us to redefine them. If they would all be in the same scope, we'd produce errors
because we generally don't want to allow developers to declare the same variable
multiple times within the same scope.

To handle the [built-in functions], we're adding an [outermost scope] that
contains them. This also allows developers to redefine the built-in functions if
they wanted to.

[outermost scope]: https://github.com/terrajobst/minsk/blob/53d729f2bca858b510b6c8bc3ed9a200cbd62099/src/Minsk/CodeAnalysis/Binding/Binder.cs#L59-L67

### Conversions

One simple program we'd like to write is this:

```
for i = 1 to 10
{
    let v = rnd(100)
    print(v)
}
```

However, the `print` functions takes a `string`, not an `int`. We need some kind
of conversion mechanism. We're using a similar syntax to Pascal where casting
looks like function calls:

```
for i = 1 to 10
{
    let v = rnd(100)
    print(string(v))
}
```

To bind them, we [check][bind-conversion] wether the call has exactly one
argument and the name is resolving to a type. If so, we bind it as a conversion
expression, otherwise as a regular function invocation.

In order to decide which conversions are legal, we introduce the [`Conversion`]
class. It tells us whether a given conversion from `fromType` to `toType` is
valid and what kind it is. Right now, we don't support implicit conversions, but
we'll add those later.

[bind-conversion]: https://github.com/terrajobst/minsk/blob/eccaa40146d4330d9e8e2c48668135249edf9605/src/Minsk/CodeAnalysis/Binding/Binder.cs#L293-L294
[`Conversion`]: https://github.com/terrajobst/minsk/blob/eccaa40146d4330d9e8e2c48668135249edf9605/src/Minsk/CodeAnalysis/Binding/Conversion.cs