# Episode 9

[Video](https://www.youtube.com/watch?v=QwZuY1dExAc&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=9) |
[Pull Request](https://github.com/terrajobst/minsk/pull/40) |
[Previous](episode-08.md) |
[Next](episode-10.md)

## Completed items

This episode doesn't have much to do with compiler building. We just made the
REPL a bit easier to use. This includes the ability to edit multiple lines, have
history, and syntax highlighting.

## Interesting aspects

### Two classes

The REPL is split into two classes:

* [Repl] is a generic REPL editor and deals with the interception of keys and
  rendering.
* [MinskRepl] contains the Minsk specific portion, specifically evaluating the
  expressions, keeping track of previous compilations, and using the parser to
  decide whether a submission is complete.

I haven't done this to reuse the REPL, but to make it easier to maintain. It's
not great if the language specific aspects of the REPL are mixed with the
tedious components of key processing and output rendering.

## Document/View

The REPL uses a simple document/view architecture to update the output of the
screen whenever the document changes.

[Repl]: https://github.com/terrajobst/minsk/blob/69123841304be0b9be0c5dc451c20fa07742f567/src/mc/Repl.cs
[MinskRepl]: https://github.com/terrajobst/minsk/blob/69123841304be0b9be0c5dc451c20fa07742f567/src/mc/MinskRepl.cs