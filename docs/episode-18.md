# Episode 18

[Video](https://www.youtube.com/watch?v=9b8-okd-ZkU&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y&index=18) |
[Pull Request](https://github.com/terrajobst/minsk/pull/93) |
[Previous](episode-17.md) |
[Next](episode-19.md)

## Completed items

* Add (fixed) IL emitter for Hello World
* Add MSBuild project file for Minsk

## Interesting aspects

### Choosing a metadata library

Before we can even think of IL emitting we need to talk about tools. In order to
read and write .NET metadata we need to use some library.

You might be tempted to use `System.Reflection` but that that's not appropriate
for a compiler. The reason being that reflection is primarily used for executing
code. Thus, opening an assembly with reflection means loading said assembly into
the .NET runtime your app is running on. And since a runtime can only load
assemblies that were built for the same runtime this would prevent Minsk from
compiling for different .NET runtimes. For example, say Minsk runs on .NET Core
3.1 then it wouldn't be able to reflect over .NET Framework binaries and thus
also be unable to compile code for it.

We could solve this by using using the (relatively new) [MetadataLoadContext]
API, which is an implementation of the reflection APIs over pure metadata,
meaning the underlying binaries aren't actually loaded. However, we also need to
*write* metadata which `MetadataLoadContext` doesn't help with.

Another option would be to use the `System.Reflection.Metadata` API, which is
what Roslyn is using to read & write metadata. However, this API is very low
level and doesn't provide an object model over .NET metadata. That's awesome for
production compilers because it means that there is only one object model
(namely the compiler's symbol API) but it's not very convenient for us.

Instead we'll be using [Mono.Cecil]. Cecil is used by Mono's C# compiler and
provides a very convenient way to read & write .NET metadata.

[MetadataLoadContext]: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.metadataloadcontext?view=dotnet-plat-ext-3.1
[Mono.Cecil]: https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/

### Resolving dependencies

In order to emit IL we need to think about the inputs to our compiler. There are basically three items:

1. **Sources**. The list of source files.
2. **References**. The list of .NET assemblies to compile against.
3. **Output path**. The path to the output file the compiler should produce.

The second item might feel like overkill as we don't allow users to reference
any assemblies or even instantiating types.

However, Minsk already has dependencies on functionality that we need to import:

* The .NET representation for our own types (`any` maps to `System.Object`,
  `int` maps to `System.Int32` and so on).
* Implementations for built-in functions (for example, `print()` will call
  `System.Console.WriteLine()`).

We could hard code those references but that's not very elegant. The way real
.NET compilers work is that they are passed a bunch of references and then they
resolve required types and members using some sort of scheme, usually by name
but sometimes also by shape. For example, C# will only search for
`System.Object` in the core library, which is the only assembly that itself
doesn't depend on any other assemblies.

For now, [we're just searching][resolve-types] all assemblies using the type
name.

### About IL

Technologies like the .NET runtime are often called *virtual machines* because
their instruction set (also called byte code) isn't targeting a physical CPU but
an imaginary one. It's the responsibility of the just-in-time (JIT) compiler to
produce the machine code. In some cases this is also done ahead-of-time (AOT),
for example, by using ready-to-run or an AOT compiler. Having this separation is
common in compiler pipelines because it allows to support multiple languages
(e.g. C#, VB, F#) and multiple CPU architectures (e.g. x86, x64, ARM32, ARM64)
without having to build all combinations. Adding a new CPU target only requires
adding a single compiler from IL to machine code as opposed to having to build
this for each language. It also simplifies letting different source languages
reference each other.

The .NET byte code is called [common intermediate language (CIL)][CIL], but most
people just call it IL.

[CIL]: https://en.wikipedia.org/wiki/Common_Intermediate_Language

IL doesn't use registers. Instead, it's a stack-based language where expressions
are *pushed* onto an evaluation stack. Operations then *pop* their arguments
from this imaginary stack and then push the result on the stack. Let's look at a
few examples.

A function like this:

```C#
static int Add(int x, int y)
{
    return x + y;
}
```

will look as follows in IL:

```
ldarg.0     // Push the value of x
ldarg.1     // Push the value of y
add         // Pops two values, adds them, and then pushes the result
ret         // Pops current value and returns it
```

Method invocations are analogous. This C# code:

```C#
var result = Add(2, 3);
```

looks as follows in IL:

```
ldc.i4.2                                    // Push the literal 2
ldc.i4.3                                    // Push the literal 3
call int32 hello.Program::Add(int32, int32) // Pops the values 2 and 3 and calls the static method
stloc.0                                     // Pops the return value and stores it in the first local
```

### Emitting Hello World

For now, [we're just emitting][emit-hello] the traditional "Hello World":

```C#
var objectType = knownTypes[TypeSymbol.Any];
var typeDefinition = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed, objectType);
assemblyDefinition.MainModule.Types.Add(typeDefinition);

var voidType = knownTypes[TypeSymbol.Void];
var mainMethod = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Private, voidType);
typeDefinition.Methods.Add(mainMethod);

var ilProcessor = mainMethod.Body.GetILProcessor();
ilProcessor.Emit(OpCodes.Ldstr, "Hello world from Minsk!");
ilProcessor.Emit(OpCodes.Call, consoleWriteLineReference);
ilProcessor.Emit(OpCodes.Ret);
```

### Extending MSBuild

In order to simplify the invocation of the compiler, most projects use some sort
of build orchestration tool, such as make or MSBuild. In the case of C#, the
project file uses the extension `.csproj`. For Minsk, we're going to use
`.msproj`.

Of course, building a project with that extension won't invoke our compiler --
how would MSBuild know how to do that? The answer is the `CoreCompile` target.
For the built-in languages (such as C#, VB, or F#), MSBuild choses the
appropriate definition based on the project extension. To fill it in, all we have to
do is [define this target][core-compile] in our project file:

```xml
  <Target Name="CoreCompile" DependsOnTargets="$(CoreCompileDependsOn)">
    <Exec Command="dotnet run --project &quot;$(MSBuildThisFileDirectory)\..\src\msc\msc.csproj&quot; --
                   @(Compile->'&quot;%(Identity)&quot;', ' ')
                   /o &quot;@(IntermediateAssembly)&quot;
                   @(ReferencePath->'/r &quot;%(Identity)&quot;', ' ')"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>
```

This looks complicated but all it does is this:

1. It uses `dotnet run` to build & run the Minsk compiler
2. It passes the source files to the compiler, which are provided as MSBuild `<Compile>` items
3. It specifies the output path (which is usually a location in `obj`)
4. It passes the list of references, which are provided as MBuild `<ReferencePath>` items

The nice thing is that (2), (3), and (4) are already computed for us by the basic .NET Core SDK:

* The source files are based on a wildcard inclusion. We told MSBuild via
  `DefaultLanguageSourceExtension` that our extension is `.ms` and MSBuild
  helpfully already scanned the project's folder and all subdirectories to find
  all the source files.
* The intermediate output path is provided by MSBuild and wil be cleaned up in
  case someone invokes `dotnet clean`.
* The references are based on several things, such as the `TargetFramework`
  property and project- and package references.

In order to keep the sample project files neat & tidy, we have extracted the
properties and targets into `Directory.Build.props` and
`Directory.Build.targets`, which are going to be imported automatically. We
could redistribute our compiler and build targets in a custom MSBuild SDK. The
only difference for the consumer would be the `SDK` attribute in the project
file.

### Launcher app

It feels intuitive to launch your console apps just like any other application.
But .NET Core normally doesn't produce native code. So how does this even work?

The answer is simple. A .NET Core console app is just a DLL. The build process
effectively copies a native exe from the SDK to your output folder, under the
name of your app. This native exe is responsibly for booting the .NET Core
runtime and deferring control to the entry point in your code.

You can, however, also run .NET Core console apps by using the `dotnet` tool:

```
$ dotnet exec hello.dll
```

The exec command is optional, so `dotnet hello.dll` will do the same thing. It's
worth pointing out that `dotnet exec` isn't part of the .NET Core SDK -- it's
part of the .NET Core runtime. In other words, it also exists on non-developer
machines that only have the runtime installed. This allows you to have
applications you can XCOPY between different operating systems.

The good news is that we don't have to worry about any of this. Since we're
plugged into MSBuild, we get all of this for free!

[resolve-types]: https://github.com/terrajobst/minsk/blob/1b6b9a2779ff6c159eeaf52883b22b9d10307821/src/Minsk/CodeAnalysis/Emit/Emitter.cs#L57-L60
[emit-hello]: https://github.com/terrajobst/minsk/blob/1b6b9a2779ff6c159eeaf52883b22b9d10307821/src/Minsk/CodeAnalysis/Emit/Emitter.cs#L131-L142
[core-compile]: https://github.com/terrajobst/minsk/blob/1b6b9a2779ff6c159eeaf52883b22b9d10307821/samples/Directory.Build.targets#L5-L8