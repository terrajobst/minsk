# Episode 12

[Video](https://www.youtube.com/watch?v=psTZi6xpTlM&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=12) |
[Pull Request](https://github.com/terrajobst/minsk/pull/55) |
[Previous](episode-11.md) |
[Next](episode-13.md)

## Completed items

We added support for explicit typing of variables and function declarations.

## Interesting aspects

### Procedures vs. functions

Some languages have different concepts for functions that return values and
functions that don't return values. The latter are often called *procedures*. In
C-style languages both are called functions except that procedures have a
special return type called `void`, that is to say they don't return anything.

In Minsk I'm doing the same thing except that you'll be able to omit the return
type specification, so code doesn't have to say `void`. In fact, the type `void`
cannot be uttered in code at all:

```
» function hi(name: string): void
· {
· }

(1, 28): Type 'void' doesn't exist.
    function hi(name: string): void
                               ^^^^
```

### Forward declarations

Inside of function bodies code is logically executing from top to bottom, or
more precisely from higher nodes in the tree to lower nodes in the tree. In that
context, symbols must appear before use because code is depending on side
effects.

However, outside of functions there isn't necessarily a well-defined order. Some
languages, such as C or C++, are designed to compile top to bottom in a [single
pass][single-pass], which means developers cannot call functions or refer to
global variables unless they already appeared in the file. In order to solve
problems where two functions need to [refer to each other][mutual recursion],
they allow [forward declarations], where you basically only write the signature
and omit the body, which is basically promising that you'll provide the
definition later.

Other languages, such as C#, don't do that. Instead, the compiler is using
[multiple phases][multi-pass]. For example, first everything is parsed, then all
types are being declared, then all members are being declared, and then all
method bodies are bound. This can be implemented relatively efficiently and
frees the developer from having to write any forward declarations or header
files.

In Minsk, we're using multiple passes so that global variables and functions can
appear in any order. We're doing this by first [declaring all functions] before
[binding function bodies].

[single-pass]: https://en.wikipedia.org/wiki/One-pass_compiler
[forward declarations]: https://en.wikipedia.org/wiki/Forward_declaration
[multi-pass]: https://en.wikipedia.org/wiki/Multi-pass_compiler
[mutual recursion]: https://en.wikipedia.org/wiki/Mutual_recursion
[declaring all functions]:  https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Binding/Binder.cs#L36-L37
[binding function bodies]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Binding/Binder.cs#L39-L45

### Stack frames

The evaluator currently only evaluates a single block. All variables are global
so there is only a single instance of them in the entire program, so having a
single dictionary that holds their value works.

In order to call functions, we need to have a way to let each function have
their own instance of their local variables, per invocation. Keep in mind that
in the symbol table there is only a single symbol for a local variable in any
given function, but each time you call that function, you need to create a new
storage location. Otherwise code will break in funny ways if you can end up
calling a function you're currently int the middle of executing already, for
example by recursion.

In virtually all systems this is achieved by using a stack. Each time you call a
function, a new entry is pushed on the stack that represents the local state of
the function, usually covering both the arguments as well as the local
variables. This is usually called a *stack frame*. Each time you return from a
function, the top most stack frame is popped off of that stack.

In Minsk, we're doing the same thing:

1. When calling a function, a new set of locals is initialized. All [parameters
   are added][params] to that new frame and that [frame is pushed][push].
2. The function's body is identified and the [statement is executed][call].
3. When the function is done, the frame is [popped off][pop].

This also required us to change how we assign & lookup values for variables: by
looking at the symbol kind we [identify] whether it's a global, a local variable
or parameter. Global variables use the global dictionary while local variables
and parameter use the current stack frame.

It might be tempting to check the contents of the global dictionary to see
whether a given variable is global, but this dictionary is currently populated
lazily, so the initial state is empty. We could change that but it's easier to
change the binder to [create different kind of symbols][symbol creation] for
global variables, local variables, and parameters. This also enables
higher-level components (such as an IDE) to treat them differently, for example,
by colorizing them differently, without having to walk the symbol table.

[params]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Evaluator.cs#L235-L241
[push]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Evaluator.cs#L243
[call]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Evaluator.cs#L245-L246
[pop]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Evaluator.cs#L248
[identify]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Evaluator.cs#L269-L277
[symbol creation]: https://github.com/terrajobst/minsk/blob/c4ad1b199a8e858a9e01535aead020471fbd86f2/src/Minsk/CodeAnalysis/Binding/Binder.cs#L462-L464