// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Webczat.RecordBooster.Utils;

public sealed class RoslynWorkspaceTestingContext
{
    public CodeRefactoringTester<T> CreateCodeRefactoringTester<T>()
    where T : CodeRefactoringProvider =>
        new(this);
}