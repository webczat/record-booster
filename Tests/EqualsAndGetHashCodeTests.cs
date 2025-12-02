// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis.Testing;

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
    [|public record Test
    {

        
    }|]
    """)]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethods_CursorOnTypeAndNoMembers(string input)
    {
        var output = """
        using System;

        public record Test
        {
            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(EqualityContract);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test
    {

        
        private int _field;

    }
    """)]
    [InlineData("""
    public record Test
    $${

        
        private int _field;

    }
    """)]
    [InlineData("""
    public record Test
    {
    $$
        
        private int _field;

    }
    """)]
    [InlineData("""
    public record Test
    {

        $$
        private int _field;

    }
    """)]
    [InlineData("""
    public record Test
    {
    [|
        |]
        private int _field;

    }
    """)]
    [InlineData("""
    public record Test
    {

    [|    |]
        private int _field;

    }
    """)]
    [InlineData("""
    public record Test
    {

        
        private int _field;
    $$
    }
    """)]
    [InlineData("""
    public record Test
    {

        
        private int _field;

    $$}
    """)]
    [InlineData("""
    [|public record Test
    {

        
        private int _field;

    }|]
    """)]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethods_CursorOnTypeAndOneMember(string input)
    {
        var output = """
        using System;

        public record Test
        {

            
            private int _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("""
    $$public record Test
    {

        
        private int _field;

        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    $${

        
        private int _field;

        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    {
    $$
        
        private int _field;

        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    {

        $$
        private int _field;

        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    {
    [|
        |]
        private int _field;

        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    {

    [|    |]
        private int _field;

        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    {

        
        private int _field;
    $$
        private int _field2;

    }
    """)]
    [InlineData("""
    public record Test
    {

        
        private int _field;

        private int _field2;
    $$
    }
    """)]
    [InlineData("""
    public record Test
    {

        
        private int _field;

        private int _field2;

    $$}
    """)]
    [InlineData("""
    [|public record Test
    {

        
        private int _field;

        private int _field2;

    }|]
    """)]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethods_CursorOnTypeAndTwoMembers(string input)
    {
        var output = """
        using System;

        public record Test
        {

            
            private int _field;

            private int _field2;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field &&
                    _field2 == other._field2;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field,
                    _field2);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethodsInInnerType_NestedRecordAndCursorOnInnerType()
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
        using System;

        public record Outer
        {

            public record Inner
            {
                public virtual bool Equals(Inner? other)
                {
                    return other is not null &&
                        EqualityContract == other.EqualityContract;
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(EqualityContract);
                }
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethodsInOuterType_NestedRecordAndCursorBeforeInnerType()
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
        using System;

        public record Outer
        {

            public record Inner
            {
            }

            public virtual bool Equals(Outer? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(EqualityContract);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethodsIgnoringFields_AllFieldsStatic()
    {
        var input = """
        $$public record Test
        {
            public static int X;
        }
        """;

        var output = """
        using System;

        public record Test
        {
            public static int X;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(EqualityContract);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesHashCodeCombine_SevenFields()
    {
        var input = """
        $$public record Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
        }
        """;

        var output = """
        using System;

        public record Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field1 == other._field1 &&
                    _field2 == other._field2 &&
                    _field3 == other._field3 &&
                    _field4 == other._field4 &&
                    _field5 == other._field5 &&
                    _field6 == other._field6 &&
                    _field7 == other._field7;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field1,
                    _field2,
                    _field3,
                    _field4,
                    _field5,
                    _field6,
                    _field7);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesHashCodeAdd_MoreThanSevenFields()
    {
        var input = """
        $$public record Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;
        }
        """;

        var output = """
        using System;

        public record Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field1 == other._field1 &&
                    _field2 == other._field2 &&
                    _field3 == other._field3 &&
                    _field4 == other._field4 &&
                    _field5 == other._field5 &&
                    _field6 == other._field6 &&
                    _field7 == other._field7 &&
                    _field8 == other._field8;
            }

            public override int GetHashCode()
            {
                HashCode h = default;
                h.Add(EqualityContract);
                h.Add(_field1);
                h.Add(_field2);
                h.Add(_field3);
                h.Add(_field4);
                h.Add(_field5);
                h.Add(_field6);
                h.Add(_field7);
                h.Add(_field8);
                return h.ToHashCode();
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("byte")]
    [InlineData("sbyte")]
    [InlineData("ushort")]
    [InlineData("short")]
    [InlineData("uint")]
    [InlineData("int")]
    [InlineData("ulong")]
    [InlineData("long")]
    [InlineData("bool")]
    [InlineData("char")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("decimal")]
    [InlineData("string")]
    [InlineData("object")]
    [InlineData("nuint")]
    [InlineData("nint")]
    [InlineData("System.Type")]
    [InlineData("System.DateTime")]
    public Task EqualsAndGetHashCodeRefactoring_UsesEqualityOperator_BuiltinTypes(string typeName)
    {
        var input = $$"""
        $$public record Test
        {
            private {{typeName}} _field;
        }
        """;

        var output = $$"""
        using System;

        $$public record Test
        {
            private {{typeName}} _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesEqualityOperator_EnumTypes()
    {
        var input = """
        public enum TestEnum
        {
            None = 0,
        }

        $$public record Test
        {
            private TestEnum _field;
        }
        """;

        var output = """
        using System;

        public enum TestEnum
        {
            None = 0,
        }

        $$public record Test
        {
            private TestEnum _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesEqualityOperator_RecordTypes()
    {
        var input = """
        public record TestFieldType
        {
        }

        $$public record Test
        {
            private TestFieldType _field;
        }
        """;

        var output = """
        using System;

        public record TestFieldType
        {
        }

        $$public record Test
        {
            private TestFieldType _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Theory]
    [InlineData("string[]")]
    [InlineData("System.Text.StringBuilder")]
    public Task EqualsAndGetHashCodeRefactoring_UsesDefaultEqualityComparer_OtherTypes(string typeName)
    {
        var input = $$"""
        $$public record Test
        {
            private {{typeName}} _field;
        }
        """;

        var output = $$"""
        using System;
        using System.Collections.Generic;

        $$public record Test
        {
            private {{typeName}} _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    EqualityComparer<{{typeName}}>.Default.Equals(_field, other._field);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_ReturnsEqualityContractHashCode_NoHashCodeStructAndNoMembers()
    {
        var input = """
        $$public record Test
        {
        }
        """;

        var output = """
        public record Test
        {
            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return EqualityContract.GetHashCode();
            }
        }
        """;

        return CodeRefactoringVerifier<RefactoringProvider, CSharpEqualsAndGetHashCodeNetStandardCodeRefactoringTest, DefaultVerifier>.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesValueTupleHashCode_NoHashCodeStructAndMembersPresent()
    {
        var input = """
        $$public record Test
        {
            private int _field;
        }
        """;

        var output = """
        public record Test
        {
            private int _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return (
                    EqualityContract,
                    _field).GetHashCode();
            }
        }
        """;

        return CodeRefactoringVerifier<RefactoringProvider, CSharpEqualsAndGetHashCodeNetStandardCodeRefactoringTest, DefaultVerifier>.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_IgnoresProperties_PropertiesNotAuto()
    {
        var input = """
        $$public record Test
        {
            public int Prop => 0;
        }
        """;

        var output = """
        using System;

        public record Test
        {
            public int Prop => 0;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(EqualityContract);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesProperties_PropertiesAuto()
    {
        var input = """
        $$public record Test
        {
            public int Prop { get; init; }
        }
        """;

        var output = """
        using System;

        public record Test
        {
            public int Prop { get; init; }

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    Prop == other.Prop;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    Prop);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesProperties_RecordPositional()
    {
        var input = """
        $$public record Test(int Prop);
        """;

        var output = """
        using System;

        public record Test(int Prop)
        {
            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    Prop == other.Prop;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    Prop);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesPropertiesAndFields_MixedFieldsAndProperties()
    {
        var input = """
        $$public record Test
        {
            private int _field;
            public int Prop { get; init; }
        }
        """;

        var output = """
        using System;

        public record Test
        {
            private int _field;
            public int Prop { get; init; }

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    _field == other._field &&
                    Prop == other.Prop;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field,
                    Prop);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesNonVirtualEquals_RecordSealed()
    {
        var input = """
        $$public sealed record Test
        {
        }
        """;

        var output = """
        using System;

        public sealed record Test
        {
            public bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(EqualityContract);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesNoOpMethods_RecordStructWithNoMembers()
    {
        var input = """
        $$public record struct Test
        {
        }
        """;

        var output = """
        public record struct Test
        {
            public readonly bool Equals(Test other)
            {
                return true;
            }

            public override readonly int GetHashCode()
            {
                return 0;
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethods_RecordStructWithSingleMember()
    {
        var input = """
        $$public record struct Test
        {
            private int _field;
        }
        """;

        var output = """
        using System;

        public record struct Test
        {
            private int _field;

            public readonly bool Equals(Test other)
            {
                return _field == other._field;
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(_field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_GeneratesMethods_RecordStructWithTwoMembers()
    {
        var input = """
        $$public record struct Test
        {
            private int _field;
            private int _field2;
        }
        """;

        var output = """
        using System;

        public record struct Test
        {
            private int _field;
            private int _field2;

            public readonly bool Equals(Test other)
            {
                return _field == other._field &&
                    _field2 == other._field2;
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(
                    _field,
                    _field2);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesHashCodeCombine_RecordStructWithEightMembers()
    {
        var input = """
        $$public record struct Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;
        }
        """;

        var output = """
        using System;

        public record struct Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;

            public readonly bool Equals(Test other)
            {
                return _field1 == other._field1 &&
                    _field2 == other._field2 &&
                    _field3 == other._field3 &&
                    _field4 == other._field4 &&
                    _field5 == other._field5 &&
                    _field6 == other._field6 &&
                    _field7 == other._field7 &&
                    _field8 == other._field8;
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(
                    _field1,
                    _field2,
                    _field3,
                    _field4,
                    _field5,
                    _field6,
                    _field7,
                    _field8);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesHashCodeAdd_RecordStructWithMoreThanEightMembers()
    {
        var input = """
        $$public record struct Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;
            private int _field9;
        }
        """;

        var output = """
        using System;

        public record struct Test
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;
            private int _field9;

            public readonly bool Equals(Test other)
            {
                return _field1 == other._field1 &&
                    _field2 == other._field2 &&
                    _field3 == other._field3 &&
                    _field4 == other._field4 &&
                    _field5 == other._field5 &&
                    _field6 == other._field6 &&
                    _field7 == other._field7 &&
                    _field8 == other._field8 &&
                    _field9 == other._field9;
            }

            public override readonly int GetHashCode()
            {
                HashCode h = default;
                h.Add(_field1);
                h.Add(_field2);
                h.Add(_field3);
                h.Add(_field4);
                h.Add(_field5);
                h.Add(_field6);
                h.Add(_field7);
                h.Add(_field8);
                h.Add(_field9);
                return h.ToHashCode();
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_ReturnsMemberHashCode_NoHashCodeStructAndRecordStructWithSingleMember()
    {
        var input = """
        $$public record struct Test
        {
            private int _field;
        }
        """;

        var output = """
        public record struct Test
        {
            private int _field;

            public readonly bool Equals(Test other)
            {
                return _field == other._field;
            }

            public override readonly int GetHashCode()
            {
                return _field.GetHashCode();
            }
        }
        """;

        return CodeRefactoringVerifier<RefactoringProvider, CSharpEqualsAndGetHashCodeNetStandardCodeRefactoringTest, DefaultVerifier>.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesValueTupleHashCode_NoHashCodeStructAndRecordStructWithMultipleMembersPresent()
    {
        var input = """
        $$public record struct Test
        {
            private int _field;
            private int _field2;
        }
        """;

        var output = """
        public record struct Test
        {
            private int _field;
            private int _field2;

            public readonly bool Equals(Test other)
            {
                return _field == other._field &&
                    _field2 == other._field2;
            }

            public override readonly int GetHashCode()
            {
                return (
                    _field,
                    _field2).GetHashCode();
            }
        }
        """;

        return CodeRefactoringVerifier<RefactoringProvider, CSharpEqualsAndGetHashCodeNetStandardCodeRefactoringTest, DefaultVerifier>.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_DelegatesToBase_InheritedAndNoMembers()
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
        using System;

        public record Base
        {
        }

        public record Test : Base
        {
            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    base.Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode());
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_DelegatesToBase_InheritedAndSingleMember()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
            private int _field;
        }
        """;

        var output = """
        using System;

        public record Base
        {
        }

        public record Test : Base
        {
            private int _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    base.Equals(other) &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_ReturnsBaseHashCode_NoHashCodeStructAndInheritedWithNoMembers()
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
        public record Base
        {
        }

        public record Test : Base
        {
            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    base.Equals(other);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
        """;

        return CodeRefactoringVerifier<RefactoringProvider, CSharpEqualsAndGetHashCodeNetStandardCodeRefactoringTest, DefaultVerifier>.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesValueTupleHashCode_NoHashCodeStructAndInheritedWithMembersPresent()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
            private int _field;
        }
        """;

        var output = """
        public record Base
        {
        }

        public record Test : Base
        {
            private int _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    base.Equals(other) &&
                    _field == other._field;
            }

            public override int GetHashCode()
            {
                return (
                    base.GetHashCode(),
                    _field).GetHashCode();
            }
        }
        """;

        return CodeRefactoringVerifier<RefactoringProvider, CSharpEqualsAndGetHashCodeNetStandardCodeRefactoringTest, DefaultVerifier>.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesHashCodeCombine_InheritedWithSevenFields()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
        }
        """;

        var output = """
        using System;

        public record Base
        {
        }

        public record Test : Base
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    base.Equals(other) &&
                    _field1 == other._field1 &&
                    _field2 == other._field2 &&
                    _field3 == other._field3 &&
                    _field4 == other._field4 &&
                    _field5 == other._field5 &&
                    _field6 == other._field6 &&
                    _field7 == other._field7;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    base.GetHashCode(),
                    _field1,
                    _field2,
                    _field3,
                    _field4,
                    _field5,
                    _field6,
                    _field7);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_UsesHashCodeAdd_InheritedAndMoreThanSevenFields()
    {
        var input = """
        public record Base
        {
        }

        $$public record Test : Base
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;
        }
        """;

        var output = """
        using System;

        public record Base
        {
        }

        public record Test : Base
        {
            private int _field1;
            private int _field2;
            private int _field3;
            private int _field4;
            private int _field5;
            private int _field6;
            private int _field7;
            private int _field8;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    base.Equals(other) &&
                    _field1 == other._field1 &&
                    _field2 == other._field2 &&
                    _field3 == other._field3 &&
                    _field4 == other._field4 &&
                    _field5 == other._field5 &&
                    _field6 == other._field6 &&
                    _field7 == other._field7 &&
                    _field8 == other._field8;
            }

            public override int GetHashCode()
            {
                HashCode h = default;
                h.Add(base.GetHashCode());
                h.Add(_field1);
                h.Add(_field2);
                h.Add(_field3);
                h.Add(_field4);
                h.Add(_field5);
                h.Add(_field6);
                h.Add(_field7);
                h.Add(_field8);
                return h.ToHashCode();
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_RespectsNullableContext_NullableDisabled()
    {
        var input = """
        #nullable disable

        $$public record Test
        {
        }
        """;

        var output = """
        #nullable disable

        using System;

        public record Test
        {
            public virtual bool Equals(Test other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(EqualityContract);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }

    [Fact]
    public Task EqualsAndGetHashCodeRefactoring_DoesNotAddUsings_UsingsPresent()
    {
        var input = """
        using System;
        using System.Collections.Generic;

        $$public record Test
        {
            private string[] _field;
        }
        """;

        var output = """
        using System;
        using System.Collections.Generic;

        public record Test
        {
            private string[] _field;

            public virtual bool Equals(Test? other)
            {
                return other is not null &&
                    EqualityContract == other.EqualityContract &&
                    EqualityComparer<string[]>.Default.Equals(_field, other._field);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    EqualityContract,
                    _field);
            }
        }
        """;

        return Verify.VerifyRefactoringAsync(input, output);
    }
}
