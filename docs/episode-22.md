# Episode 22

[Video](https://www.youtube.com/watch?v=EkDzge5WdAQ&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=22) |
[Pull Request](https://github.com/terrajobst/minsk/pull/123) |
[Previous](episode-21.md) |
[Next](episode-23.md)

## Completed items

* Perform constant folding
* [Contributor bonus items](https://github.com/terrajobst/minsk/pull/128):
   - Emit constant values
   - Use constant values in evaluator

## Interesting aspects

### Constant folding

It's pretty common in source code to have expression that involve constants.
However, right now we're evaluating their value at runtime. We can improve this
by detecting such expressions during compilation and pre-computing their values.
This technique is called [constant folding]. For example:

```JS
let x = 4 * 3
```

We know that `x` has the value `12`. This gets more interesting when we combine
this with boolean expressions that, such as:

```JS
let x = 4 * 3
while x > 12
{
   print(x)
   x = x - 1
}
```

Since we understand that `x` has the value `12` (and `x` cannot be changed), we
also know that the `while` loop will never execute. We could use that
information to provide a warning ([#136]) because there is a decent chance that
the developer made a mistake.

But this allows us to remove the entire body of `while` and, in this case, also
the condition as well. You might wonder what I mean by "in this case". Well,
expressions can have side-effects. For example:

```JS
while process(x) && x > 12
{
   doSomething(x)
}
```

Even though we know that the value of the condition is always `false`, not
evaluating it might change observable behavior of the program. For example, the
way it is written, the `process()` function would be called once before the loop
condition is evaluated to `false`. Depending on language semantics removing this
call completely might not be desirable.

In Minsk I'm declaring this as undesired behavior. However, we're currently not
handling this correctly ([#125]). We'll fix this when we also handle
short-circuit evaluation ([#111]).

[constant folding]: https://en.wikipedia.org/wiki/Constant_folding
[#111]: https://github.com/terrajobst/minsk/issues/111
[#125]: https://github.com/terrajobst/minsk/issues/125
[#136]: https://github.com/terrajobst/minsk/issues/136

### Folding without changing the tree

One can implement constant folding in various ways. One way is to perform
constant folding during binding by returning literals for constant expressions.

Let's consider this piece of code:

```JS
let x = false
let y = true
return x && y
```

For the `return`-statement the bound tree would normally look something like
this:

```text
          return
            |
     BinaryOperator &&
            |
    +-------+-------+
    |               |
Variable x      Variable y
```

When folding constants as part of binding we incrementally lower constant
expressions to literals. The idea here is that each `BindXxxExpression` method
(where `Xxx` supports constant folding) will generally also check wether it can
pre-compute its value and if so return a `BoundLiteralExpression`. This would
mean that the above tree would never be produced. Rather,
`BindVariableExpression` would have returned a literal expression and so would
`BindBinaryExpression`. The result is that the tree would immediately look as
follows:

```text
    return
      |
Literal false
```

This means the output of the `Bind` operation is lossy, i.e. we can't easily map
syntax nodes to bound nodes. I said *easily* because you could imagine that the
intermediate nodes are stored in a dictionary before being replaced but that's
still somewhat messy.

For batch compilers (i.e. text book compilers that only have a command line
mode) the loss of information isn't a big deal. However, in the context of an
IDE where you want to be able to ask questions later, not being able to
understand that `x` in `return x && y` was bound to the local variable `x` would
be quite unfortunate.

But ignoring the information loss, there is another issue with this approach.
Consider that we're also synthesizing bound nodes during compilation, for
example, by de-sugaring language constructs into simpler forms. If constant
folding requires modifying the tree then each step that creates/replaces nodes
must be careful to apply constant folding. Otherwise later phases might not be
able to detect certain expressions as constant.

So instead of modifying the tree we can model constant folding as a cross cutting
feature. We do this by [exposing a `ConstantValue` property][BoundExpression.ConstantValue]
on `BoundExpression`:

```C#
abstract partial class BoundExpression : BoundNode
{
   public virtual BoundConstant ConstantValue => null;
}
```

All expressions that support constant folding (such as unary expressions, binary
expressions, and variables) will [compute the constant][BoundBinaryExpression.ConstantValue]
in the constructor and override this property to return it:

```C#
partial class BoundBinaryExpression : BoundExpression
{
   public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
   {
      // ...
      ConstantValue = ConstantFolding.ComputeConstant(left, op, right);
   }

   // ...

   public override BoundConstant ConstantValue { get; }
}
```

But to make the code maintainable, the logic for constant folding is centralized
in the type `ConstantFolding`.

Looking at the constructor I just realized that I should have called the method
`Fold` rather than `ComputeConstant`. Future me, please fix this.

[BoundExpression.ConstantValue]: https://github.com/terrajobst/minsk/blob/7c070182829075d4664a05db3a4a21044f8cae39/src/Minsk/CodeAnalysis/Binding/BoundExpression.cs#L9
[BoundBinaryExpression.ConstantValue]: https://github.com/terrajobst/minsk/blob/episode22/src/Minsk/CodeAnalysis/Binding/BoundBinaryExpression.cs#L13
