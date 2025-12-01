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

namespace Webczat.RecordBooster;

[ExportCodeRefactoringProvider(LanguageNames.CSharp)]
public sealed class RefactoringProvider : CodeRefactoringProvider
{
    public const string ToStringKey = "ToString";
    public const string PrintMembersKey = "PrintMembers";

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
        var hasPrintMembers = recordSymbol.GetMembers("PrintMembers").Any(s => s is IMethodSymbol { Parameters: [{ Type: var type }], Arity: 0, IsImplicitlyDeclared: false }
        && SymbolEqualityComparer.Default.Equals(type, stringBuilderSymbol));

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
}
