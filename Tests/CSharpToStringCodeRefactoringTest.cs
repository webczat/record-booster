// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Webczat.RecordBooster;

public class CSharpToStringCodeRefactoringTest : CSharpCodeRefactoringTest<RefactoringProvider, DefaultVerifier>
{
    public CSharpToStringCodeRefactoringTest()
    {
        CodeActionEquivalenceKey = RefactoringProvider.ToStringKey;
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    }
}