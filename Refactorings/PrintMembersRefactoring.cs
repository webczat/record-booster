// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Webczat.RecordBooster.Refactorings;

public sealed class PrintMembersRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel) :
CodeRefactoring(context, syntaxRoot, semanticModel)
{
    public const string Method = "PrintMembers";

    private readonly ITypeSymbol? _stringBuilderSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Text.StringBuilder");

    public override string Key => "PrintMembers";

    public override string Title => "Generate default record \"PrintMembers\"";

    protected override bool Prepare(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol)
    {
        if (_stringBuilderSymbol is null)
        {
            return false;
        }

        return !RecordHelpers.HasExplicitPrintMembers(originalRecordSymbol, _stringBuilderSymbol);
    }

    protected async override Task<Document> Execute(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default)
    {
        var document = Context.Document;
        var generator = SyntaxGenerator.GetGenerator(document);

        var stringBuilder = generator.TypeExpression(_stringBuilderSymbol!);
        var sb = generator.IdentifierName("sb");
        var runtimeHelpers = generator.MemberAccessExpression(
            generator.MemberAccessExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName("System"),
                    "Runtime"),
                "CompilerServices"),
            "RuntimeHelpers");

        // Retrieve printable members, being public instance fields and readable properties.
        var printableMembers = originalRecordSymbol.GetMembers()
            .Where(m => m is
                IFieldSymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } or
                IPropertySymbol
                {
                    IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null,
                    Parameters.IsDefaultOrEmpty: true, IsIndexer: false
                })
            .ToList();

        // This works because records inherit only from other records or Object.
        var inherited = originalRecordSymbol.BaseType?.IsRecord ?? false;
        var accessibility = originalRecordSymbol.IsSealed && !inherited ? Accessibility.Private : Accessibility.Protected;
        var modifiers = inherited ?
            DeclarationModifiers.Override :
            originalRecordSymbol.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;

        // Structs default to readonly unless non readonly getters detected.
        if (originalRecordSymbol.IsValueType && !printableMembers.Any(m => m is IPropertySymbol { GetMethod.IsReadOnly: false }))
        {
            modifiers |= DeclarationModifiers.ReadOnly;
        }

        List<SyntaxNode> statements = [];
        if (printableMembers.Count == 0)
        {
            // Just return false or result of base's PrintMembers if inherited.
            statements.Add(generator.ReturnStatement(
                inherited ? generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.BaseExpression(), "PrintMembers"),
                    [sb]) :
                generator.FalseLiteralExpression()));
        }
        else
        {
            // If neither inherited nor value type, insert call to EnsureSufficientExecutionStack.
            if (!inherited && !originalRecordSymbol.IsValueType)
            {
                statements.Add(generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(runtimeHelpers, "EnsureSufficientExecutionStack"))));
            }

            // Inherited records call up to base's PrintMembers.
            if (inherited)
            {
                statements.Add(generator.IfStatement(
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(generator.BaseExpression(), "PrintMembers"),
                        [sb]),
                    [
                        generator.ExpressionStatement(generator.InvocationExpression(
                            generator.MemberAccessExpression(sb, "Append"),
                            [generator.LiteralExpression(", ")]))
                ]));
            }

            // Add all printable members.
            var firstMember = true;
            foreach (var m in printableMembers)
            {
                var memberType = (m as IFieldSymbol)?.Type ?? ((IPropertySymbol)m).Type;

                if (!firstMember)
                {
                    statements.Add(generator.ExpressionStatement(generator.InvocationExpression(
                        generator.MemberAccessExpression(sb, "Append"),
                        [generator.LiteralExpression(", ")])));
                }

                firstMember = false;
                statements.Add(generator.ExpressionStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(sb, "Append"),
                    [generator.LiteralExpression($"{m.Name} = ")])));

                // Append member value directly, or through ToString if it's a value type.
                if (memberType.IsValueType)
                {
                    statements.Add(generator.ExpressionStatement(generator.InvocationExpression(
                        generator.MemberAccessExpression(sb, "Append"),
                        [generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.MemberAccessExpression(generator.ThisExpression(), m.Name),
                            "ToString"))])));
                }
                else
                {
                    statements.Add(generator.ExpressionStatement(generator.InvocationExpression(
                        generator.MemberAccessExpression(sb, "Append"),
                        [generator.MemberAccessExpression(generator.ThisExpression(), m.Name)])));
                }
            }

            statements.Add(generator.ReturnStatement(generator.TrueLiteralExpression()));
        }

        // Generate PrintMembers.
        var printMembers = generator.MethodDeclaration(
        name: Method,
        parameters: [generator.ParameterDeclaration("sb", stringBuilder)],
        returnType: generator.TypeExpression(SpecialType.System_Boolean),
        accessibility: accessibility,
        modifiers: modifiers,
        statements: statements)
        .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        var newRecord = generator.AddMembers(originalRecord, printMembers);
        var newRoot = SyntaxRoot.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}
