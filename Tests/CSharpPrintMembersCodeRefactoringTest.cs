// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Webczat.RecordBooster;

public class CSharpPrintMembersCodeRefactoringTest : CSharpCodeRefactoringTest<RefactoringProvider, DefaultVerifier>
{
    public CSharpPrintMembersCodeRefactoringTest()
    {
        CodeActionEquivalenceKey = RefactoringProvider.PrintMembersKey;
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    }
}