# Episode 17

[Video](https://www.youtube.com/watch?v=Lsi1Itrzyl4&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=17) |
[Pull Request](https://github.com/terrajobst/minsk/pull/89) |
[Previous](episode-16.md) |
[Next](episode-18.md)

## Completed items

* Introduce `Compilation.IsScript` and use it to restrict expression statements
* Support implicit argument conversions when calling functions
* Add `any` type
* Lower global statements into `main` function

## Interesting aspects

### Regular vs. script mode

In virtual all C-like languages some expressions are also allowed as statements.
The canonical examples are assignments and expressions:

```JavaScript
x = 10
print(string(x))
```

Syntactically, this also allows for other expressions such as

```JavaScript
x + 1
```

Normally these expressions are pointless because their values aren't observed.
Strictly speaking these expressions aren't pure, for instance `f()` could have a
side effect here:

```JavaScript
x + f(3)
```

But the top level binary expression will produce a value that's not going
anywhere, which is most likely indicative that the developer made a mistake.
Hence, most C-like languages disallow or at least warn when they encounter these
expressions.

However, when entering code in a REPL these expression are super useful. And
their return value is observed by printing it back to the console.

To differentiate between the two modes we're changing our `Compilation` to be in
either script-  or in regular mode:

* **regular mode** will only allow *assignment-* and *call expressions* inside
  of expression statements while

* **script mode** will allow any expression so long the containing statement is
  a global statement (in other words as soon as the statement is part of a block
  it's like in regular mode).

### Lowering global statements

We'd like our logical model to be that all code is contained in a function. For
regular programs that are compiled that means we're expected to have a `main`
function where execution begins. `main` takes no arguments and returns no value
(for simplicity, we can change that later).

In script mode, we want a script function that takes no arguments and returns
`any` (that is an expression of any type, like `object` in C#).

For ease of use we'll still allow global statements in our language which means
we're ending with these modes:

* **Regular mode**. The developer can use global statements or explicitly
  declare a `main` function. When global statements are used, the compiler will
  synthesize a `main` function that will contain those statements. That's why
  using both global statements and a `main` function is illegal. Furthermore, we
  only allow one syntax tree to have global statements because unless we allow
  the developer to control the order of files, the execution order between them
  would be ill-defined.

* **Script mode**. The developer can declare a function called `main` in script
  mode but the function isn't treated specially and thus doesn't conflict with
  global statements. When global statements are used, they are put in a
  synthesized function with a name that the developer can't use (this avoids
  naming conflicts).

That means regardless of form, both models end up with a collection of functions
and no global statements. Having this unified shape will make it easier to
generate code later.
