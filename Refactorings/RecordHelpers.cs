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

    public static IEnumerable<IMethodSymbol> GetDeconstruct(ITypeSymbol record, IList<IParameterSymbol> primaryConstructorParameters)
    {
        // Get all the deconstructs with correct parameter count.
        var candidates = record.GetMembers("Deconstruct")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0 } &&
                m.Parameters.Length == primaryConstructorParameters.Count);

        // Return any deconstruct for which parameters are the same order/type/ref kind.
        foreach (var m in candidates)
        {
            if (primaryConstructorParameters.Zip(m.Parameters, (left, right) => (Left: left, Right: right))
                .All(p => SymbolEqualityComparer.Default.Equals(p.Left.Type, p.Right.Type) && p.Right.RefKind is not RefKind.None))
            {
                yield return m;
            }
        }
    }

    public static bool HasExplicitDeconstruct(ITypeSymbol record, IList<IParameterSymbol> primaryConstructorParameters) =>
        GetDeconstruct(record, primaryConstructorParameters).Any(m => m is { IsImplicitlyDeclared: false });

    public static IEnumerable<IMethodSymbol> GetEquals(ITypeSymbol record) =>
        record.GetMembers("Equals")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0, Parameters: [{ Type: var type, RefKind: RefKind.None }] } &&
                SymbolEqualityComparer.Default.Equals(type, record));

    public static bool HasExplicitEquals(ITypeSymbol record) =>
        GetEquals(record).Any(m => m is { IsImplicitlyDeclared: false });

    public static IEnumerable<IMethodSymbol> GetGetHashCode(ITypeSymbol record) =>
        record.GetMembers("GetHashCode")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0, Parameters: [] });

    public static bool HasExplicitGetHashCode(ITypeSymbol record) =>
        GetGetHashCode(record).Any(m => m is { IsImplicitlyDeclared: false });
}
