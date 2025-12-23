// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Webczat.RecordBooster.Refactorings;

public sealed class DeconstructRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel) :
CodeRefactoring(context, syntaxRoot, semanticModel)
{
    public const string Method = "Deconstruct";

    private IList<IParameterSymbol>? _parameterSymbols;
    private IList<ISymbol>? _associatedMembers;

    public override string Key => "Deconstruct";

    public override string Title => "Generate default record \"Deconstruct\"";

    protected async override Task<bool> PrepareAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol)
    {
        // Ignore non positional records.
        if (originalRecord.ParameterList is not ParameterListSyntax parameterList || parameterList.Parameters.Count == 0)
        {
            return false;
        }

        // Get all symbols corresponding to parameters.
        _parameterSymbols = new List<IParameterSymbol>();
        foreach (var parameter in parameterList.Parameters)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(parameter, Context.CancellationToken);
            if (symbol is not IParameterSymbol ps)
            {
                return false;
            }

            _parameterSymbols.Add(ps);
        }

        _associatedMembers = new List<ISymbol>(_parameterSymbols.Count);
        foreach (var param in _parameterSymbols)
        {
            var member = originalRecordSymbol.GetMembers(param.Name)
            .SingleOrDefault(m => m is IFieldSymbol or (IPropertySymbol { GetMethod: not null }));
            if (member is null)
            {
                return false;
            }

            _associatedMembers.Add(member);
        }

        // Proper deconstruct depends on the primary constructor, so we need to check if order, type and ref kind of all parameters match expectations.
        return !RecordHelpers.HasExplicitDeconstruct(originalRecordSymbol, _parameterSymbols);
    }

    protected async override Task<Document> ExecuteAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default)
    {
        var document = Context.Document;
        var generator = SyntaxGenerator.GetGenerator(document);
        var isReadOnly = originalRecordSymbol.IsValueType;

        // Compute deconstruct method parameters and assignment expressions based on
        // members associated to primary constructor parameters.
        var parameterList = new SyntaxList<SyntaxNode>();
        var assignments = new SyntaxList<SyntaxNode>();
        foreach (var m in _associatedMembers!)
        {
            var type = (m as IFieldSymbol)?.Type ?? ((IPropertySymbol)m).Type;

            if (m is IPropertySymbol { GetMethod.IsReadOnly: false })
            {
                // Presence of any associated property with non readonly getter makes method non readonly.
                isReadOnly = false;
            }

            parameterList = parameterList.Add(generator.ParameterDeclaration(
                m.Name,
                generator.TypeExpression(type),
                refKind: RefKind.Out));
            assignments = assignments.Add(generator.AssignmentStatement(
                generator.IdentifierName(m.Name),
                generator.MemberAccessExpression(generator.ThisExpression(), m.Name)));
        }

        var deconstruct = generator.MethodDeclaration(
            Method,
            accessibility: Accessibility.Public,
            modifiers: isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None,
            parameters: parameterList,
            statements: assignments)
            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation, Simplifier.AddImportsAnnotation);

        var newRecord = generator.AddMembers(originalRecord, deconstruct);
        var newRoot = SyntaxRoot.ReplaceNode(originalRecord, newRecord);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }
}
