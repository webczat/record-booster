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
    public void X()
    {
        $$Console.WriteLine("test");
    }
    }
    """)]
    [InlineData("""
    public record Test($$int I, int J, int K);
    """)]
    public Task PrintMembersRefactoring_DoesNotAppear_CursorInMembers(string input) =>
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
    [InlineData("""
        [|public record Test
        {

        }|]
    """)]
    public Task PrintMembersRefactoring_GeneratesMethodReturningFalse_NoMembers(string input)
    {
        var output = """
        using System.Text;

        public record Test
        {
            protected virtual bool PrintMembers(StringBuilder sb)
            {
                return false;
            }}
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
        $$public record Test
        {
            private int _field;
            private readonly string _anotherField;
            internal object InternalField;
            protected double Property { get; init; }
            public string AnotherProperty { set; }
            public static int StaticField;

            public static int StaticProperty { get; init; }
        }
        """)]
    [InlineData("""
        public record Test
        $${
            private int _field;
            private readonly string _anotherField;
            internal object InternalField;
            protected double Property { get; init; }
            public string AnotherProperty { set; }
            public static int StaticField;

            public static int StaticProperty { get; init; }
        }
        """)]
    [InlineData("""
        public record Test
        {
            private int _field;
            private readonly string _anotherField;
            internal object InternalField;
            protected double Property { get; init; }
            public string AnotherProperty { set; }
            public static int StaticField;
            $$
            public static int StaticProperty { get; init; }
        }
        """)]
    [InlineData("""
        public record Test
        {
            private int _field;
            private readonly string _anotherField;
            internal object InternalField;
            protected double Property { get; init; }
            public string AnotherProperty { set; }
            public static int StaticField;

            public static int StaticProperty { get; init; }
        $$}
        """)]
    [InlineData("""
        [|public record Test
        {
            private int _field;
            private readonly string _anotherField;
            internal object InternalField;
            protected double Property { get; init; }
            public string AnotherProperty { set; }
            public static int StaticField;

            public static int StaticProperty { get; init; }
        }|]
        """)]
    public Task PrintMembersRefactoring_GeneratesMethodReturningFalse_NoPrintableMembers(string input)
    {
        var output = """
        using System.Text;

        public record Test
        {
            private int _field;
            private readonly string _anotherField;
            internal object InternalField;
            protected double Property { get; init; }
            public string AnotherProperty { set; }
            public static int StaticField;

            public static int StaticProperty { get; init; }

            protected virtual bool PrintMembers(StringBuilder sb)
            {
                return false;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test
    {
        public string Field;

        public string Prop { get; init; }
    }
    """)]
    [InlineData("""
    public record Test
    $${
        public string Field;

        public string Prop { get; init; }
    }
    """)]
    [InlineData("""
    public record Test
    {
        public string Field;
        $$
        public string Prop { get; init; }
    }
    """)]
    [InlineData("""
    public record Test
    {
        public string Field;

        public string Prop { get; init; }
    $$}
    """)]
    [InlineData("""
    ["public record Test
    {
        public string Field;

        public string Prop { get; init; }
    }|]
    """)]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_PrintableMembersPresent(string input)
    {
        var output = """
        using System.Text;

        public record Test
        {
            public string Field;

            public string Prop { get; init; }

            protected virtual bool PrintMembers(StringBuilder sb)
            {
            System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append("Field = ");
                sb.Append(Field);
                sb.Append(", ");
                sb.Append("Prop = ");
                sb.Append(Prop);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_MixedPrintableAndNotPrintableMembers()
    {
        var input = """
        $$public record Test
        {
        private string PrivateProp { get; init; }
            public string Prop { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record Test
        {
        private string PrivateProp { get; init; }
            public string Prop { get; init; }

            protected virtual bool PrintMembers(StringBuilder sb)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append("Prop = );
                sb.Append(Prop);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_SinglePrintableMember()
    {
        var input = """
        $$public record Test
        {
            public string Prop { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record Test
        {
            public string Prop { get; init; }

            protected virtual bool PrintMembers(StringBuilder sb)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append("Prop = );
                sb.Append(Prop);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodUsingToString_ValueTypeMembers()
    {
        var input = """
        $$public record Test
        {
            public int Field;
            public int Prop { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record Test
        {
            public int Field;
            public int Prop { get; init; }

            protected virtual bool PrintMembers(StringBuilder sb)
            {
            System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append("Field = ");
                sb.Append(Field.ToString());
                sb.Append(", ");
                sb.Append("Prop = ");
                sb.Append(Prop.ToString());
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test(string Prop);
    """)]
    [InlineData("""
    $$public record Test(string Prop)
    {

    }
    """)]
    [InlineData("""
    public record Test(string Prop)
    $${

    }
    """)]
    [InlineData("""
    public record Test(string Prop)
    {
    $$
    }
    """)]
    [InlineData("""
    public record Test(string Prop)
    {

    $$}
    """)]
    [InlineData("""
    [|public record Test(string Prop)
    {

    }|]
    """)]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_PositionalRecordWithParams(string input)
    {
        var output = """
        using System.Text;

        public record Test(string Prop)
        {
            protected virtual bool PrintMembers(StringBuilder sb)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append("Prop = );
                sb.Append(Prop);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_PositionalRecordWithParamsAndExplicitProperty()
    {
        var input = """
        $$public record Test(string prop)
        {
            public string Prop => prop;
        }
        """;

        var output = """
        using System.Text;

        public record Test(string prop)
        {
            public string Prop => prop;

            protected virtual bool PrintMembers(StringBuilder sb)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append("Prop = );
                sb.Append(Prop);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesPrivateMethod_RecordSealed()
    {
        var input = """
        $$public sealed record Test
        {

        }
        """;

        var output = """
        using System.Text;

        public sealed record Test
        {
            private bool PrintMembers(StringBuilder sb)
            {
                return false;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCallingBase_InheritedAndNoMembers()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
        }
        """;

        var output = """
        using System.Text;

        public record Base
        {
        }

        public record Test : Base
        {
            protected override bool PrintMembers(StringBuilder sb) =>
                base.PrintMembers(sb);
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCallingBase_InheritedAndNoPrintableMembers()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
            public static string StaticProperty { get; init; }
            public static int StaticField;
            private readonly int _field;
            private int AnotherField;
            internal string InternalProperty { get; init; }
            public string UnreadableProperty { set; }
        }
        """;

        var output = """
        using System.Text;

        public record Base
        {
        }

        public record Test : Base
        {
            public static string StaticProperty { get; init; }
            public static int StaticField;
            private readonly int _field;
            private int AnotherField;
            internal string InternalProperty { get; init; }
            public string UnreadableProperty { set; }

            protected override bool PrintMembers(StringBuilder sb) =>
                base.PrintMembers(sb);
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_InheritedAndPrintableMembers()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
            public int Field;
            public string Property { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record Base
        {
        }

        public record Test : Base
        {
            public int Field;
            public string Property { get; init; }

            protected override bool PrintMembers(StringBuilder sb)
            {
            if (base.PrintMembers(sb)
            {
                sb.Append(", ");
            })

            sb.Append("Field = ");
            sb.Append(Field.ToString());
            sb.Append(", ");
            sb.Append("Property = ");
            sb.Append(Property);
            return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_InheritedAndPositionalWithSingleParam()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test(string Property) : Base
        {
        }
        """;

        var output = """
        using System.Text;

        public record Base
        {
        }

        public record Test(string Property) : Base
        {

            protected override bool PrintMembers(StringBuilder sb)
            {
            if (base.PrintMembers(sb)
            {
                sb.Append(", ");
            })

            sb.Append("Property = ");
            sb.Append(Property);
            return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesMethodCorrectly_InheritedAndPositionalWithSingleParamWithExplicitProperty()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test(string property) : Base
        {
            public string Property => property;
        }
        """;

        var output = """
        using System.Text;

        public record Base
        {
        }

        public record Test(string property) : Base
        {
            public string Property => property;

            protected override bool PrintMembers(StringBuilder sb)
            {
            if (base.PrintMembers(sb)
            {
                sb.Append(", ");
            })

            sb.Append("Property = ");
            sb.Append(Property);
            return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesReadonlyMethod_RecordStructWithNoMembers()
    {
        var input = """
        public record struct Test
        {
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
            private readonly bool PrintMembers(StringBuilder sb)
            {
                return false;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesReadonlyMethod_RecordStructWithReadonlyProperties()
    {
        var input = """
        public record struct Test
        {
            public string Property { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
            public string Property { get; init; }

            private readonly bool PrintMembers(StringBuilder sb)
            {
                sb.Append("Property = ");
                sb.Append(Property);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesReadonlyMethod_RecordStructWithReadonlyPropertiesAndNonReadonlyFields()
    {
        var input = """
        public record struct Test
        {
            public int Field;
            public string Property { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
            public int Field;
            public string Property { get; init; }

            private readonly bool PrintMembers(StringBuilder sb)
            {
                sb.Append("Field = ");
                sb.Append(Field);
                sb.Append(", ");
                sb.Append("Property = ");
                sb.Append(Property);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesNonReadonlyMethod_RecordStructWithNonReadonlyProperties()
    {
        var input = """
        public record struct Test
        {
            public string Property => "";
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
            public string Property => "";

            private bool PrintMembers(StringBuilder sb)
            {
                sb.Append("Property = ");
                sb.Append(Property);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task PrintMembersRefactoring_GeneratesNonReadonlyMethod_RecordStructWithMixedReadonlyAndNonReadonlyProperties()
    {
        var input = """
        public record struct Test
        {
            public string Property => "";
            public string Property2 { get; init; }
        }
        """;

        var output = """
        using System.Text;

        public record struct Test
        {
            public string Property => "";
            public string Property2 { get; init; }

            private bool PrintMembers(StringBuilder sb)
            {
                sb.Append("Property = ");
                sb.Append(Property);
                sb.Append(", ");
                sb.Append("Property2 = );
                sb.Append(Property2);
                return true;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }
}
