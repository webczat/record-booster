// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Xunit;

using Verify = Microsoft.CodeAnalysis.Testing.CodeRefactoringVerifier<Webczat.RecordBooster.RefactoringProvider, Webczat.RecordBooster.CSharpEqualsAndGetHashCodeCodeRefactoringTest, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Webczat.RecordBooster;

public class EqualsAndGetHashCodeTests
{
    [Theory]
    [InlineData("$$public class X { }")]
    [InlineData("$$public enum X { }")]
    [InlineData("$$public struct X { }")]
    [InlineData("$$public delegate void X();")]
    [InlineData("$$public interface X { }")]
    public Task EqualsAndGetHashCodeRefactoring_DoesNotAppear_NotRecord(string input) =>
    Verify.VerifyRefactoringAsync(input, input);

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_DoesNotAppear_EqualsAlreadyPresent()
    {
        var input = """
        #nullable enable
        using System;

        $$public record Test
        {
            public virtual bool Equals(Test? other) =>
                throw new NotImplementedException();}
        """;

        return Verify.VerifyRefactoringAsync(input, input);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_DoesNotAppear_GetHashCodeAlreadyPresent()
    {
        var input = """
        using System;

        $$public record Test
        {
            public override int GetHashCode() =>
                throw new NotImplementedException();}
        """;

        return Verify.VerifyRefactoringAsync(input, input);
    }

    [Theory]
    [InlineData("""
    public record Test
    {
        $$public int X;
    }
    """)]
    [InlineData("""
    public record Test
    {
    $$    public int X;
    }
    """)]
    [InlineData("""
    public record Test
    {
    [| |]   public int X;
    }
    """)]
    [InlineData("""
    public record Test
    {
        public void X()
        {
            $$System.Console.WriteLine("test");
        }
    }
    """)]
    [InlineData("""
    public record Test($$int I, int J, int K);
    """)]
    public Task EqualsAndGetHashCodeRefactoring_DoesNotAppear_CursorInMembers(string input) =>
    Verify.VerifyRefactoringAsync(input, input);
}
