// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

namespace Webczat.RecordBooster;

public class CSharpDeconstructCodeRefactoringTest : CSharpCodeRefactoringTest
{
    public CSharpDeconstructCodeRefactoringTest()
    {
        CodeActionEquivalenceKey = RefactoringProvider.DeconstructKey;
    }
}
