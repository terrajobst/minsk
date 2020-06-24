# Episode 27

[Video](https://www.youtube.com/watch?v=pfEJJ9SAppE&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=27) |
[Pull Request](https://github.com/terrajobst/minsk/pull/157) |
[Previous](episode-26.md) |
[Next](episode-28.md)

## Completed items

* Add `SyntaxNode.Parent`
* Add `SyntaxNode.Ancestors()` and `AncestorsAndSelf()`
* Add `BoundNode.Syntax`

## Interesting aspects

### Adding a parent property to an immutable tree

For an IDE it's super useful to be able to walk the syntax nodes from the leaves
upwards. This allows us to build a single API that, given a position, can return
the token it is contained in. Calling code can then walk the parents until it
finds the node it's interesting in (for example, the containing expression,
statement, or function declaration).

However, in order to construct an immutable tree, one has to construct the
children before the parent, which is exactly what our parser does. This raises
the question how one can possibly have a `Parent` property on `SyntaxNode`.

The answer is: we can cheat.

The parser is executed from the constructor of `SyntaxTree`. This allows the
parser to pass in the `SyntaxTree` to each node. Since the `SyntaxTree` provides
access to the syntax root, we can add a method on `SyntaxTree` that constructs a
dictionary from child-to-parent, which we'll produce upon first request, also
known as [lazy initialization].

Lazy initialization is a common technique for immutable data structures but one
thing to watch out for are race conditions. One of the reasons why we make
things immutable is so that we can freely pass them around to background threads
and not worry about multi-threading bugs because the data structures can't be
modified.

Strictly speaking, lazy initialization violates this because we have a side
effect that writes to an underlying field. The trick is making sure this side
effect is unobservable. We achieve this by using `Interlocked.CompareExchange`
from [SyntaxNode.GetParent][syntaxtree-getparent]:

```C#
internal SyntaxNode? GetParent(SyntaxNode syntaxNode)
{
    if (_parents == null)
    {
        var parents = CreateParentsDictionary(Root);
        Interlocked.CompareExchange(ref _parents, parents, null);
    }

    return _parents[syntaxNode];
}
```

Logically, this code

```C#
Interlocked.CompareExchange(ref _parents, parents, null);
```

is equivalent to this code:

```C#
if (_parents == null)
    _parents = parents;
```

but it does so in an atomic fashion, that is to say, between the check and the
assignment no other thread will have the opportunity to write to the underlying
field.

The net effect is this: some thread will be the first to assign to this field
and all other threads will see this value. However, please note that multiple
threads might have have called `CreateParentsDictionary` but only one thread
will succeed in storing the result in the `_parent` field. So this only works if
multiple threads can call `CreateParentsDictionary` without problems, which
generally means it shouldn't have any observable side effects either.

In the literature, `CompareExchange` is often referred to as [compare-and-swap].

You might wonder why it matters that only one thread succeeds here. In the end,
it doesn't matter if we were to overwrite the previous dictionary because it
contains the same information. That is true here. But sometimes you construct
objects that are publicly visible. Changing already observed instances to new
ones (even if they are logically equivalent) can break observers because object
identity changes. So don't do that.

[lazy initialization]: https://en.wikipedia.org/wiki/Lazy_initialization
[syntaxtree-getparent]: https://github.com/terrajobst/minsk/blob/73462c1a8b4e876bd326340350015141cec2673e/src/Minsk/CodeAnalysis/Syntax/SyntaxTree.cs#L103-L112
[compare-and-swap]: https://en.wikipedia.org/wiki/Compare-and-swap

### Map bound nodes to syntax nodes

The bound tree is produced by the binder by walking the syntax tree. Right now,
the bound node doesn't record the syntax node it was produced from, but having
this information would be super useful:

1. **It allows mapping syntax nodes to bound nodes**. In order to provide code
   completion, we need to know what type a given `ExpressionSyntax` was bound
   to. If we can produce a mapping from `SyntaxNode` to `BoundNode` we can just
   look up the `BoundExpressionNode` and return its `Type` property.

2. **Allows debugging**. In order to make debugging possible, we need to
   associate the produced code (IL or machine code) to specific locations in the
   source file. This information is often called "debugging symbols". This
   requires the emitter to be able to get this information from a bound node.
   For this, it would be quite convenient if `BoundNode` had a `Syntax`
   property.

3. **Error reporting**. Some errors are only being detected during lowering or
   even emit. For example, let's say a specific language construct requires a
   particular API in the .NET platform in order to work. It might be
   inconvenient to detect this during lowering rather than during binding.
   However, during lowering and emit we operate on `BoundNodes`, so once again
   it would be quite convenient to have `BoundNode.Syntax`.

This isn't complicated, but it's busy work as we now require `SyntaxNode`
parameter to be added to all `BoundNode` types.
