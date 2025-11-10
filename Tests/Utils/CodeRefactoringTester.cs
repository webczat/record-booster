// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Webczat.RecordBooster.Utils;

public sealed class CodeRefactoringTester<T>
where T : CodeRefactoringProvider
{
    private readonly RoslynWorkspaceTestingContext _context;

    internal CodeRefactoringTester(RoslynWorkspaceTestingContext context)
    {
        _context = context;
    }

    public RefactoringInput<T> ForSource(string input) =>
        new(_context, input);
}