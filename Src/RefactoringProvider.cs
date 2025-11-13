// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

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

        if (root is null || semanticModel is null)
        {
            return;
        }

        // Find code to be refactored.
        var codeToRefactor = root.FindNode(context.Span, findInsideTrivia: false, getInnermostNodeForTie: true);
        var symbol = semanticModel.GetDeclaredSymbol(codeToRefactor, cancellationToken);

        if (symbol is not ITypeSymbol { IsRecord: true } recordSymbol)
        {
            return;
        }

        RegisterToString(context, root, recordSymbol, codeToRefactor, semanticModel.Compilation);
    }

    private static void RegisterToString(CodeRefactoringContext context, SyntaxNode root, ITypeSymbol recordSymbol, SyntaxNode originalRecord, Compilation compilation)
    {
        // Fetch symbols needed for ToString.
        var stringBuilderSymbol = compilation.GetTypeByMetadataName("System.Text.StringBuilder");

        if (stringBuilderSymbol is null)
        {
            return;
        }

        // See if appropriate symbols exist and prevent their generation if so.
        // Implicitly declared symbols are assumed not to exist.
        var hasToString = recordSymbol.GetMembers("ToString").Any(s => s is IMethodSymbol { Parameters: [], Arity: 0, IsImplicitlyDeclared: false });

        if (hasToString)
        {
            return;
        }

        context.RegisterRefactoring(CodeAction.Create(
            "Generate default record \"ToString\"",
            ct => GenerateToString(context.Document, root, originalRecord, recordSymbol, stringBuilderSymbol),
            ToStringKey));
    }

    private static async Task<Document> GenerateToString(Document document, SyntaxNode root, SyntaxNode originalRecord, ITypeSymbol recordSymbol, ITypeSymbol stringBuilderSymbol)
    {
        var isReadOnly = recordSymbol.IsValueType && recordSymbol.GetMembers("PrintMembers")
        .Any(m => m is IMethodSymbol { Arity: 0, Parameters: { Length: 1 }, IsReadOnly: true } method && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, stringBuilderSymbol));

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
}
