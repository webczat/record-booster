// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Webczat.RecordBooster.Refactorings;

/// <summary>
/// This class represents a code refactoring that can be run on records.
/// Objects of this class are created for single refactoring runs.
/// </summary>
/// <param name="context">The passed refactoring context.</param>
/// <param name="syntaxRoot">The syntax representation of compilation unit in which record is present.</param>
/// <param name="semanticModel">The semantic model.</param>
public abstract class CodeRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel)
{
    /// <summary>
    /// Gets the refactoring code action equivalence key.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// Gets the refactoring title.
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Gets the refactoring context containing document and refactoring span.
    /// </summary>
    protected CodeRefactoringContext Context { get; } = context;

    /// <summary>
    /// Gets the refactored document's syntax root.
    /// </summary>
    protected SyntaxNode SyntaxRoot { get; } = syntaxRoot ?? throw new ArgumentNullException(nameof(syntaxRoot));

    /// <summary>
    /// Gets the refactored document's semantic model.
    /// </summary>
    protected SemanticModel SemanticModel { get; } = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

    /// <summary>
    /// Tries to register the refactoring if preconditions are met.
    /// </summary>
    /// <param name="originalRecord">The syntax representing record to be refactored.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel refactoring registration.</param>
    /// <returns><c>true</c> if refactoring was registered, <c>false</c> othervise.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="originalRecord"/> is <c>null</c>.</exception>
    public async Task<bool> TryRegisterAsync(RecordDeclarationSyntax originalRecord, CancellationToken cancellationToken = default)
    {
        originalRecord = originalRecord ?? throw new ArgumentNullException(nameof(originalRecord));

        // Get symbol for this record.
        var symbol = SemanticModel.GetDeclaredSymbol(originalRecord, cancellationToken);

        if (symbol is not ITypeSymbol originalRecordSymbol)
        {
            return false;
        }

        if (!await PrepareAsync(originalRecord, originalRecordSymbol, cancellationToken))
        {
            return false;
        }

        Context.RegisterRefactoring(CodeAction.Create(Title, ct => ExecuteAsync(originalRecord, originalRecordSymbol, ct), Key));
        return true;
    }

    /// <summary>
    /// Prepares the refactoring for registration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is executed before refactoring is registered and might cache relevant syntactic or semantic information (like symbols) needed for proper refactoring execution.
    /// </para>
    /// <para>
    /// This method is also responsible for checking whether refactoring is applicable for a given span. For example, it can check whether required symbols exist or whether a method to be generated already exists.
    /// </para>
    /// </remarks>
    /// <param name="originalRecord">The original record's syntax.</param>
    /// <param name="originalRecordSymbol">The original record's symbol.</param>
    /// <param name="cancellationToken">The cancellation token used to cancel operation.</param>
    /// <returns>A task which completes to <c>true</c> if the refactoring should be registered, <c>false</c> othervise.</returns>
    protected abstract Task<bool> PrepareAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the refactoring.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that this method shouldn't throw or return early. Any error conditions should ideally be handled in the <see cref="PrepareAsync(RecordDeclarationSyntax, ITypeSymbol)"/>.
    /// </para>
    /// </remarks>
    /// <param name="originalRecord">The original record syntax.</param>
    /// <param name="originalRecordSymbol">The original record symbol.</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>A task completing with a <see cref="Document"/> representing the changed document.</returns>
    protected abstract Task<Document> ExecuteAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken);
}
