// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.Testing;

using Xunit;

using Verify = Microsoft.CodeAnalysis.Testing.CodeRefactoringVerifier<Webczat.RecordBooster.RefactoringProvider, Webczat.RecordBooster.CSharpToStringCodeRefactoringTest, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Webczat.RecordBooster;

public class ToStringTests
{
    [Theory]
    [InlineData("$$public class X { }")]
    [InlineData("$$public enum X { }")]
    [InlineData("$$public struct X { }")]
    [InlineData("$$public delegate void X();")]
    [InlineData("$$public interface X { }")]
    public Task ToStringRefactoring_DoesNotAppear_NotRecord(string input) =>
    Verify.VerifyRefactoringAsync(input, input);

    [Fact]
    public Task ToStringRefactoring_DoesNotAppear_ToStringAlreadyPresent()
    {
        var input = """
        using System;

        [|public record Test|]
        {
        public override string ToString() =>
            throw new NotImplementedException();
        }
        """;
        return Verify.VerifyRefactoringAsync(input, input);
    }

    [Theory]
    [InlineData("""
    public record Test
    {
        public void $$x()
        {
        }
    }
    """)]
    [InlineData("""
    using System;

    public record Test
    {
        public void x()
        {
            Console.$$WriteLine("hello");
        }
    }
    """)]
    [InlineData("""
    public record Test
    {
        public string $$x { get; }
    }
    """)]
    public Task ToStringRefactoring_DoesNotAppear_CursorInMembers(string input) =>
        Verify.VerifyRefactoringAsync(input, input);

    [Theory]
    [InlineData("""
    $$public record Test
    {

    }
    """)]
    [InlineData("""
    public record Test
    $${

    }
    """)]
    [InlineData("""
    public record Test
    {
    $$
    }
    """)]
    [InlineData("""
    public record Test
    {

    $$}
    """)]
    public Task ToStringRefactoring_GeneratesToString_NoToStringAndNoMembers(string input)
    {
        var output = """
        using System.Text;

        public record Test
        {
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(nameof(Test));
                sb.Append(" { ");

                if (PrintMembers(sb))
                {
                    sb.Append(' ');
                }

                sb.Append('}');
                return sb.ToString();
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test
    {
        public int Prop { get; }

    }
    """)]
    [InlineData("""
    public record Test
    $${
        public int Prop { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {
        public int Prop { get; }
        $$
    }
    """)]
    [InlineData("""
    public record Test
    {
        public int Prop { get; }

    $$}
    """)]
    public Task ToStringRefactoring_GeneratesToString_NoToStringAndMembersPresent(string input)
    {
        var output = """
        using System.Text;

        public record Test
        {
            public int Prop { get; }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(nameof(Test));
                sb.Append(" { ");

                if (PrintMembers(sb))
                {
                    sb.Append(' ');
                }

                sb.Append('}');
                return sb.ToString();
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }
}