// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Webczat.RecordBooster.Refactorings;

/// <summary>
/// This class contains manual formatting utilities.
/// </summary>
public static class FormatterUtils
{
    /// <summary>
    /// Formats a binary expression, splitting it into multiple lines.
    /// </summary>
    /// <param name="expression">The expression to format.</param>
    /// <param name="depth">The expected indent level.</param>
    /// <returns>The formatted expression.</returns>
    public static BinaryExpressionSyntax FormatBinaryExpression(BinaryExpressionSyntax expression, int depth = 2) =>
        expression.WithOperatorToken(expression.OperatorToken
            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")))
            .WithRight(expression.Right.WithLeadingTrivia(SyntaxFactory.Whitespace(string.Join(string.Empty, Enumerable.Repeat('\t', depth)))));

    /// <summary>
    /// Formats an argument list.
    /// </summary>
    /// <typeparam name="T">The type of arguments.</typeparam>
    /// <param name="arguments">A list of arguments to format.</param>
    /// <param name="depth">Expected indentation level.</param>
    /// <returns>The formatted argument list.</returns>
    public static SeparatedSyntaxList<T> FormatArgumentList<T>(SeparatedSyntaxList<T> arguments, int depth = 2)
    where T : SyntaxNode
    {
        if (arguments.Count <= 1)
        {
            return arguments;
        }

        // Add whitespace before each argument list item.
        for (int i = 0; i < arguments.Count; i++)
        {
            arguments = arguments.Replace(
                arguments[i],
                arguments[i].WithLeadingTrivia(SyntaxFactory.Whitespace(string.Join(string.Empty, Enumerable.Repeat('\t', depth)))));
        }

        // Add newline after each comma.
        for (int i = 0; i < arguments.SeparatorCount; i++)
        {
            arguments = arguments.ReplaceSeparator(
                arguments.GetSeparator(i),
                arguments.GetSeparator(i).WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")));
        }

        return arguments;
    }

    /// <summary>
    /// Formats the method invocation argument list.
    /// </summary>
    /// <param name="argumentList">The argument list.</param>
    /// <param name="depth">The expected indentation level.</param>
    /// <returns>The formatted argument list.</returns>
    public static ArgumentListSyntax FormatFunctionArgumentList(ArgumentListSyntax argumentList, int depth = 2)
    {
        if (argumentList.Arguments.Count <= 1)
        {
            return argumentList;
        }

        return argumentList.WithOpenParenToken(argumentList.OpenParenToken.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")))
            .WithArguments(FormatArgumentList(argumentList.Arguments, depth));
    }

    /// <summary>
    /// Formats a tuple expression.
    /// </summary>
    /// <param name="tuple">The tuple expression.</param>
    /// <param name="depth">The expected indentation level.</param>
    /// <returns>The formatted tuple expression.</returns>
    public static TupleExpressionSyntax FormatTupleExpression(TupleExpressionSyntax tuple, int depth = 2)
    {
        if (tuple.Arguments.Count <= 1)
        {
            return tuple;
        }

        return tuple.WithOpenParenToken(tuple.OpenParenToken.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")))
            .WithArguments(FormatArgumentList(tuple.Arguments, depth));
    }
}
