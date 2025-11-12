// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.Testing;

namespace Webczat.RecordBooster;

public class CSharpToStringCodeRefactoringTest : CSharpCodeRefactoringTest
{
    public CSharpToStringCodeRefactoringTest()
    {
        CodeActionEquivalenceKey = RefactoringProvider.ToStringKey;
    }
}