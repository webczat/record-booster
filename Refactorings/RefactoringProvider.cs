// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Webczat.RecordBooster.Refactorings;

[ExportCodeRefactoringProvider(LanguageNames.CSharp)]
public sealed class RefactoringProvider : CodeRefactoringProvider
{
    public const string ToStringKey = "ToString";
    public const string PrintMembersKey = "PrintMembers";
    public const string EqualsAndGetHashCodeKey = "EqualsAndGetHashCode;";
    public const string DeconstructKey = "Deconstruct";

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

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || semanticModel is null || text is null)
        {
            return;
        }

        // Find code to be refactored.
        var codeToRefactor = GetType(root, context.Span, text);

        if (codeToRefactor is null)
        {
            return;
        }

        var symbol = semanticModel.GetDeclaredSymbol(codeToRefactor, cancellationToken);

        if (symbol is not ITypeSymbol { IsRecord: true } recordSymbol)
        {
            return;
        }

        RegisterToStringAndPrintMembers(context, root, recordSymbol, codeToRefactor, semanticModel.Compilation);
        RegisterEqualsAndGetHashCode(context, root, recordSymbol, codeToRefactor, semanticModel);
        RegisterDeconstruct(context, root, recordSymbol, codeToRefactor, semanticModel, cancellationToken);
    }

    private static SyntaxNode? GetType(SyntaxNode root, TextSpan span, SourceText text)
    {
        // Find on what node we're standing.
        var node = root.FindNode(span, findInsideTrivia: false, getInnermostNodeForTie: true);

        // We expect we're starting at or next to a member declaration, which includes top level types.
        if (node is not MemberDeclarationSyntax member)
        {
            return null;
        }

        // If we're actually standing between members of a type, return parent of current member.
        if (IsBetweenMembers(member, span, text))
        {
            return member.Parent;
        }

        // Expect we're on a type declaration, standing on non type members directly is not allowed.
        return member as TypeDeclarationSyntax;
    }

    private static bool IsBetweenMembers(MemberDeclarationSyntax member, TextSpan span, SourceText text)
    {
        // We assume being between members when we're on a blank line between member declarations.
        // We look at members from the perspective of their parent container, which is expected to be a type.
        var parent = member.Parent;
        if (parent is not TypeDeclarationSyntax parentType)
        {
            return false;
        }

        var memberIndex = parentType.Members.IndexOf(member);
        Debug.Assert(memberIndex != -1, "Member must be found");

        var previousMemberSpan = memberIndex > 0 ? parentType.Members[memberIndex - 1].Span : parentType.OpenBraceToken.Span;
        if (previousMemberSpan.End > span.Start || span.End > member.Span.Start)
        {
            return false;
        }

        var lineSpan = text.Lines.GetLinePositionSpan(span);
        var startLine = lineSpan.Start.Line;
        var endLine = span.IsEmpty || lineSpan.End.Character > 0 ? lineSpan.End.Line : lineSpan.End.Line - 1;
        for (int i = startLine; i <= endLine; i++)
        {
            var line = text.Lines[i];

            for (int j = line.Start; j < line.End; j++)
            {
                if (!SyntaxFacts.IsWhitespace(text[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void RegisterToStringAndPrintMembers(CodeRefactoringContext context, SyntaxNode root, ITypeSymbol recordSymbol, SyntaxNode originalRecord, Compilation compilation)
    {
        // Fetch symbols needed for ToString and PrintMembers.
        var stringBuilderSymbol = compilation.GetTypeByMetadataName("System.Text.StringBuilder");
        if (stringBuilderSymbol is null)
        {
            return;
        }

        // See if appropriate symbols exist and prevent their generation if so.
        // Implicitly declared symbols are assumed not to exist.
        var hasToString = recordSymbol.GetMembers("ToString").Any(s => s is IMethodSymbol { Parameters: [], Arity: 0, IsImplicitlyDeclared: false });
        var hasPrintMembers = recordSymbol.GetMembers("PrintMembers").Any(s => s is IMethodSymbol { Parameters: [{ Type: var type, RefKind: RefKind.None }], Arity: 0, IsImplicitlyDeclared: false } &&
            SymbolEqualityComparer.Default.Equals(type, stringBuilderSymbol));

        if (!hasToString)
        {
            context.RegisterRefactoring(CodeAction.Create(
                "Generate default record \"ToString\"",
                ct => GenerateToString(context.Document, root, originalRecord, recordSymbol, stringBuilderSymbol),
                ToStringKey));
        }

        if (!hasPrintMembers)
        {
            context.RegisterRefactoring(CodeAction.Create(
                "Generate default record \"PrintMembers\"",
                ct => GeneratePrintMembers(context.Document, root, originalRecord, recordSymbol, stringBuilderSymbol),
                PrintMembersKey));
        }
    }

    private static async Task<Document> GenerateToString(Document document, SyntaxNode root, SyntaxNode originalRecord, ITypeSymbol recordSymbol, ITypeSymbol stringBuilderSymbol)
    {
        var isReadOnly = recordSymbol.IsValueType && recordSymbol.GetMembers("PrintMembers")
            .Any(m => m is IMethodSymbol { Arity: 0, Parameters: [{ Type: var type }], IsReadOnly: true } method
            && SymbolEqualityComparer.Default.Equals(type, stringBuilderSymbol));

        var generator = SyntaxGenerator.GetGenerator(document);

        // Few often used names...
        var recordExpression = generator.TypeExpression(recordSymbol);
        var stringBuilder = generator.TypeExpression(stringBuilderSymbol);
        var sb = generator.IdentifierName("sb");

        // Generate the ToString method.
        var modifiers = isReadOnly ? DeclarationModifiers.ReadOnly | DeclarationModifiers.Override : DeclarationModifiers.Override;
        var toString = generator.MethodDeclaration(
            "ToString",
            returnType: generator.TypeExpression(SpecialType.System_String),
            accessibility: Accessibility.Public,
            modifiers: modifiers,
            statements: [
                generator.LocalDeclarationStatement(stringBuilder, "sb", generator.ObjectCreationExpression(stringBuilder)),
                generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), generator.NameOfExpression(recordExpression))),
                generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), generator.LiteralExpression(" { ")))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed),
                generator.IfStatement(
                    generator.InvocationExpression(generator.IdentifierName("PrintMembers"), [sb]),
                    [generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), generator.LiteralExpression(' ')))])
                    .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed),
                generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), generator.LiteralExpression('}'))),
                generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "ToString"))),
            ])
            .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        // Add member to end of record.
        var newRecord = generator.AddMembers(originalRecord, toString);
        var newRoot = root.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }

    private static async Task<Document> GeneratePrintMembers(Document document, SyntaxNode root, SyntaxNode originalRecord, ITypeSymbol recordSymbol, ITypeSymbol stringBuilderSymbol)
    {
        var generator = SyntaxGenerator.GetGenerator(document);

        var stringBuilder = generator.TypeExpression(stringBuilderSymbol);
        var sb = generator.IdentifierName("sb");
        var runtimeHelpers = generator.MemberAccessExpression(generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("System"), "Runtime"), "CompilerServices"), "RuntimeHelpers");

        // Retrieve printable members, being public instance fields and readable properties.
        var printableMembers = recordSymbol.GetMembers()
        .Where(m => m is
            IFieldSymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } or
            IPropertySymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null, Parameters.IsDefaultOrEmpty: true, IsIndexer: false })
        .ToList();

        var inherited = recordSymbol.BaseType?.IsRecord ?? false;
        var accessibility = recordSymbol.IsSealed && !inherited ? Accessibility.Private : Accessibility.Protected;
        var modifiers = inherited ? DeclarationModifiers.Override : recordSymbol.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;

        // Structs default to readonly unless non readonly getters detected.
        if (recordSymbol.IsValueType && !printableMembers.Any(m => m is IPropertySymbol { GetMethod.IsReadOnly: false }))
        {
            modifiers |= DeclarationModifiers.ReadOnly;
        }

        // Prepare a list of statements that append members.
        var firstMember = true;
        List<SyntaxNode> appendStatements = [];
        foreach (var m in printableMembers)
        {
            var memberType = (m as IFieldSymbol)?.Type ?? ((IPropertySymbol)m).Type;

            if (!firstMember)
            {
                appendStatements.Add(generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), [generator.LiteralExpression(", ")])));
            }

            firstMember = false;
            appendStatements.Add(generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), [generator.LiteralExpression($"{m.Name} = ")])));

            // Append member value directly, or through ToString if it's a value type.
            if (memberType.IsValueType)
            {
                appendStatements.Add(generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), [generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName(m.Name), "ToString"))])));
            }
            else
            {
                appendStatements.Add(generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), [generator.IdentifierName(m.Name)])));
            }
        }

        // Generate PrintMembers.
        var printMembers = generator.MethodDeclaration(
        name: "PrintMembers",
        parameters: [generator.ParameterDeclaration("sb", stringBuilder)],
        returnType: generator.TypeExpression(SpecialType.System_Boolean),
        accessibility: accessibility,
        modifiers: modifiers,
        statements: [
            ..(SyntaxNode[])(printableMembers.Count == 0 || inherited || recordSymbol.IsValueType ? [] : [generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(runtimeHelpers, "EnsureSufficientExecutionStack")))]),
            ..(SyntaxNode[])(!inherited || printableMembers.Count == 0 ? [] : [
                generator.IfStatement(
                    generator.InvocationExpression(generator.MemberAccessExpression(generator.BaseExpression(), "PrintMembers"), [sb]),
                    [
                        generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(sb, "Append"), [generator.LiteralExpression(", ")]))
                ]),
            ]),
            ..appendStatements,
            generator.ReturnStatement(
                printableMembers.Count == 0 ? (
                    inherited ? generator.InvocationExpression(generator.MemberAccessExpression(generator.BaseExpression(), "PrintMembers"), [sb]) :
                    generator.FalseLiteralExpression()) :
                    generator.TrueLiteralExpression()),
        ])
        .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var newRecord = generator.AddMembers(originalRecord, printMembers);
        var newRoot = root.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }

    private static void RegisterEqualsAndGetHashCode(CodeRefactoringContext context, SyntaxNode root, ITypeSymbol recordSymbol, SyntaxNode originalRecord, SemanticModel semanticModel)
    {
        bool hasEquals = recordSymbol.GetMembers("Equals")
        .Any(m => m is IMethodSymbol { Arity: 0, IsImplicitlyDeclared: false, Parameters: [{ Type: var type, RefKind: RefKind.None }] } &&
            SymbolEqualityComparer.Default.Equals(type, recordSymbol));
        bool hasGetHashCode = recordSymbol.GetMembers("GetHashCode")
.Any(m => m is IMethodSymbol { Arity: 0, IsImplicitlyDeclared: false, Parameters: [] });

        if (!hasEquals && !hasGetHashCode)
        {
            context.RegisterRefactoring(CodeAction.Create(
                "Generate default record \"Equals\" and \"GetHashCode\"",
                ct => GenerateEqualsAndGetHashCode(context.Document, root, originalRecord, recordSymbol, semanticModel, ct),
                EqualsAndGetHashCodeKey));
        }
    }

    private static async Task<Document> GenerateEqualsAndGetHashCode(Document document, SyntaxNode root, SyntaxNode originalRecord, ITypeSymbol recordSymbol, SemanticModel semanticModel, CancellationToken cancellationToken = default)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var inherited = recordSymbol.BaseType?.IsRecord ?? false;

        // We try to compare actual object data, so take all instance fields irrespective of accessibility, but not properties.
        // Then because we can't access auto props directly via fields, take respective properties in their place.
        var comparableMembers = recordSymbol.GetMembers()
        .Where(m => m is IFieldSymbol { IsStatic: false })
        .Select(m => m is IFieldSymbol { AssociatedSymbol: not null } f ? f.AssociatedSymbol : m)
        .ToList();

        // In case it's a reference type not inheriting from anything, add EqualityContract first.
        if (!recordSymbol.IsValueType && !inherited)
        {
            comparableMembers.Insert(0, recordSymbol.GetMembers("EqualityContract").Single(m => m is IPropertySymbol));
        }

        // Construct the equality expression based on members.
        ExpressionSyntax notNull = SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName("other"),
                SyntaxFactory.UnaryPattern(SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
        SyntaxNode? equalityExpression = recordSymbol.IsValueType ? null : notNull;

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

        var hasHashCode = semanticModel.Compilation.GetTypeByMetadataName("System.HashCode") is not null;

        SyntaxList<SyntaxNode> hashCodeStatements;
        if (comparableMembers.Count == 0 && !inherited)
        {
            hashCodeStatements = new(generator.ReturnStatement(generator.LiteralExpression(0)));
        }
        else if (hasHashCode && ((!inherited && comparableMembers.Count > 8) || (inherited && comparableMembers.Count > 7)))
        {
            hashCodeStatements = new();
            hashCodeStatements = hashCodeStatements.Add(generator.LocalDeclarationStatement(generator.TypeExpression(semanticModel.Compilation.GetTypeByMetadataName("System.HashCode")!), "h", generator.DefaultExpression(generator.TypeExpression(semanticModel.Compilation.GetTypeByMetadataName("System.HashCode")!))));

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
            "Equals",
            accessibility: Accessibility.Public,
            modifiers: recordSymbol.IsValueType ? DeclarationModifiers.ReadOnly : recordSymbol.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual,
            returnType: generator.TypeExpression(SpecialType.System_Boolean),
            parameters: [generator.ParameterDeclaration("other", recordSymbol.IsValueType ? generator.TypeExpression(recordSymbol) : generator.NullableTypeExpression(generator.TypeExpression(recordSymbol)))],
            statements: [
                generator.ReturnStatement(equalityExpression),
            ])
        .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var getHashCode = generator.MethodDeclaration(
            "GetHashCode",
            accessibility: Accessibility.Public,
            modifiers: recordSymbol.IsValueType ? DeclarationModifiers.Override | DeclarationModifiers.ReadOnly : DeclarationModifiers.Override,
            returnType: generator.TypeExpression(SpecialType.System_Int32),
            parameters: [],
            statements: hashCodeStatements)
            .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var newRecord = originalRecord;
        if (((RecordDeclarationSyntax)originalRecord).SemicolonToken != default)
        {
            newRecord = ((RecordDeclarationSyntax)originalRecord).WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
            .WithSemicolonToken(default)
            .NormalizeWhitespace();
        }

        newRecord = generator.AddMembers(newRecord, equals, getHashCode);

        var imports = semanticModel.GetImportScopes(originalRecord.Span.Start, cancellationToken);
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

        var newRoot = generator.AddNamespaceImports(root.ReplaceNode(originalRecord, newRecord), namespacesToImport);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }

    private static void RegisterDeconstruct(CodeRefactoringContext context, SyntaxNode root, ITypeSymbol recordSymbol, SyntaxNode originalRecord, SemanticModel semanticModel, CancellationToken cancellationToken = default)
    {
        // Ignore non positional records.
        if (((RecordDeclarationSyntax)originalRecord).ParameterList is not ParameterListSyntax parameterList || parameterList.Parameters.Count == 0)
        {
            return;
        }

        // Get all symbols corresponding to parameters.
        var parameterSymbols = new List<IParameterSymbol>();
        foreach (var parameter in parameterList.Parameters)
        {
            var symbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken);
            if (symbol is null)
            {
                return;
            }

            parameterSymbols.Add(symbol);
        }

        // Proper deconstruct depends on the primary constructor, so we need to check if order, type and ref kind of all parameters match expectations.
        var hasDeconstruct = recordSymbol.GetMembers("Deconstruct")
        .OfType<IMethodSymbol>()
        .Where(m => m is { Arity: 0, IsImplicitlyDeclared: false } &&
            m.Parameters.Length == parameterSymbols.Count)
        .Any(
            m => parameterSymbols.Zip(m.Parameters, (left, right) => (Left: left, Right: right))
            .All(p => SymbolEqualityComparer.Default.Equals(p.Left.Type, p.Right.Type) && p.Right.RefKind is not RefKind.None));

        if (!hasDeconstruct)
        {
            var associatedMembers = new List<ISymbol>(parameterSymbols.Count);
            foreach (var param in parameterSymbols)
            {
                var member = recordSymbol.GetMembers(param.Name)
                .SingleOrDefault(m => m is IFieldSymbol or (IPropertySymbol { GetMethod: not null }));
                if (member is null)
                {
                    return;
                }

                associatedMembers.Add(member);
            }

            context.RegisterRefactoring(CodeAction.Create(
                "Generate default record \"Deconstruct\"",
                ct => GenerateDeconstruct(context.Document, root, originalRecord, recordSymbol, associatedMembers),
                DeconstructKey));
        }
    }

    private static async Task<Document> GenerateDeconstruct(Document document, SyntaxNode root, SyntaxNode originalRecord, ITypeSymbol recordSymbol, List<ISymbol> associatedMembers)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var isReadOnly = recordSymbol.IsValueType;

        var parameterList = new List<SyntaxNode>(associatedMembers.Count);
        var assignments = new List<SyntaxNode>(associatedMembers.Count);
        foreach (var m in associatedMembers)
        {
            var type = (m as IFieldSymbol)?.Type ?? ((IPropertySymbol)m).Type;

            if (m is IPropertySymbol { GetMethod.IsReadOnly: false })
            {
                isReadOnly = false;
            }

            parameterList.Add(generator.ParameterDeclaration(m.Name, generator.TypeExpression(type), refKind: RefKind.Out));
            assignments.Add(generator.AssignmentStatement(generator.IdentifierName(m.Name), generator.MemberAccessExpression(generator.ThisExpression(), m.Name)));
        }

        var deconstruct = generator.MethodDeclaration(
            "Deconstruct",
            accessibility: Accessibility.Public,
            modifiers: isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None,
            parameters: parameterList,
            statements: assignments)
            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation, Simplifier.AddImportsAnnotation);

        var newRecord = generator.AddMembers(originalRecord, deconstruct);
        var newRoot = root.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}
