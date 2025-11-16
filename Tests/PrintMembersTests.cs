// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Xunit;

using Verify = Microsoft.CodeAnalysis.Testing.CodeRefactoringVerifier<Webczat.RecordBooster.RefactoringProvider, Webczat.RecordBooster.CSharpPrintMembersCodeRefactoringTest, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Webczat.RecordBooster;

public class PrintMembersTests
{
    [Theory]
    [InlineData("$$public class X { }")]
    [InlineData("$$public enum X { }")]
    [InlineData("$$public struct X { }")]
    [InlineData("$$public delegate void X();")]
    [InlineData("$$public interface X { }")]
    public Task PrintMembersRefactoring_DoesNotAppear_NotRecord(string input) =>
    Verify.VerifyRefactoringAsync(input, input);

    [Fact]
    public Task PrintMembersRefactoring_DoesNotAppear_PrintMembersAlreadyPresent()
    {
        var input = """
        using System;
        using System.Text;

        [|public record Test|]
        {
        protected virtual bool PrintMembers(StringBuilder sb) =>
            throw new NotImplementedException();
        }
        """;
        return Verify.VerifyRefactoringAsync(input, input);
    }
}
