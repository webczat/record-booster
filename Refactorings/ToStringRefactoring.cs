// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Webczat.RecordBooster.Refactorings;

public sealed class ToStringRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel) :
CodeRefactoring(context, syntaxRoot, semanticModel)
{
    public const string Method = "ToString";

    private readonly ITypeSymbol? _stringBuilderSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Text.StringBuilder");

    public override string Key => "ToString";

    public override string Title => "Generate default record \"ToString\"";

    protected async override Task<bool> PrepareAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol)
    {
        if (_stringBuilderSymbol is null)
        {
            return false;
        }

        // Check for existing explicit ToString.
        return !RecordHelpers.HasExplicitToString(originalRecordSymbol);
    }

    protected async override Task<Document> ExecuteAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default)
    {
        var document = Context.Document;
        var isReadOnly = originalRecordSymbol.IsValueType && RecordHelpers.GetPrintMembers(originalRecordSymbol, _stringBuilderSymbol!)
            .Any(m => m is { IsReadOnly: true });

        var generator = SyntaxGenerator.GetGenerator(document);

        // Few often used names...
        var recordExpression = generator.TypeExpression(originalRecordSymbol);
        var stringBuilder = generator.TypeExpression(_stringBuilderSymbol!);
        var sb = generator.IdentifierName("sb");

        // Generate the ToString method.
        var modifiers = isReadOnly ? DeclarationModifiers.ReadOnly | DeclarationModifiers.Override : DeclarationModifiers.Override;
        var toString = generator.MethodDeclaration(
            Method,
            returnType: generator.TypeExpression(SpecialType.System_String),
            accessibility: Accessibility.Public,
            modifiers: modifiers,
            statements: [
                generator.LocalDeclarationStatement(stringBuilder, "sb", generator.ObjectCreationExpression(stringBuilder)),
                generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(sb, "Append"),
                    generator.NameOfExpression(recordExpression))),
                generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(sb, "Append"),
                    generator.LiteralExpression(" { ")))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed),
                generator.IfStatement(
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(generator.ThisExpression(), "PrintMembers"),
                        [sb]),
                    [generator.ExpressionStatement(generator.InvocationExpression(
                        generator.MemberAccessExpression(sb, "Append"),
                        generator.LiteralExpression(' ')))])
                    .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed),
                generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(sb, "Append"),
                    generator.LiteralExpression('}'))),
                generator.ReturnStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(sb, "ToString"))),
            ])
            .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        // Add member to end of record.
        var newRecord = generator.AddMembers(originalRecord, toString);
        var newRoot = SyntaxRoot.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}
