// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Xunit;

using Verify = Microsoft.CodeAnalysis.Testing.CodeRefactoringVerifier<Webczat.RecordBooster.RefactoringProvider, Webczat.RecordBooster.CSharpCodeRefactoringTest, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Webczat.RecordBooster;

public class ToStringTests
{
    [Theory]

    // This test will pass.
    [InlineData("""
    $$public record Test(int Prop)
    {
    }
    """)]

    // And this won't.
    [InlineData("""
    $$public record Test(int Prop);
    """)]
    public Task ToStringRefactoring_GeneratesToString_NoToStringAndRecordPositional(string input)
    {
        var output = """
        using System.Text;

        public record Test(int Prop)
        {
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                return sb.ToString();
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }
}