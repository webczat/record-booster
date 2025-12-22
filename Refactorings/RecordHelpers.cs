// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;

namespace Webczat.RecordBooster.Refactorings;

public static class RecordHelpers
{
    public static IEnumerable<IMethodSymbol> GetToString(ITypeSymbol record) =>
        record.GetMembers("ToString")
            .OfType<IMethodSymbol>()
            .Where(s => s is { Parameters: [], Arity: 0 });

    public static bool HasExplicitToString(ITypeSymbol record) =>
        GetToString(record).Any(s => s is { IsImplicitlyDeclared: false });

    public static IEnumerable<IMethodSymbol> GetPrintMembers(ITypeSymbol record, ISymbol stringBuilderSymbol) =>
        record.GetMembers("PrintMembers")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0, Parameters: [{ Type: var type, RefKind: RefKind.None }] }
            && SymbolEqualityComparer.Default.Equals(type, stringBuilderSymbol));

    public static bool HasExplicitPrintMembers(ITypeSymbol record, ISymbol stringBuilderSymbol) =>
        GetPrintMembers(record, stringBuilderSymbol).Any(m => m is { IsImplicitlyDeclared: false });
}
