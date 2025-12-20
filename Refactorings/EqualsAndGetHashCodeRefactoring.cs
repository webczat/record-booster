// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Webczat.RecordBooster.Refactorings;

public sealed class EqualsAndGetHashCodeRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel) :
CodeRefactoring(context, syntaxRoot, semanticModel)
{
    public const string EqualsMethod = "Equals";
    public const string GetHashCodeMethod = "GetHashCode";

    private static readonly SpecialType[] BuiltinTypes = [
    SpecialType.System_Object,
        SpecialType.System_String,
        SpecialType.System_Char,
        SpecialType.System_Boolean,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Byte,
        SpecialType.System_SByte,
        SpecialType.System_IntPtr,
        SpecialType.System_UIntPtr,
    ];

    public override string Key => "EqualsAndGetHashCode";

    public override string Title => "Generate default record \"Equals\" and \"GetHashCode\"";

    protected override bool IsApplicable(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol)
    {
        bool hasEquals = originalRecordSymbol.GetMembers(EqualsMethod)
            .Any(m => m is IMethodSymbol { Arity: 0, IsImplicitlyDeclared: false, Parameters: [{ Type: var type, RefKind: RefKind.None }] } &&
                SymbolEqualityComparer.Default.Equals(type, originalRecordSymbol));
        bool hasGetHashCode = originalRecordSymbol.GetMembers(GetHashCodeMethod)
            .Any(m => m is IMethodSymbol { Arity: 0, IsImplicitlyDeclared: false, Parameters: [] });

        return !hasEquals && !hasGetHashCode;
    }

    protected async override Task<Document> Execute(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default)
    {
        var document = Context.Document;
        var generator = SyntaxGenerator.GetGenerator(document);
        var inherited = originalRecordSymbol.BaseType?.IsRecord ?? false;

        // We try to compare actual object data, so take all instance fields irrespective of accessibility, but not properties.
        // Then because we can't access auto props directly via fields, take respective properties in their place.
        var comparableMembers = originalRecordSymbol.GetMembers()
            .Where(m => m is IFieldSymbol { IsStatic: false })
            .Select(m => m is IFieldSymbol { AssociatedSymbol: not null } f ? f.AssociatedSymbol : m)
            .ToList();

        // In case it's a reference type not inheriting from anything, add EqualityContract first.
        if (!originalRecordSymbol.IsValueType && !inherited)
        {
            comparableMembers.Insert(0, originalRecordSymbol.GetMembers("EqualityContract").Single(m => m is IPropertySymbol));
        }

        // Construct the equality expression based on members.
        ExpressionSyntax notNull = SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName("other"),
                SyntaxFactory.UnaryPattern(SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
        SyntaxNode? equalityExpression = originalRecordSymbol.IsValueType ? null : notNull;

        if (inherited && equalityExpression is not null)
        {
            equalityExpression = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                (ExpressionSyntax)equalityExpression,
                SyntaxFactory.Token(SyntaxKind.AmpersandAmpersandToken).WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")),
                (ExpressionSyntax)generator.InvocationExpression(generator.MemberAccessExpression(generator.BaseExpression(), "Equals"), [generator.IdentifierName("other")]).WithLeadingTrivia(SyntaxFactory.Whitespace("\t\t")));
        }

        bool usesEC = false;
        foreach (var f in comparableMembers)
        {
            var type = (f as IFieldSymbol)?.Type ?? ((IPropertySymbol)f).Type;
            SyntaxNode expression;
            if (BuiltinTypes.Contains(type.SpecialType) || type.TypeKind is TypeKind.Enum | type.GetMembers(WellKnownMemberNames.EqualityOperatorName)
            .Any(m => m is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.BuiltinOperator, Parameters: [{ Type: var t1 }, { Type: var t2 }] } &&
                SymbolEqualityComparer.Default.Equals(t1, t2) &&
                SymbolEqualityComparer.Default.Equals(t1, type)))
            {
                expression = generator.ValueEqualsExpression(generator.IdentifierName(f.Name), generator.MemberAccessExpression(generator.IdentifierName("other"), f.Name));
            }
            else
            {
                usesEC = true;
                expression = generator.InvocationExpression(generator.MemberAccessExpression(generator.MemberAccessExpression(generator.MemberAccessExpression(generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("System"), "Collections"), "Generic"), generator.GenericName("EqualityComparer", [generator.TypeExpression(type, false)])), "Default"), "Equals"), [generator.IdentifierName(f.Name), generator.MemberAccessExpression(generator.IdentifierName("other"), f.Name)]);
            }

            equalityExpression = equalityExpression == null ? expression : SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                (ExpressionSyntax)equalityExpression,
                SyntaxFactory.Token(SyntaxKind.AmpersandAmpersandToken).WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")),
                (ExpressionSyntax)expression.WithLeadingTrivia(SyntaxFactory.Whitespace("\t\t")));
        }

        equalityExpression ??= generator.TrueLiteralExpression();

        var hasHashCode = SemanticModel.Compilation.GetTypeByMetadataName("System.HashCode") is not null;

        SyntaxList<SyntaxNode> hashCodeStatements;
        if (comparableMembers.Count == 0 && !inherited)
        {
            hashCodeStatements = new(generator.ReturnStatement(generator.LiteralExpression(0)));
        }
        else if (hasHashCode && ((!inherited && comparableMembers.Count > 8) || (inherited && comparableMembers.Count > 7)))
        {
            hashCodeStatements = new();
            hashCodeStatements = hashCodeStatements.Add(generator.LocalDeclarationStatement(generator.TypeExpression(SemanticModel.Compilation.GetTypeByMetadataName("System.HashCode")!), "h", generator.DefaultExpression(generator.TypeExpression(SemanticModel.Compilation.GetTypeByMetadataName("System.HashCode")!))));

            if (inherited)
            {
                hashCodeStatements = hashCodeStatements.Add(generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName("h"), "Add"), [generator.InvocationExpression(generator.MemberAccessExpression(generator.BaseExpression(), "GetHashCode"), [])]));
            }

            foreach (var f in comparableMembers)
            {
                hashCodeStatements = hashCodeStatements.Add(generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName("h"), "Add"), [generator.IdentifierName(f.Name)]));
            }

            hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName("h"), "ToHashCode"), [])));
        }
        else if (hasHashCode)
        {
            // Create hashcode invocation.
            var prettifyGetHashCode = (!inherited && comparableMembers.Count > 1) || (inherited && comparableMembers.Count > 0);
            List<SyntaxNodeOrToken> arguments = [];
            bool firstMember = !inherited;

            if (inherited)
            {
                arguments.Add(SyntaxFactory.Argument(SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.BaseExpression(), SyntaxFactory.IdentifierName("GetHashCode")), SyntaxFactory.ArgumentList()))
.WithLeadingTrivia(prettifyGetHashCode ? SyntaxFactory.Whitespace("\t\t") : SyntaxFactory.ElasticMarker));
            }

            foreach (var f in comparableMembers)
            {
                if (!firstMember)
                {
                    // Add comma.
                    var token = SyntaxFactory.Token(SyntaxKind.CommaToken)
                    .WithTrailingTrivia(prettifyGetHashCode ? SyntaxFactory.EndOfLine("\r\n") : SyntaxFactory.ElasticMarker);
                    arguments.Add(token);
                }

                firstMember = false;
                arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(f.Name))
                .WithLeadingTrivia(prettifyGetHashCode ? SyntaxFactory.Whitespace("\t\t") : SyntaxFactory.ElasticMarker));
            }

            var argumentList = SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(prettifyGetHashCode ? SyntaxFactory.EndOfLine("\r\n") : SyntaxFactory.ElasticMarker),
                SyntaxFactory.SeparatedList<ArgumentSyntax>(arguments),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken));
            hashCodeStatements = new([
                generator.ReturnStatement(SyntaxFactory.InvocationExpression((MemberAccessExpressionSyntax)generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("System"), "HashCode"), "Combine"), argumentList)),
                ]);
        }
        else
        {
            if (inherited && comparableMembers.Count == 0)
            {
                hashCodeStatements = new(generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(generator.BaseExpression(), "GetHashCode"), [])));
            }
            else if (!inherited && comparableMembers.Count == 1)
            {
                hashCodeStatements = new(generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName(comparableMembers[0].Name), "GetHashCode"), [])));
            }
            else
            {
                var tuple = ((TupleExpressionSyntax)generator.TupleExpression(comparableMembers.Select(m => generator.IdentifierName(m.Name))))
                .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")));
                var tupleArguments = tuple.Arguments;

                if (inherited)
                {
                    tupleArguments = tupleArguments.Insert(0, SyntaxFactory.Argument((ExpressionSyntax)generator.InvocationExpression(generator.MemberAccessExpression(generator.BaseExpression(), "GetHashCode"), [])));
                }

                for (int i = 0; i < tupleArguments.Count; i++)
                {
                    tupleArguments = tupleArguments.Replace(tupleArguments[i], tupleArguments[i].WithLeadingTrivia(SyntaxFactory.Whitespace("\t\t")));
                }

                for (int i = 0; i < tupleArguments.SeparatorCount; i++)
                {
                    tupleArguments = tupleArguments.ReplaceSeparator(tupleArguments.GetSeparator(i), SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")));
                }

                tuple = tuple.WithArguments(tupleArguments);
                hashCodeStatements = new(generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(tuple, "GetHashCode"), [])));
            }
        }

        var equals = generator.MethodDeclaration(
            EqualsMethod,
            accessibility: Accessibility.Public,
            modifiers: originalRecordSymbol.IsValueType ? DeclarationModifiers.ReadOnly : originalRecordSymbol.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual,
            returnType: generator.TypeExpression(SpecialType.System_Boolean),
            parameters: [generator.ParameterDeclaration("other", originalRecordSymbol.IsValueType ? generator.TypeExpression(originalRecordSymbol) : generator.NullableTypeExpression(generator.TypeExpression(originalRecordSymbol)))],
            statements: [
                generator.ReturnStatement(equalityExpression),
            ])
        .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var getHashCode = generator.MethodDeclaration(
            GetHashCodeMethod,
            accessibility: Accessibility.Public,
            modifiers: originalRecordSymbol.IsValueType ? DeclarationModifiers.Override | DeclarationModifiers.ReadOnly : DeclarationModifiers.Override,
            returnType: generator.TypeExpression(SpecialType.System_Int32),
            parameters: [],
            statements: hashCodeStatements)
            .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        SyntaxNode newRecord = originalRecord;
        if (((RecordDeclarationSyntax)originalRecord).SemicolonToken != default)
        {
            newRecord = ((RecordDeclarationSyntax)originalRecord).WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
            .WithSemicolonToken(default)
            .NormalizeWhitespace();
        }

        newRecord = generator.AddMembers(newRecord, equals, getHashCode);

        var imports = SemanticModel.GetImportScopes(originalRecord.Span.Start, cancellationToken);
        var systemImported = imports.Any(i => i.Imports.Any(i => i.NamespaceOrType is INamespaceSymbol { Name: "System", ContainingNamespace.IsGlobalNamespace: true }));
        var scgImported = imports.Any(i => i.Imports.Any(i => i.NamespaceOrType is INamespaceSymbol { Name: "Generic", ContainingNamespace: { Name: "Collections", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } }));
        SyntaxList<SyntaxNode> namespacesToImport = new();
        if (!systemImported && hasHashCode && (comparableMembers.Count > 0 || inherited))
        {
            namespacesToImport = namespacesToImport.Add(generator.IdentifierName("System"));
        }

        if (usesEC && !scgImported)
        {
            namespacesToImport = namespacesToImport.Add(generator.DottedName("System.Collections.Generic"));
        }

        var newRoot = generator.AddNamespaceImports(SyntaxRoot.ReplaceNode(originalRecord, newRecord), namespacesToImport);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}
