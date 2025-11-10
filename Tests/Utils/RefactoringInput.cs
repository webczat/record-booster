// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Webczat.RecordBooster.Utils;

public sealed class RefactoringInput<T>
where T : CodeRefactoringProvider
{
    private readonly RoslynWorkspaceTestingContext _context;

    internal RefactoringInput(RoslynWorkspaceTestingContext context, string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _context = context;
    }

    public void DoesNotInclude(string equivalenceKey)
    {
        throw new NotImplementedException();
    }
}