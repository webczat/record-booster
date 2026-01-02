# Introduction
This repository contains useful roslyn plugins to work with records.

Current contents:
* Refactorings - to generate explicit versions of implicit record members.

# Refactorings
This project contains refactorings that explicitly generate record members that are normally implicit.
They are useful when one needs to replace any of the replaceable implicit members and augment them with custom behavior. In this case, these refactorings provide an useful starting point.

Behavior of explicit members is based on implicitly declared members as defined in the [Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/records) and [Record structs](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/record-structs) specifications, however, sometimes an alternative implementation was chosen, mostly for readability purposes.

The following methods can be generated:
* `Equals` and `GetHashCode`,
* `Deconstruct` (for positional records),
* `PrintMembers`,
* `ToString`.

# Development environment setup
The recommended development environment is (Visual Studio Code)[https://code.visualstudio.com/]) code editor with `devcontainers` extensions installed.

This repository contains a devcontainer configuration and using a devcontainer is recommended. If you don't plan to using a devcontainer, you can install recommended extensions from marketplace.

# Building
Building is straight forward. Generally you can just execute the following command in the root folder:
```cs
dotnet build
```
Note that building the project requires minimum .NET version 8.0.

To run tests, execute:
```cs
dotnet test
```
