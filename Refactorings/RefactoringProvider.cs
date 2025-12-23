// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Webczat.RecordBooster.Refactorings;

/// <summary>
///  Provides record related refactorings to the IDE.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp)]
public sealed class RefactoringProvider : CodeRefactoringProvider
{
    /// <summary>
    /// Computes and registers applicable refactorings for given context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that no refactoring will be registered if given type is not a record.
    /// </para>
    /// </remarks>
    /// <param name="context">The refactoring context describing location where quick fix was invoked.</param>
    /// <returns>A task that completes when refactorings are registered.</returns>
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

        if (codeToRefactor is not RecordDeclarationSyntax originalRecord)
        {
            return;
        }

        _ = await new ToStringRefactoring(context, root, semanticModel)
            .TryRegisterAsync(originalRecord);
        _ = await new PrintMembersRefactoring(context, root, semanticModel)
.TryRegisterAsync(originalRecord);
        _ = await new EqualsAndGetHashCodeRefactoring(context, root, semanticModel)
.TryRegisterAsync(originalRecord);
        _ = await new DeconstructRefactoring(context, root, semanticModel)
.TryRegisterAsync(originalRecord);
    }

    // Gets the type that is to be affected by refactorings.
    private static TypeDeclarationSyntax? GetType(SyntaxNode root, TextSpan span, SourceText text)
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
            return member.Parent as TypeDeclarationSyntax;
        }

        // Expect we're on a type declaration, standing on non type members directly is not allowed.
        return member as TypeDeclarationSyntax;
    }

    // Check whether cursor/selection is between given members.
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
}
