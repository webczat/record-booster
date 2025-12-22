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

    public async Task<bool> TryRegister(RecordDeclarationSyntax originalRecord, CancellationToken cancellationToken = default)
    {
        originalRecord = originalRecord ?? throw new ArgumentNullException(nameof(originalRecord));

        // Get symbol for this record.
        var symbol = SemanticModel.GetDeclaredSymbol(originalRecord, cancellationToken);

        if (symbol is not ITypeSymbol originalRecordSymbol)
        {
            return false;
        }

        if (!await PrepareAsync(originalRecord, originalRecordSymbol))
        {
            return false;
        }

        Context.RegisterRefactoring(CodeAction.Create(Title, ct => ExecuteAsync(originalRecord, originalRecordSymbol, ct), Key));
        return true;
    }

    protected abstract Task<bool> PrepareAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol);

    protected abstract Task<Document> ExecuteAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken = default);
}
