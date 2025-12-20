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

        return !originalRecordSymbol.GetMembers(Method)
            .Any(s => s is IMethodSymbol { Parameters: [{ Type: var type, RefKind: RefKind.None }], Arity: 0, IsImplicitlyDeclared: false } &&
                SymbolEqualityComparer.Default.Equals(type, _stringBuilderSymbol));
    }

    protected async override Task<Document> Execute(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default)
    {
        var document = Context.Document;
        var generator = SyntaxGenerator.GetGenerator(document);

        var stringBuilder = generator.TypeExpression(_stringBuilderSymbol!);
        var sb = generator.IdentifierName("sb");
        var runtimeHelpers = generator.MemberAccessExpression(generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("System"), "Runtime"), "CompilerServices"), "RuntimeHelpers");

        // Retrieve printable members, being public instance fields and readable properties.
        var printableMembers = originalRecordSymbol.GetMembers()
            .Where(m => m is
                IFieldSymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } or
                IPropertySymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null, Parameters.IsDefaultOrEmpty: true, IsIndexer: false })
            .ToList();

        var inherited = originalRecordSymbol.BaseType?.IsRecord ?? false;
        var accessibility = originalRecordSymbol.IsSealed && !inherited ? Accessibility.Private : Accessibility.Protected;
        var modifiers = inherited ? DeclarationModifiers.Override : originalRecordSymbol.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;

        // Structs default to readonly unless non readonly getters detected.
        if (originalRecordSymbol.IsValueType && !printableMembers.Any(m => m is IPropertySymbol { GetMethod.IsReadOnly: false }))
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
        name: Method,
        parameters: [generator.ParameterDeclaration("sb", stringBuilder)],
        returnType: generator.TypeExpression(SpecialType.System_Boolean),
        accessibility: accessibility,
        modifiers: modifiers,
        statements: [
            ..(SyntaxNode[])(printableMembers.Count == 0 || inherited || originalRecordSymbol.IsValueType ? [] : [generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(runtimeHelpers, "EnsureSufficientExecutionStack")))]),
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
        var newRoot = SyntaxRoot.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}
