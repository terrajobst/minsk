# Episode 21

[Video](https://www.youtube.com/watch?v=JSFZ3qDx83g&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=21) |
[Pull Request](https://github.com/terrajobst/minsk/pull/115) |
[Previous](episode-20.md) |
[Next](episode-22.md)

## Completed items

* Replaced reflection code in `SyntaxNode.GetChildren()` with source generator
* Made source generator work in VS Code and CLI/CI as well

## Interesting aspects

### Source Generators

Reflection and Reflection Emit are powerful tools. They are also fairly
convenient and easy to debug. And used right, they can create flexible
solutions.

At the same time, they come at a price. Any type of runtime dynamism makes your
code harder to reason about statically. And this also burdens the compiler,
especially [ahead-of-time (AoT) compilers][aot] because it makes optimizations
more difficult, if not outright impossible. That's not to say that reflection is
super slow; it's just that there is overhead. Some of that is conceptual in
nature while others are limitations of the reflection API itself (for example,
it causes boxing).

For many of the scenarios where we use reflection it's not that we need to use a
dynamic solution; it's just that reflection is so much more convenient to use.
For example, instead of serializing a type by hand it's easier to use a
serializer.

Enter [Roslyn Source Generators][blog]. To quote the post:

> A Source Generator is a piece of code that runs during compilation and can
> inspect your program to produce additional files that are compiled together
> with the rest of your code.

![Compilation pipeline with source generators][pipeline]

In other words, a source generator can *reflect* over the types in your *source*
and *generate additional source*. This allows, for example, implementing a
serializer that performs the same way as handwritten serializer (i.e. with no
additional overhead) but can be used as conveniently as a reflection-based
serializer.

Generators are very similar to [analyzers]: you can think of an *analyzer* as a
thing that looks at your source code and generates diagnostics (e.g. a warning
or an error) while a *generators* is a thing that looks at your source code and
generates additional source code.

For more details, check out the [blog post][blog], the [proposal][proposal] and
the [cookbook][cookbook].

[aot]: https://en.wikipedia.org/wiki/Ahead-of-time_compilation
[blog]: https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/
[pipeline]: https://devblogs.microsoft.com/dotnet/wp-content/uploads/sites/10/2020/04/Picture1.png
[proposal]: https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.md
[cookbook]: https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.cookbook.md
[analyzers]: https://docs.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview

### Referencing a Source Generator

Generators are referenced the same way as analyzers: both use the `analyzer`
folder in a NuGet package and the `Analyzer` item type in MSBuild.

As a library author it often makes sense to ship analyzers for your APIs as a
NuGet package. But when it comes to generators I'm hard-pressed to think of
examples where shipping a general purpose generator would have been useful.
Virtually all the cases I can think of I'd be using a generator to simplify the
implementation of my library. In other words, I'd be the only consumer of it.
That's not say that all generators are like that. For example, the [cookbook]
has examples of features we might end up shipping as part of the .NET SDK. I'm
merely talking about my own experience, which I think many other developers will
also share.

As such, I don't really want to reference the analyzer as a binary or even a
NuGet package, I'd rather reference it as a project.

[Rainer Sigwald][tashkant] from the MSBuild team [posted][tweet] which shows a
neat way to achieve this:

```XML
<ProjectReference Include="..\Minsk.Generators\Minsk.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

This does three things for us:

1. Using a project reference ensures that the generator is built before the
   consuming project is being built.

2. Setting `OutputItemType` to `Analyzer` means that MSBuild will put the path
   to the DLL into an `Analyzer` item (rather than a `Reference` item).

3. Setting `ReferenceOutputAssembly` to `false` means the consuming project
   doesn't have a runtime dependency on the generator.

Isn't that elegant? If you haven't, make sure to follow [Rainer][tashkant]!

[tashkant]: https://twitter.com/Tashkant
[tweet]: https://twitter.com/Tashkant/status/1256749223850266624

### Source Generators in VS

Please note that generators are currently in preview. As such, not everything
works yet. In order to get generator support in VS you need to be on the latest
VS preview (AFAIK 16.6.0 Preview 5.0 and higher).

Examples of current limitations:

* **No unloading**. Visual Studio currently can't unload a previously loaded
  generator. If you reference the analyzer as a binary, then this is usually not
  an issue. However, when you do this as a project reference, then this can be a
  problem because the consuming project can't pick up changes you made to the
  generator. You can work this around by reloading the solution. For inner loop
  testing of the generator you can build on the CLI.

* **No easy way to debug**. You can't just F5 your generator project. You can
  work this around by either manually attaching a debugger (by calling
  `Debugger.Launch()` from your generator's methods) or by manually configuring
  the debug launch options. You can cheat by using MSBuild's structured log
  viewer to copy the CSC.EXE invocation from your consuming project.

* **Source is not embedded**. Generator source code is not written to disk.
  Instead, the compiler uses it from memory. When debugging the generated code
  in the consuming project this can be a problem. The idea is that eventually
  the source code will be embedded in the debugging symbols (*.pdb).

* **Not always run**. It's not practical for the IDE to rerun all generators
  after every keystroke. As far as I can tell, VS will run the generator when
  open a solution and each time you build. When you're editing, VS seems to have
  some heuristic to determine whether or not run your generator. You can tweak
  that heuristic in your generator's `Initialize` method by subscribing to
  syntax change notifications.

### Source Generators in VS Code

Continuing on the "not everything works yet"-train: Visual Studio Code, or more
specifically the C# extension, doesn't support generators at all yet. That's a
bummer for us because that's the IDE we're using for Minsk.

I spent half of this episode (~2hrs out of ~4hrs) and quite some time after it
to find a viable workaround that makes generators work in VS Code, the command
line, and on the CI machine.

Here is the summary:

1. The current .NET 5 preview (SDK 5.0.100-preview.3) has a bug and doesn't
   support generators. [We work this around][compiler-ref] by referencing the
   compiler as a NuGet package.

2. To fix VS Code, we [persist the generated code][persist] by making our
   generator write it to a file.

3. While adding the file to the source directory fixes VS Code it now breaks VS
   and regular command line builds because they now see both the generated code
   as well as the file written to disk. We can fix that by [excluding the
   generated code][exclude] in MSBuild. Since VS Code doesn't evaluate the
   MSBuild project, this will cause the generated code to be part of the project
   while neither VS nor the command line build will see that file.

Happy generating! 🖖

[compiler-ref]: https://github.com/terrajobst/minsk/blob/2b220d1b91ae0cecc9797a7744a7f7704ef3322b/src/Directory.Build.targets#L3-L13
[persist]: https://github.com/terrajobst/minsk/blob/2b220d1b91ae0cecc9797a7744a7f7704ef3322b/src/Minsk.Generators/SyntaxNodeGetChildrenGenerator.cs#L102-L121
[exclude]: https://github.com/terrajobst/minsk/blob/2b220d1b91ae0cecc9797a7744a7f7704ef3322b/src/Directory.Build.props#L8-L26
