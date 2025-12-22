// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Webczat.RecordBooster.Refactorings;

public static class FormatterUtils
{
    public static BinaryExpressionSyntax FormatBinaryExpression(BinaryExpressionSyntax expression) =>
        expression.WithOperatorToken(expression.OperatorToken
            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")))
            .WithRight(expression.Right.WithLeadingTrivia(SyntaxFactory.Whitespace("\t\t")));

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

    public static ArgumentListSyntax FormatFunctionArgumentList(ArgumentListSyntax argumentList, int depth = 2)
    {
        if (argumentList.Arguments.Count <= 1)
        {
            return argumentList;
        }

        return argumentList.WithOpenParenToken(argumentList.OpenParenToken.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")))
            .WithArguments(FormatArgumentList(argumentList.Arguments, depth));
    }

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
