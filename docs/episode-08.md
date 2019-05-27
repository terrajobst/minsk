# Episode 8

[Video](https://www.youtube.com/watch?v=PfpayNvfu20&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=8) |
[Pull Request](https://github.com/terrajobst/minsk/pull/35) |
[Previous](episode-07.md) |
[Next](episode-09.md)

## Completed items

* Add support for bitwise operators
* Add ability to output the bound tree
* Add ability to lower bound tree
* Lower `for`-statements into `while`-statements
* Print syntax and bound tree before evaluation
* Lower `if`, `while`, and `for` into gotos

## Interesting aspects

### Lowering

Right now, the interpreter is directly executing the output of the binder. The
binder produces the bound tree, which is an abstract representation of the
language. It represents the semantic understanding of the program, such as the
symbols the names are bound to and the types of intermediary expressions.

Usually, this representation is as rich as the input language. That
characteristic is very useful as it allows exposing it to tooling, for example,
to produce code completion, tool tips, or even refactoring tools.

While it's possible to generate code directly out of this representation it's
not the most convenient approach. Many language constructs can be reduced, also
called *lowered*, to other constructs. That's because languages often provide
syntactic sugar that is merely a shorthand for other constructs. Take, for
example, the `for`-statement in our language. This code block:

```js
for i = 1 to 100
    <statement>
```

is just a shorthand for this `while`-statement:

```js
let i = 1
while i <= 100
{
    <statement>
    i = i + 1
}
```

Instead of having to generate code for both, `for`- and `while`-statements, it's
easier to reduce `for` to `while`.

To do this, we're adding the concept of a [BoundTreeRewriter]. This class has
virtual methods for all nodes that can appear in the tree and allows derived
classes to replace specific nodes. Since our bound tree is immutable, the
replacement is happening in a bottom up fashion, which is relatively efficient
for immutable trees because it only requires to rewrite the spine of the tree
(i.e. all ancestors of the nodes that need to be replaced); all other parts of
the tree can be reused.

The rewriting process looks as follows: individual methods simply rewrite the
components and only produce new nodes when any of them are different. For
example, this is how `if`-statements [are handled][if-node]:

```C#
protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
{
    var condition = RewriteExpression(node.Condition);
    var thenStatement = RewriteStatement(node.ThenStatement);
    var elseStatement = node.ElseStatement == null ? null : RewriteStatement(node.ElseStatement);
    if (condition == node.Condition && thenStatement == node.ThenStatement && elseStatement == node.ElseStatement)
        return node;

    return new BoundIfStatement(condition, thenStatement, elseStatement);
}
```

The [Lowerer] is derived from `BoundTreeRewriter` and handles the simplification
process. For example, this is how `for`-statements [are lowered][for-lowering]:

```C#
protected override BoundStatement RewriteForStatement(BoundForStatement node)
{
    // for <var> = <lower> to <upper>
    //      <body>
    //
    // ---->
    //
    // {
    //      var <var> = <lower>
    //      while (<var> <= <upper>)
    //      {
    //          <body>
    //          <var> = <var> + 1
    //      }
    // }

    var variableDeclaration = new BoundVariableDeclaration(node.Variable, node.LowerBound);
    var variableExpression = new BoundVariableExpression(node.Variable);
    var condition = new BoundBinaryExpression(
        variableExpression,
        BoundBinaryOperator.Bind(SyntaxKind.LessOrEqualsToken, typeof(int), typeof(int)),
        node.UpperBound
    );
    var increment = new BoundExpressionStatement(
        new BoundAssignmentExpression(
            node.Variable,
            new BoundBinaryExpression(
                    variableExpression,
                    BoundBinaryOperator.Bind(SyntaxKind.PlusToken, typeof(int), typeof(int)),
                    new BoundLiteralExpression(1)
            )
        )
    );
    var whileBody = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(node.Body, increment));
    var whileStatement = new BoundWhileStatement(condition, whileBody);
    var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(variableDeclaration, whileStatement));

    return RewriteStatement(result);
}
```

Please note that we call `RewriteStatement` at the end which makes sure that the
produced `while`-statement is lowered as well.

[BoundTreeRewriter]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Binding/BoundTreeRewriter.cs
[Lowerer]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Lowering/Lowerer.cs
[if-node]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Binding/BoundTreeRewriter.cs#L73-L82
[for-lowering]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Lowering/Lowerer.cs#L144-L182

### Gotos

Actual processors -- or even virtual machines like the .NET runtime -- usually
don't have representation for `if` statements, or specific loops such as `for`
or `while`. Instead, they provide two primitives: *unconditional jumps* and
*conditional jumps*.

In order to make generating code easier, we've added representations for those:
[BoundGotoStatement] and [BoundConditionalGotoStatement]. In order to specify
the target of the jump, we need a representation for the label, for which we use
the new [LabelSymbol], as well as a way to label a specific statement, for which
we use [BoundLabelStatement]. It's tempting to define the `BoundLabelStatement`
similar to how C# represents them in the syntax, which means that it references
a label and a statement but that's very inconvenient. Very often, we need a way
to create a label for whatever comes after the current node. However, since
nodes cannot navigate to their siblings, one usually cannot easily get "the
following" statement. The easiest way to solve this problem is by not
referencing a statement from `BoundLabelStatement` and simply have the semantics
that the label it references applies to the next statement.

With these primitives, it's pretty straightforward to replace the flow-control
elements. For example, this is how an `if` without an `else` [is
lowered][if-lowering]:

```C#
protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
{
    if (node.ElseStatement == null)
    {
        // if <condition>
        //      <then>
        //
        // ---->
        //
        // gotoFalse <condition> end
        // <then>
        // end:
        var endLabel = GenerateLabel();
        var gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, true);
        var endLabelStatement = new BoundLabelStatement(endLabel);
        var result = new BoundBlockStatement(ImmutableArray.Create<BoundStatement>(gotoFalse, node.ThenStatement, endLabelStatement));
        return RewriteStatement(result);
    }
    else
    {
        ...
    }
}
```

[BoundGotoStatement]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Binding/BoundGotoStatement.cs
[BoundConditionalGotoStatement]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Binding/BoundConditionalGotoStatement.cs
[BoundLabelStatement]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Binding/BoundLabelStatement.cs
[LabelSymbol]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/LabelSymbol.cs
[if-lowering]: https://github.com/terrajobst/minsk/blob/93d972db2d473b1e6bf5fb80ccbe0c3ddc0037d2/src/Minsk/CodeAnalysis/Lowering/Lowerer.cs#L56-L71