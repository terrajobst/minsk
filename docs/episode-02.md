# Episode 2

[Video](https://www.youtube.com/watch?v=3XM9vUGduhk&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=3) |
[Pull Request](https://github.com/terrajobst/minsk/pull/3) |
[Previous](episode-01.md) |
[Next](episode-03.md)

## Completed items

* Generalized parsing using precedences
* Support unary operators, such as `+2` and `-3`
* Support for Boolean literals (`false`, `true`)
* Support for conditions such as `1 == 3 && 2 != 3 || true`
* Internal representation for type checking (`Binder`, and `BoundNode`)

## Interesting aspects

### Generalized precedence parsing

In the [first episode](episode-01.md), we've written our recursive descent
parser in such a way that it parses additive and multiplicative expressions
correctly. We did this by parsing `+` and `-` in one method (`ParseTerm`) and
the `*` and `/` operators in another method `ParseFactor`. However, this doesn't
scale very well if you have a dozen operators. In this episode, we've replaced
this with [unified method][precedence-parsing].

[precedence-parsing]: https://github.com/terrajobst/minsk/blob/b9e0a3f8858b410ead4afbc3e165c316a628208e/mc/CodeAnalysis/Syntax/Parser.cs#L69-L96

### Bound tree

Our first version of the evaluator was walking the syntax tree directly. But the
syntax tree doesn't have any *semantic* information, for example, it doesn't
know which types an expression will be evaluating to. This makes more
complicated features close to impossible, for instance having operators that
depend on the input types.

To tackle this, we've introduced the concept of a *bound tree*. The bound tree
is created by the [Binder][binder] by walking the syntax tree and *binding* the
nodes to symbolic information. The binder represents the semantic analysis of
our compiler and will perform things like looking up variable names in scope,
performing type checks, and enforcing correctness rules.

You can see this in action in [Binder.BindBinaryExpression][bind-binary] which
binds `BinaryExpressionSyntax` to a [BoundBinaryExpression][bound-binary]. The
operator is looked up by using the types of the left and right expressions in
[BoundBinaryOperator.Bind][bind-binary-op].

[binder]: https://github.com/terrajobst/minsk/blob/9fa4ecb5347575cd5699afb659074c76f3f2e0fa/mc/CodeAnalysis/Binding/Binder.cs
[bind-binary]: https://github.com/terrajobst/minsk/blob/9fa4ecb5347575cd5699afb659074c76f3f2e0fa/mc/CodeAnalysis/Binding/Binder.cs#L48-L60
[bound-binary]: https://github.com/terrajobst/minsk/blob/9fa4ecb5347575cd5699afb659074c76f3f2e0fa/mc/CodeAnalysis/Binding/BoundBinaryExpression.cs#L5-L18
[bind-binary-op]: https://github.com/terrajobst/minsk/blob/9fa4ecb5347575cd5699afb659074c76f3f2e0fa/mc/CodeAnalysis/Binding/BoundBinaryOperator.cs#L50-L59
