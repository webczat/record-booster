// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Webczat.RecordBooster.Refactorings;

public abstract class CodeRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel)
{
    public abstract string Key { get; }

    public abstract string Title { get; }

    protected CodeRefactoringContext Context { get; } = context;

    protected SyntaxNode SyntaxRoot { get; } = syntaxRoot ?? throw new ArgumentNullException(nameof(syntaxRoot));

    protected SemanticModel SemanticModel { get; } = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

    public bool TryRegister(RecordDeclarationSyntax originalRecord, CancellationToken cancellationToken = default)
    {
        originalRecord = originalRecord ?? throw new ArgumentNullException(nameof(originalRecord));

        // Get symbol for this record.
        var symbol = SemanticModel.GetDeclaredSymbol(originalRecord, cancellationToken);

        if (symbol is not ITypeSymbol originalRecordSymbol)
        {
            return false;
        }

        if (!IsApplicable(originalRecord, originalRecordSymbol))
        {
            return false;
        }

        Context.RegisterRefactoring(CodeAction.Create(Title, ct => Execute(originalRecord, originalRecordSymbol, ct), Key));
        return true;
    }

    protected abstract bool IsApplicable(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol);

    protected abstract Task<Document> Execute(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default);
}
