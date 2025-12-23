// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;

namespace Webczat.RecordBooster.Refactorings;

/// <summary>
/// Contains numerous helpers used to find record specific members.
/// </summary>
public static class RecordHelpers
{
    /// <summary>
    /// Retrieves all <c>ToString</c> methods declared for the record.
    /// </summary>
    /// <param name="record">Record to introspect.</param>
    /// <returns>Enumerable of all <C>ToString</c> members.</returns>
    public static IEnumerable<IMethodSymbol> GetToString(ITypeSymbol record) =>
        record.GetMembers("ToString")
            .OfType<IMethodSymbol>()
            .Where(s => s is { Parameters: [], Arity: 0 });

    /// <summary>
    /// Checks whether given record has an explicitly declared <c>ToString</c> method.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <returns><c>true</c> if record has an explicit <c>ToString</c>, <c>false</c> othervise.</returns>
    public static bool HasExplicitToString(ITypeSymbol record) =>
        GetToString(record).Any(s => s is { IsImplicitlyDeclared: false });

    /// <summary>
    /// Gets the record's declared <c>PrintMembers</c> methods.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <param name="stringBuilderSymbol">The <see cref="StringBuilder"/> symbol .</param>
    /// <returns>The enumerable of all <c>PrintMember</c> methods.</returns>
    public static IEnumerable<IMethodSymbol> GetPrintMembers(ITypeSymbol record, ISymbol stringBuilderSymbol) =>
        record.GetMembers("PrintMembers")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0, Parameters: [{ Type: var type, RefKind: RefKind.None }] }
            && SymbolEqualityComparer.Default.Equals(type, stringBuilderSymbol));

    /// <summary>
    /// Checks if the given record has an explicit <c>PrintMembers</c> method.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <param name="stringBuilderSymbol">The <see cref="StringBuilder"/> symbol .</param>
    /// <returns><c>true</c> if the record has an explicit <c>PrintMembers</c>, <c>false</c> othervise.</returns>
    public static bool HasExplicitPrintMembers(ITypeSymbol record, ISymbol stringBuilderSymbol) =>
        GetPrintMembers(record, stringBuilderSymbol).Any(m => m is { IsImplicitlyDeclared: false });

    /// <summary>
    /// Gets all the deconstruct methods declared in the given record, matching given parameters.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <param name="expectedParameters">The parameters to match.</param>
    /// <returns>Enumerable of the matching record's <c>Deconstruct</c> methods.</returns>
    public static IEnumerable<IMethodSymbol> GetDeconstruct(ITypeSymbol record, IList<IParameterSymbol> expectedParameters)
    {
        // Get all the deconstructs with correct parameter count.
        var candidates = record.GetMembers("Deconstruct")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0 } &&
                m.Parameters.Length == expectedParameters.Count);

        // Return any deconstruct for which parameters are the same order/type/ref kind.
        foreach (var m in candidates)
        {
            if (expectedParameters.Zip(m.Parameters, (left, right) => (Left: left, Right: right))
                .All(p => SymbolEqualityComparer.Default.Equals(p.Left.Type, p.Right.Type) && p.Right.RefKind is not RefKind.None))
            {
                yield return m;
            }
        }
    }

    /// <summary>
    /// Checks whether the given record has an explicitly declared <c>Deconstruct</c> method.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <param name="expectedParameters">The parameters to match.</param>
    /// <returns><c>true</c> if the record has an explicit <c>Deconstruct</c>, <c>false</c> othervise.</returns>
    public static bool HasExplicitDeconstruct(ITypeSymbol record, IList<IParameterSymbol> primaryConstructorParameters) =>
        GetDeconstruct(record, primaryConstructorParameters).Any(m => m is { IsImplicitlyDeclared: false });

    /// <summary>
    ///  Gets the record's declared <c>Equals</c> methods.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <returns>Enumerable of record's <c>Equals</c> methods.</returns>
    public static IEnumerable<IMethodSymbol> GetEquals(ITypeSymbol record) =>
        record.GetMembers("Equals")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0, Parameters: [{ Type: var type, RefKind: RefKind.None }] } &&
                SymbolEqualityComparer.Default.Equals(type, record));

    /// <summary>
    /// Checks if the given record has an explicit <c>Equals</c> method.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <returns><c>true</c> if the record has an explicit <c>Equals</c>, <c>false</c> othervise.</returns>
    public static bool HasExplicitEquals(ITypeSymbol record) =>
        GetEquals(record).Any(m => m is { IsImplicitlyDeclared: false });

    /// <summary>
    /// Gets the record's declared <c>GetHashCode</c> methods.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <returns>Enumerable of the record's <c>GetHashCode</c> methods.</returns>
    public static IEnumerable<IMethodSymbol> GetGetHashCode(ITypeSymbol record) =>
        record.GetMembers("GetHashCode")
            .OfType<IMethodSymbol>()
            .Where(m => m is { Arity: 0, Parameters: [] });

    /// <summary>
    /// Checks if the given record has an explicit <c>GetHashCode</c> method.
    /// </summary>
    /// <param name="record">The record to introspect.</param>
    /// <returns><c>true</c> if the record has an explicit <c>GetHashCode</c>, <c>false</c> othervise.</returns>
    public static bool HasExplicitGetHashCode(ITypeSymbol record) =>
        GetGetHashCode(record).Any(m => m is { IsImplicitlyDeclared: false });
}
