// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

namespace Webczat.RecordBooster.Refactorings;

public class CSharpEqualsAndGetHashCodeCodeRefactoringTest : CSharpCodeRefactoringTest
{
    public CSharpEqualsAndGetHashCodeCodeRefactoringTest()
    {
        CodeActionEquivalenceKey = RefactoringProvider.EqualsAndGetHashCodeKey;
    }
}
