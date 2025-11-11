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

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
        .ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
        .ConfigureAwait(false);
        var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken)
        .ConfigureAwait(false);

        if (compilation is null || root is null || semanticModel is null)
        {
            return;
        }

        // Find code to be refactored.
        var codeToRefactor = root.FindNode(context.Span, false, true);
        var symbol = semanticModel.GetDeclaredSymbol(codeToRefactor, context.CancellationToken);

        if (symbol is not ITypeSymbol recordSymbol || !recordSymbol.IsRecord)
        {
            return;
        }

        RegisterToStringAndPrintMembers(context, root, recordSymbol, codeToRefactor, compilation);
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
        var toString = recordSymbol.GetMembers("ToString").SingleOrDefault(s => s is IMethodSymbol { Parameters: [], Arity: 0, IsImplicitlyDeclared: false });

        if (toString is null)
        {
            context.RegisterRefactoring(CodeAction.Create(
                "Generate default record \"ToString\"",
                ct => GenerateToString(context.Document, root, originalRecord, recordSymbol, stringBuilderSymbol, ct),
                ToStringKey));
        }
    }

    private static async Task<Document> GenerateToString(Document document, SyntaxNode root, SyntaxNode originalRecord, ITypeSymbol recordSymbol, ITypeSymbol stringBuilderSymbol, CancellationToken cancellationToken = default)
    {
        var generator = SyntaxGenerator.GetGenerator(document);

        // Few often used names...
        var recordExpression = generator.TypeExpression(recordSymbol);
        var stringBuilder = generator.TypeExpression(stringBuilderSymbol);
        var sb = generator.IdentifierName("sb");

        // Generate the ToString method.
        var toString = generator.MethodDeclaration(
            "ToString",
            returnType: generator.TypeExpression(SpecialType.System_String),
            accessibility: Accessibility.Public,
            modifiers: DeclarationModifiers.Override,
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
            ]);

        // Add member to end of record.
        var newRecord = generator.AddMembers(originalRecord, toString);
        var newRoot = root.ReplaceNode(originalRecord, newRecord).WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}