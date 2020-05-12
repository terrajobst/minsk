# Episode 23

[Video](https://www.youtube.com/watch?v=bpVFmD_JYuU&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=24) |
[Pull Request](https://github.com/terrajobst/minsk/pull/130/files) |
[Previous](episode-22.md) |
[Next](episode-24.md)

## Completed items

* Added single-line and multi-line comments
* Made sure multi-line comments are rendered properly in the REPL

## Interesting aspects

### Comments are nice

If you think to yourself "why does he think that's worth mentioning" consider
that JSON doesn't have comments :-)

Also, while we shamelessly borrowed the comments from the C family, it's worth
mentioning what these choices mean:

1. **Comments are valid everywhere where whitespace is valid**. Again, you might
   be tempted to say "duh", but consider that's not the case in all languages.
   For example, in XML comments are only valid where elements are valid. In
   other words, you can't comment out individual attributes.
2. **We support both single-line and multi-line comments**. Single line comments
   are nice because you don't have to write a terminator nor do you have to
   worry about escaping them.
3. **We don't support nested multi-line comments**. For example, `/* /* */ */`
   isn't valid. The comment is terminated by the first `*/` which causes a
   syntax error for the second one.

### Line-independent syntax highlighting

Right now, we don't have any tokens that span multiple lines. This makes syntax
highlighting rather simple: we can tokenize each line independently. With the
advent of multi-line comments this is no longer the case. Consider I have
several lines of code. Now let's say I insert a new line at the beginning and
start typing `/*`. We now have to repaint multiple lines because they are all
considered being part of the comment.

A common trick that fast syntax highlighters use is that they will track a state
per line. Usually that state is just an integer representing the initial state
the tokenizer should be considered in when tokenizing the next line. In our case
there would be only two: regular state and an in-comment state.

We're doing a simpler version of that and simply say that [the state] is the
same for all lines: the fully tokenized input.

```C#
private sealed class RenderState
{
   public RenderState(SourceText text, ImmutableArray<SyntaxToken> tokens)
   {
         Text = text;
         Tokens = tokens;
   }

   public SourceText Text { get; }
   public ImmutableArray<SyntaxToken> Tokens { get; }
}
```

When rendering, [we pass the array of lines, the index of the current line, and
the previous line's state][RenderLine]. If the previous line's render state is
`null`, we tokenize the input:

```C#
protected override object RenderLine(IReadOnlyList<string> lines, int lineIndex, object state)
{
   RenderState renderState;

   if (state == null)
   {
         var text = string.Join(Environment.NewLine, lines);
         var sourceText = SourceText.From(text);
         var tokens = SyntaxTree.ParseTokens(sourceText);
         renderState = new RenderState(sourceText, tokens);
   }
   else
   {
         renderState = (RenderState) state;
   }

   // ...
}
```

For the actual rendering we only look at tokens that [overlap with the span of
the current line][tokenLoop]. We also have to make sure that we trim the token
to the line start and line end:

```C#
var lineSpan = renderState.Text.Lines[lineIndex].Span;

foreach (var token in renderState.Tokens)
{
      if (!lineSpan.OverlapsWith(token.Span))
         continue;

      var tokenStart = Math.Max(token.Span.Start, lineSpan.Start);
      var tokenEnd = Math.Min(token.Span.End, lineSpan.End);
      var tokenSpan = TextSpan.FromBounds(tokenStart, tokenEnd);
      var tokenText = renderState.Text.ToString(tokenSpan);

      // Print token
}
```

And that's it.

[the state]: https://github.com/terrajobst/minsk/blob/521bdcd435b813b7b43bd9161ac5041fdc2c8f66/src/msi/MinskRepl.cs#L28-L38
[RenderLine]: https://github.com/terrajobst/minsk/blob/521bdcd435b813b7b43bd9161ac5041fdc2c8f66/src/msi/MinskRepl.cs#L40-L54
[tokenLoop]: https://github.com/terrajobst/minsk/blob/521bdcd435b813b7b43bd9161ac5041fdc2c8f66/src/msi/MinskRepl.cs#L56-L90