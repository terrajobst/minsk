# Minsk

[![Build Status](https://terrajobst.visualstudio.com/Minsk/_apis/build/status/terrajobst.minsk?branchName=master)](https://terrajobst.visualstudio.com/Minsk/_build/latest?definitionId=13)

> Have you considered Minsk? -- Worf, naming things.

This repo contains **Minsk**, a handwritten compiler in C#. It illustrates basic
concepts of compiler construction and how one can tool the language inside of an
IDE by exposing APIs for parsing and type checking.

This compiler uses many of the concepts that you can find in the Microsoft
C# and Visual Basic compilers, code named [Roslyn].

[Roslyn]: https://github.com/dotnet/roslyn

## Live coding

This code base was written live during streaming. You can watch the recordings
on [YouTube] or browse the [episode PRs][episodes].

[YouTube]: https://www.youtube.com/playlist?list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y
[episodes]: https://github.com/terrajobst/minsk/pulls?q=is%3Apr+is%3Aclosed+label%3Aepisode+sort%3Acreated-asc