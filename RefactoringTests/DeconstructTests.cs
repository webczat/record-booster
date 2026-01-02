// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Xunit;

using Verify = Microsoft.CodeAnalysis.Testing.CodeRefactoringVerifier<Webczat.RecordBooster.Refactorings.RefactoringProvider, Webczat.RecordBooster.Refactorings.CSharpDeconstructCodeRefactoringTest, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Webczat.RecordBooster.Refactorings;

public class DeconstructTests
{
    [Theory]
    [InlineData("$$public class X { }")]
    [InlineData("$$public enum X { }")]
    [InlineData("$$public struct X { }")]
    [InlineData("$$public delegate void X();")]
    [InlineData("$$public interface X { }")]
    [InlineData("$$public record X { }")]
    [InlineData("$$public record struct X { }")]
    public async Task DeconstructRefactoring_DoesNotAppear_NotPositionalRecord(string input) =>
    await Verify.VerifyRefactoringAsync(input, input);

    [Fact]
    public async Task DeconstructRefactoring_DoesNotAppear_DeconstructPresent()
    {
        var input = """
        $$public record Test(int Property)
        {
            public void Deconstruct(out int Property)
            {
                Property = this.Property;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, input);
    }

    [Theory]
    [InlineData("""
    public record Test(int J)
    {
        $$public int X;
    }
    """)]
    [InlineData("""
    public record Test(int J)
    {
    $$    public int X;
    }
    """)]
    [InlineData("""
    public record Test(int J)
    {
    [| |]   public int X;
    }
    """)]
    [InlineData("""
    public record Test(int J)
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
    public async Task DeconstructRefactoring_DoesNotAppear_CursorInMembers(string input) =>
    await Verify.VerifyRefactoringAsync(input, input);

    [Theory]
    [InlineData("""
    $$public record Test(int Prop)
    {

        
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    $${

        
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {
    $$
        
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        $$
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        
    $$}
    """)]
    [InlineData("""
    public record Test(int Prop)
    {
    [|
        |]
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

    [|    |]
    }
    """)]
    [InlineData("""
    [|public record Test(int Prop)
    {

        
    }|]
    """)]
    public async Task DeconstructRefactoring_GeneratesMethod_CursorOnTypeAndNoMembers(string input)
    {
        var output = """
        public record Test(int Prop)
        {
            public void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test(int Prop)
    {

        
    private int _field;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    $${

        
    private int _field;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {
    $$
        
    private int _field;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        $$
    private int _field;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        
    private int _field;
    $$
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        
    private int _field;

    $$}
    """)]
    [InlineData("""
    public record Test(int Prop)
    {
    [|
        |]
    private int _field;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

    [|    |]
    private int _field;

    }
    """)]
    [InlineData("""
    [|public record Test(int Prop)
    {

        
    private int _field;

    }|]
    """)]
    public async Task DeconstructRefactoring_GeneratesMethod_CursorOnTypeAndSingleMember(string input)
    {
        var output = """
        public record Test(int Prop)
        {

            
        private int _field;

            public void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test(int Prop)
    {

        
    private int _field;

    private int _field2;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    $${

        
    private int _field;

    private int _field2;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {
    $$
        
    private int _field;

    private int _field2;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        $$
    private int _field;

    private int _field2;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        
    private int _field;
    $$
    private int _field2;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        
    private int _field;

    private int _field2;
    $$
    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

        
    private int _field;

    private int _field2;

    $$}
    """)]
    [InlineData("""
    public record Test(int Prop)
    {
    [|
        |]
    private int _field;

    private int _field2;

    }
    """)]
    [InlineData("""
    public record Test(int Prop)
    {

    [|    |]
    private int _field;

    private int _field2;

    }
    """)]
    [InlineData("""
    [|public record Test(int Prop)
    {

        
    private int _field;

    private int _field2;

    }|]
    """)]
    public async Task DeconstructRefactoring_GeneratesMethod_CursorOnTypeAndTwoMembers(string input)
    {
        var output = """
        public record Test(int Prop)
        {

            
        private int _field;

        private int _field2;

            public void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("public void Deconstruct(out int Prop1)")]
    [InlineData("public void Deconstruct()")]
    [InlineData("public void Deconstruct(int Prop1, out int Prop2)")]
    [InlineData("public void Deconstruct(out string Prop1, out int Prop2)")]
    [InlineData("public void Deconstruct(out int Prop1, out int Prop2, out int Prop3)")]
    public async Task DeconstructRefactoring_GeneratesMethod_OtherDeconstructOverloadsPresent(string overload)
    {
        var input = $$"""
        using System;

        $$public record Test(int Prop1, int Prop2)
        {
            {{overload}}
            {
                throw new NotImplementedException();
            }
        }
        """;

        var output = $$"""
        using System;

        public record Test(int Prop1, int Prop2)
        {
            {{overload}}
            {
                throw new NotImplementedException();
            }

            public void Deconstruct(out int Prop1, out int Prop2)
            {
                Prop1 = this.Prop1;
                Prop2 = this.Prop2;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesMethodInOuter_NestedRecordsAndCursorBeforeInner()
    {
        var input = """
        public record Outer(int Prop)
        {

        $$
            public record Inner(int Prop)
            {
            }
        }
        """;

        var output = """
        public record Outer(int Prop)
        {


            public record Inner(int Prop)
            {
            }

            public void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesMethodInInner_NestedRecordsAndCursorOnInner()
    {
        var input = """
        public record Outer(int Prop)
        {


            $$public record Inner(int Prop)
            {
            }
        }
        """;

        var output = """
        public record Outer(int Prop)
        {


            public record Inner(int Prop)
            {
                public void Deconstruct(out int Prop)
                {
                    Prop = this.Prop;
                }
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesMethodWithParamsFromPrimaryConstructor_MultipleParameters()
    {
        var input = "$$public record Test(int Prop1, int Prop2, int Prop3);";

        var output = """
        public record Test(int Prop1, int Prop2, int Prop3)
        {
            public void Deconstruct(out int Prop1, out int Prop2, out int Prop3)
            {
                Prop1 = this.Prop1;
                Prop2 = this.Prop2;
                Prop3 = this.Prop3;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesMethod_ParamWithExplicitlyDeclaredProperty()
    {
        var input = """
        $$public record Test(int Prop)
        {
            public int Prop { get; init; } = Prop;
        }
        """;

        var output = """
        public record Test(int Prop)
        {
            public int Prop { get; init; } = Prop;

            public void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesMethod_ParamWithExplicitlyDeclaredField()
    {
        var input = """
        $$public record Test(int Prop)
        {
            public int Prop = Prop;
        }
        """;

        var output = """
        public record Test(int Prop)
        {
            public int Prop = Prop;

            public void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesMethodWithNullableParam_NullablePrimaryConstructorParam()
    {
        var input = """
        $$public record Test(string? Prop)
        {
        }
        """;

        var output = """
        public record Test(string? Prop)
        {
            public void Deconstruct(out string? Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesReadonlyMethod_RecordStructWithReadonlyPositionalProps()
    {
        var input = """
        $$public record struct Test(int Prop)
        {
            public int Prop { get; init; } = Prop;
        }
        """;

        var output = """
        public record struct Test(int Prop)
        {
            public int Prop { get; init; } = Prop;

            public readonly void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesReadonlyMethod_RecordStructWithReadonlyPositionalPropsAndNonReadonlyOtherMembers()
    {
        var input = """
        $$public record struct Test(int Prop)
        {
            public int Prop2 => 0;
        }
        """;

        var output = """
        public record struct Test(int Prop)
        {
            public int Prop2 => 0;

            public readonly void Deconstruct(out int Prop)
            {
                Prop = this.Prop;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public async Task DeconstructRefactoring_GeneratesNonReadonlyMethod_RecordStructWithNonReadonlyAndReadonlyPositionalProps()
    {
        var input = """
        $$public record struct Test(int Prop, int Prop2)
        {
            public int Prop => Prop;
            public int Prop2 { get; init; } = Prop2;
        }
        """;

        var output = """
        public record struct Test(int Prop, int Prop2)
        {
            public int Prop => Prop;
            public int Prop2 { get; init; } = Prop2;

            public void Deconstruct(out int Prop, out int Prop2)
            {
                Prop = this.Prop;
                Prop2 = this.Prop2;
            }
        }
        """;

        await Verify.VerifyRefactoringAsync(input, output);
    }
}
