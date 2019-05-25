# Episode 13

[Video](https://www.youtube.com/watch?v=NvVc8erZpeI&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=13) |
[Pull Request](https://github.com/terrajobst/minsk/pull/61) |
[Previous](episode-12.md) |
[Next](episode-14.md)

## Completed items

We added pretty printing for bound nodes as well as `break` and `continue`
statements.

## Interesting aspects

### Break and continue

Logically, all `if` statements and loops are basically just `goto`-statements.
In order to support `break` and `continue`, we only have to make sure that all
[loops have predefined labels][bound-loop] for `break` and `continue`. That
means that we can just bind them to a `BoundGotoStatement`.

During binding, we only have to track the current loop by using a
[stack][loop-stack] that has a tuple of `break` and `continue` labels. When
binding a loop body, we [generate][bind-loop-body] labels for `break` and
`continue` and push them onto that stack. And for [binding `break` and
`continue`][bind-break-continue], we only have to use the corresponding label
from the stack.

[bound-loop]: https://github.com/terrajobst/minsk/blob/3982452187b615acd60db8ec2d26a3b0cf924c44/src/Minsk/CodeAnalysis/Binding/BoundLoopStatement.cs#L11-L12
[loop-stack]: https://github.com/terrajobst/minsk/blob/3982452187b615acd60db8ec2d26a3b0cf924c44/src/Minsk/CodeAnalysis/Binding/Binder.cs#L17
[bind-loop-body]: https://github.com/terrajobst/minsk/blob/3982452187b615acd60db8ec2d26a3b0cf924c44/src/Minsk/CodeAnalysis/Binding/Binder.cs#L268-L279
[bind-break-continue]: https://github.com/terrajobst/minsk/blob/3982452187b615acd60db8ec2d26a3b0cf924c44/src/Minsk/CodeAnalysis/Binding/Binder.cs#L281-L303

### Binder state

The current design of the binder has mutable state. The assumption is that the
binder is only used in one of two cases:

1. [Binding global scope][bind-global-scope]. Since we want to allow developers
   to declare functions in any order, we first need to bind the global scope,
   that is declare all global variables and functions. Modulo diagnostics, this
   requires no state.

2. [Binding function bodies][bind-function-body]. Given the bound global scope,
   we then create a binder per function body for binding. This means the state
   on the binder can assume that all its state is for the current function. In
   other words, we don't have to worry that our loop stack would allow one
   function to accidentally transfer control to a statement in another function.

This separation also makes it easy to parallelize the compiler. For example, we
could bind all function bodies in parallel.

[bind-global-scope]: https://github.com/terrajobst/minsk/blob/3982452187b615acd60db8ec2d26a3b0cf924c44/src/Minsk/CodeAnalysis/Binding/Binder.cs#L33-L57
[bind-function-body]: https://github.com/terrajobst/minsk/blob/3982452187b615acd60db8ec2d26a3b0cf924c44/src/Minsk/CodeAnalysis/Binding/Binder.cs#L72-L73