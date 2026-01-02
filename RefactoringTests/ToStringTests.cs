// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Xunit;

using Verify = Microsoft.CodeAnalysis.Testing.CodeRefactoringVerifier<Webczat.RecordBooster.Refactorings.RefactoringProvider, Webczat.RecordBooster.Refactorings.CSharpToStringCodeRefactoringTest, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Webczat.RecordBooster.Refactorings;

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
    [InlineData("""
    public record Test
    {
    $$    public string x { get; }
    }
    """)]
    [InlineData("""
    public record Test
    {
    [| |]   public string x { get; }
    }
    """)]
    [InlineData("""
    public record Test($$int X, int Y, int Z);
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
    public record $$Test
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

        $$
    }
    """)]
    [InlineData("""
    public record Test
    {

        
    $$}
    """)]
    [InlineData("""
    public record Test
    {
    [|
    |]    
    }
    """)]
    [InlineData("""
    public record Test
    {

    [|    |]
    }
    """)]
    public Task ToStringRefactoring_GeneratesToString_CursorOnTypeAndNoMembers(string input)
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
    public record $$Test
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
    $$
        
        public int Prop { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {

        $$
        public int Prop { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {

    [|    |]
        public int Prop { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {
    [|
    |]    
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
    public Task ToStringRefactoring_GeneratesToString_CursorOnTypeAndOneMemberPresent(string input)
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

    [Theory]
    [InlineData("""
    $$public record Test
    {

        
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record $$Test
    {

        
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    $${

        
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {
    $$
        
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {

        $$
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {

    [|    |]
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {
    [|
    |]    
        public int Prop { get; }

        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {

        
        public int Prop { get; }
    $$
        public int Prop2 { get; }

    }
    """)]
    [InlineData("""
    public record Test
    {

        
        public int Prop { get; }

        public int Prop2 { get; }
    $$
    }
    """)]
    public Task ToStringRefactoring_GeneratesToString_CursorOnTypeAndTwoMembersPresent(string input)
    {
        var output = """
        using System.Text;

        public record Test
        {

            
            public int Prop { get; }

            public int Prop2 { get; }

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
    [InlineData("public string ToString(int x)")]
    [InlineData("public string ToString(ref int x)")]
    [InlineData("public string ToString(int x, int y)")]
    public Task ToStringRefactoring_GeneratesMethod_OtherToStringOverloadsPresent(string overload)
    {
        var input = $$"""
        using System;
        using System.Text;

        $$public record Test
        {
            {{overload}}
            {
                throw new NotImplementedException();
            }
        }
        """;

        var output = $$"""
        using System;
        using System.Text;

        public record Test
        {
            {{overload}}
            {
                throw new NotImplementedException();
            }

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

    [Fact]
    public Task ToStringRefactoring_GeneratesToStringInInnerType_RecordsNestedAndCursorOnNestedType()
    {
        var input = """
        public record Outer
        {
        
            $$public record Inner
            {
            }
        }
        """;

        var output = """
        using System.Text;

        public record Outer
        {
        
            $$public record Inner
            {
                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(nameof(Inner));
                    sb.Append(" { ");

                    if (PrintMembers(sb))
                    {
                        sb.Append(' ');
                    }

                    sb.Append('}');
                    return sb.ToString();
                }
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task ToStringRefactoring_GeneratesToStringInOuterType_RecordsNestedAndCursorBeforeNestedType()
    {
        var input = """
        public record Outer
        {
        $$
            public record Inner
            {
            }
        }
        """;

        var output = """
        using System.Text;

        public record Outer
        {
        
            public record Inner
            {
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(nameof(Outer));
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

    [Fact]
    public Task ToStringRefactoring_GeneratesToString_NoToStringAndRecordPositional()
    {
        var input = """
                $$public record Test(int Prop);
        """;

        var output = """
        using System.Text;

        public record Test(int Prop)
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

    [Fact]
    public Task ToStringRefactoring_GeneratesReadonlyToString_RecordStructWithNoMembers()
    {
        var input = """
        $$public record struct Test
        {
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
            public override readonly string ToString()
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

    [Fact]
    public Task ToStringRefactoring_GeneratesReadonlyToString_RecordStructWithReadonlyMembers()
    {
        var input = """
        $$public record struct Test
        {
        public int TestProperty { get; }
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
        public int TestProperty { get; }

            public override readonly string ToString()
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

    [Fact]
    public Task ToStringRefactoring_GeneratesNonReadonlyToString_RecordStructWithNonReadonlyMembers()
    {
        var input = """
        $$public record struct Test
        {
        public int TestProperty => 0;
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
        public int TestProperty => 0;

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

    [Fact]
    public Task ToStringRefactoring_GeneratesNonReadonlyToString_RecordStructWithNonReadonlyAndReadonlyMembers()
    {
        var input = """
        $$public record struct Test
        {
        public int TestProperty => 0;

        public int TestProperty2 { get; }
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
        public int TestProperty => 0;

        public int TestProperty2 { get; }

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
