// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Webczat.RecordBooster;

public class CSharpCodeRefactoringTest : CSharpCodeRefactoringTest<RefactoringProvider, DefaultVerifier>
{
    public const string EditorConfig = """
    root = true

    [*]
    charset = utf-8
    end_of_line = lf
    indent_size = 4
    indent_style = space
    insert_final_newline = true
    """;

    public CSharpCodeRefactoringTest()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EditorConfig));
    }
}