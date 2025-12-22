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
}
