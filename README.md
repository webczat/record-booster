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

## Usage
The refactorings are distributed as a nuget package called `Webczat.RecordBooster.Refactorings`. You can install them, for example, using dotnet CLI in the following way:
```
dotnet package add Webczat.RecordBooster.Refactorings
```

New refactorings should appear after installation. To see them, you can invoke the quick fix menu when standing at the record declaration. However, they won't appear when standing on members inside of a record.

# Development environment setup
The recommended development environment is (Visual Studio Code)[https://code.visualstudio.com/]) code editor with `devcontainers` extensions installed.

This repository contains a devcontainer configuration and using a devcontainer is recommended. If you don't plan to using a devcontainer, you can install recommended extensions from marketplace.

# Building
First, you need to clone a repository like so:
```
git clone https://github.com/webczat/record-booster.git
```

Then, building is straight forward. Generally you can just execute the following command in the root repo directory:
```
dotnet build
```
Note that building the project requires minimum .NET version 8.0.

# Testing in development
The project has a set of automated test cases that verify the correct behavior of plugins. To run them, execute the following command from the repository root:
```
dotnet test
```
Note that prior building using `dotnet build` is not necessary.

Additionally, if you would like to test the plugin behavior in a live environment directly from sources, there is a special empty `playground` project. The project references all plugins, so any file created inside can use them directly. It's useful when manually testing code changes before shipping them.

# Contributing
When you would like to create a code change, perform the following:
1. Make sure there's an issue related to the change in question or create one.
2. When ready to make a change, fork the repository.
3. Create a branch for the change.
4. Modify the code appropriately. Remember that each change must come with appropriate tests or changes in existing tests. It's necessary to get automatic change verification, regression testing and, in case of fixing a bug, to show that the bug has indeed been fixed. Contributions without appropriate tests are unlikely to be accepted.
5. Push code and submit a pull request. The pull request title or description needs to contain the issue number it's associated with.
6. The pull request will be reviewed. If everything is correct, it will be approved and merged. Merging also requires that building and automated tests pass.
