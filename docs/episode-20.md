# Episode 20

[Video](https://www.youtube.com/watch?v=k9WR5jueqgI&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=20) |
[Pull Request](https://github.com/terrajobst/minsk/pull/110) |
[Previous](episode-19.md) |
[Next](episode-21.md)

## Completed items

* Remove unnecessary references from compilation
* Expand implicit returns
* Emit conversions, unary expressions, and binary expressions
* Emit remaining statements
* Emit code for built-in rnd function

## Interesting aspects

### Filtering automatic references

In .NET Core, we made the decision to auto-reference most of the framework
assemblies by default. That makes sense because all these assemblies are always
available and there is no runtime penalty for superfluous references (because
compilers only add references to assemblies when the developer uses
any types from them).

The only downside is that this costs compilation time when large amounts of
assemblies are passed to the compiler. Fortunately, C#, F#, and VB are all fast
enough to handle this with ease. Unfortunately Minsk is not. And since we don't
allow users to reference arbitrary .NET types, we only need a handful of types
for specific language constructs. Hence, it makes sense to make our lives easier
by [excluding all
assemblies](https://github.com/terrajobst/minsk/blob/503e11650924ed19512c264b7bc0e761b25bde00/samples/Directory.Build.targets#L7-L10)
that we don't need:

```xml
<ItemGroup>
  <ReferencePath Remove="@(ReferencePath)"
                  Condition="'%(FileName)' != 'System.Runtime' AND
                             '%(FileName)' != 'System.Console' AND
                             '%(FileName)' != 'System.Runtime.Extensions'" />
</ItemGroup>
```

In case you didn't know: XML allows line breaks in attributes which makes this
nicer to read. Neat!

### Implicit returns

In most languages, the developer doesn't need to explicitly return from
procedures (reminder: a procedure is a method which doesn't return any value,
which C-based languages call void-functions).

However, this is not how things work at runtime. The return statement is
logically the thing that jumps back to the caller. Thus, in IL even empty
procedures need at least the `ret` instruction.

Right now, our intermediate representation (the bound nodes) don't contain
`BoundReturnStatement` nodes for procedures at the end. In the last episode,
we've cheated and manually added the `ret` instruction as part of emit. But
that's not very clean. The job of the emitter is only to produce IL; it
shouldn't alter the intermediate representation.

Fortunately, we already have a place where we can inject these implicit return
nodes, namely the [lowerer](https://github.com/terrajobst/minsk/blob/master/docs/episode-08.md#lowering).
We can just [add some logic to the `Flatten` method](https://github.com/terrajobst/minsk/blob/503e11650924ed19512c264b7bc0e761b25bde00/src/Minsk/CodeAnalysis/Lowering/Lowerer.cs#L53-L59):

```C#
if (function.Type == TypeSymbol.Void)
{
    if (builder.Count == 0 || CanFallThrough(builder.Last()))
    {
        builder.Add(new BoundReturnStatement(null));
    }
}
```

Implicit returns only occur at the end and are only necessary if the last
statement doesn't unconditionally go somewhere else, which at this point are
only `return` and `goto` statements.

### Syntactic sugar

While IL has instructions for almost all operators it doesn't provide
instructions for all of them. For example, IL has instructions for `==` (`ceq`),
`<` (`clt`), and `>` (`cgt`). But it doesn't have instructions for `!=`, `<=`,
and `>=`. The reason being that they are not required because they can be
defined as the negation of other instructions. For example `a <= b` is the same
as `!(a > b)` and `a != b` is the same as `!(a == b)`.

And logical negation is simply achieved by comparing the current value on the
stack with `0`:

```text
ldc.i4.0    // load false
ceq         // compare for equality
```

The additional operators in the input language are often called *syntactic
sugar*, which is loosely defined as anything that doesn't provide more
expressiveness but allows developers to express things in more concise fashion.

During compilation the inverse happens, which is often called *de-sugaring* or
lowering. However, in our case we de-sugar operators during emit. We could have
also done this as part of lowering.

### Backpatching

All control flow is expressed as jumps. In assembly language the target of a
jump is usually a label. However, labels are only used to make things readable
for a human. The underlying instruction sets don't allow for labels; instead,
addresses are being used. IL is no different: the jump address is a byte
position within the IL stream.

This poses a problem when the target of a jump is to a label that exits further
down, for example:

```
    gotoTrue a <= b break
    ...
break:
    return 4
```

At the time we're asked to emit the instruction for `gotoTrue a <= b break` we
don't know the address of `return 4` yet (because it depends on the size of
all instructions in between).

The usual approach for this is simple: we emit the instruction with a bogus
address and patch it later when we know the address for `return 4`. This is
possible because the size of the jump instruction doesn't depend on the actual
value of the address (for example, because it's spec'ed to be a 32-bit number).

Since we're using `Mono.Cecil` we don't have to deal with addresses directly.
But the problem is similar. The jump op code takes an argument of type
`Instruction` which we haven't constructed yet. So we're just manufacturing a
fake instruction (a nop, which is an instruction that does nothing). Then
[we're recording the instruction in a fix-up table](https://github.com/terrajobst/minsk/blob/episode20/src/Minsk/CodeAnalysis/Emit/Emitter.cs#L268-L286) which we [patch after all
instructions](https://github.com/terrajobst/minsk/blob/episode20/src/Minsk/CodeAnalysis/Emit/Emitter.cs#L218-L225) in the functions have been emitted.