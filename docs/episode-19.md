# Episode 19

[Video](https://www.youtube.com/watch?v=Ecrv8sCYEbA&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=19) |
[Pull Request](https://github.com/terrajobst/minsk/pull/96) |
[Previous](episode-18.md) |
[Next](episode-20.md)

## Completed items

* Replaced hard-coded IL emitter for Hello World by one that uses our
  intermediate representation
* Emit `input()` and variables
* Emit `string` concatenation
* Emit assignments
* Emit non-`void` functions and parameters

## Interesting aspects

### Encoding of IL instructions

IL instructions are ultimately bytes. Most instruction sets try to optimize for
size while also making sure that the format is extensible and flexible. IL is no
different. Instructions come in two forms, by themselves or with a parameter,
which is called an intermediate. The intermediate is used as an additional
parameter for the instruction, for example, the local variable being stored, the
method being called or the literal being loaded. IL only allows for a single
intermediate, which is why many instructions don't arguments directly but
instead use the evaluation stack. For example, the `add` instruction doesn't
take two arguments but instead takes them from the stack.

The general instruction for loading 32-bit integer values is `ldc.i4`. The
intermediate is the value, for example `ldc.i4 42` loads the value `42`. Since
some values are extremely common in programs (such as `0` and `1`) there are
special instructions who don't need an intermediate because the instruction
itself represents the value being loaded, for example `ldc.i4.0` just loads the
value `0`. This reduces the size of IL.

As a compiler writer, dealing with these special encodings can be tedious but
fortunately we don't have to. We're using `Mono.Cecil` for emitting IL and it
has the handy `body.OptimizeMacros()` method which will replace instructions
accordingly.

### Booleans in IL

While `System.Boolean` is a type, IL itself has no instructions that deal with
Booleans. Instead, a Boolean is just a 32-bit integer. There are no instructions
to load the values `true` and `false`. Instead, we can use `ldc.i4.0` (`false`)
and `ldc.i4.1` (`true`).

### Local variables and parameters in IL

In IL, locals and parameters aren't referred to by name. In fact, the metadata
format doesn't even record the names of locals (it is, however, part of the
debugging information).

They are referred to by index. For example `ldloc.0` loads the first local while
`stloc.2` writes to the third.

Instance methods have an implied parameter that refers to the instance, which in
C# is available via the `this` keyword. In IL, it's simply the first parameter,
`ldarg.0`. In static methods, `ldarg.0` is still valid, but it will refer to the
first actual parameter.
