// This file is licensed under the MIT license.
// See the "LICENSE" file for more details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Webczat.RecordBooster.Refactorings;

/// <summary>
/// This refactoring generates <c>Equals</c> and <c>GetHashCode</c> methods equivalent to implicitly declared ones, if not already present.
/// </summary>
/// <param name="context">The refactoring context.</param>
/// <param name="syntaxRoot">The refactored document's syntax root.</param>
/// <param name="semanticModel">The refactored document's semantic model.</param>
public sealed class EqualsAndGetHashCodeRefactoring(CodeRefactoringContext context, SyntaxNode syntaxRoot, SemanticModel semanticModel) :
CodeRefactoring(context, syntaxRoot, semanticModel)
{
    /// <summary>
    /// The name of Equals method.
    /// </summary>
    public const string EqualsMethod = "Equals";

    /// <summary>
    /// The name of GetHashCode method.
    /// </summary>
    public const string GetHashCodeMethod = "GetHashCode";

    // A list of builtin c# types.
    private static readonly SpecialType[] BuiltinTypes = [
    SpecialType.System_Object,
        SpecialType.System_String,
        SpecialType.System_Char,
        SpecialType.System_Boolean,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Byte,
        SpecialType.System_SByte,
        SpecialType.System_IntPtr,
        SpecialType.System_UIntPtr,
    ];

    // List of namespaces to import.
    private readonly IList<string> _namespacesToImport = [];

    // Depth of indentation used for manual formatting routines, defaults to 2 (meaning two tab stops).
    private int _formatDepth = 2;

    // Symbol representing System.HashCode struct, if any.
    private ITypeSymbol? _hashCodeSymbol;

    // The symbol representing implicit or explicit EqualityContract property, if any.
    private ISymbol? _equalityContractSymbol;

    // Members that will be compared.
    private IList<ISymbol> _comparableMembers = [];

    /// <inheritdoc/>
    public override string Key => "EqualsAndGetHashCode";

    /// <inheritdoc/>
    public override string Title => "Generate default record \"Equals\" and \"GetHashCode\"";

    /// <inheritdoc/>
    protected async override Task<bool> PrepareAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken)
    {
        _hashCodeSymbol = SemanticModel.Compilation.GetTypeByMetadataName("System.HashCode");
        _equalityContractSymbol = originalRecordSymbol.GetMembers("EqualityContract").SingleOrDefault(m => m is IPropertySymbol);
        if (_equalityContractSymbol is null && !originalRecordSymbol.IsValueType)
        {
            return false;
        }

        // We try to compare actual object data, so take all instance fields irrespective of accessibility, but not properties.
        // Then because we can't access auto props directly via fields, take respective properties in their place.
        _comparableMembers = originalRecordSymbol.GetMembers()
            .Where(m => m is IFieldSymbol { IsStatic: false })
            .Select(m => m is IFieldSymbol { AssociatedSymbol: not null } f ? f.AssociatedSymbol : m)
            .ToList();

        // A positional record hack: if record doesn't have braces, manually formatted expressions will be misindented by default.
        // So in that case, increase the expected indentation level by 1.
        if (originalRecord.SemicolonToken != default)
        {
            _formatDepth = 3;
        }

        return !RecordHelpers.HasExplicitEquals(originalRecordSymbol) && !RecordHelpers.HasExplicitGetHashCode(originalRecordSymbol);
    }

    /// <inheritdoc/>
    protected async override Task<Document> ExecuteAsync(RecordDeclarationSyntax originalRecord, ITypeSymbol originalRecordSymbol, CancellationToken cancellationToken)
    {
        var document = Context.Document;
        var generator = SyntaxGenerator.GetGenerator(document);
        var inherited = originalRecordSymbol.BaseType?.IsRecord ?? false;

        var equals = GenerateEquals(generator, originalRecordSymbol, inherited);

        var getHashCode = GenerateGetHashCode(generator, originalRecordSymbol, inherited);

        var newRecord = generator.AddMembers(originalRecord, equals, getHashCode);

        var newRoot = AddImports(generator, SyntaxRoot.ReplaceNode(originalRecord, newRecord), originalRecord, cancellationToken);
        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument;
    }

    // Retrieves syntax for member access to a given instantiation of EqualityComparer<T>.
    private static SyntaxNode GetEqualityComparerFor(SyntaxGenerator generator, SyntaxNode typeArgument) =>
        generator.MemberAccessExpression(
            generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("System"), "Collections"), "Generic"),
            generator.GenericName("EqualityComparer", [typeArgument]));

    // Adds namespace to list of to be imported namespaces if it doesn't already exist.
    private void AddNamespace(string @namespace)
    {
        if (!_namespacesToImport.Contains(@namespace))
        {
            _namespacesToImport.Add(@namespace);
        }
    }

    // Generates and returns the Equals method.
    private SyntaxNode GenerateEquals(SyntaxGenerator generator, ITypeSymbol originalRecordSymbol, bool inherited)
    {
        // Construct a list of expressions that constitute the equality expression.
        SyntaxList<SyntaxNode> equalityExpressions = [];

        // If not a value type and not inherited, add the not null check.
        if (!originalRecordSymbol.IsValueType && !inherited)
        {
            equalityExpressions = equalityExpressions.Add(SyntaxFactory.IsPatternExpression(
                SyntaxFactory.IdentifierName("other"),
                SyntaxFactory.UnaryPattern(SyntaxFactory.ConstantPattern((ExpressionSyntax)generator.NullLiteralExpression()))));
        }

        if (inherited)
        {
            // If we inherit from another record, call base Equals.
            equalityExpressions = equalityExpressions.Add(generator.InvocationExpression(
                generator.MemberAccessExpression(generator.BaseExpression(), "Equals"),
                [generator.IdentifierName("other")]));
        }
        else if (!originalRecordSymbol.IsValueType)
        {
            // If neither inherited nor value type, compare the "EqualityContract".
            equalityExpressions = equalityExpressions.Add(generator.ValueEqualsExpression(
                generator.MemberAccessExpression(generator.ThisExpression(), generator.IdentifierName(_equalityContractSymbol!.Name)),
                generator.MemberAccessExpression(generator.IdentifierName("other"), generator.IdentifierName(_equalityContractSymbol.Name))));
        }

        // Iterate through all the members to compare.
        bool usesEC = false;
        foreach (var f in _comparableMembers)
        {
            var type = (f as IFieldSymbol)?.Type ?? ((IPropertySymbol)f).Type;

            // Builtin types, enums and types overriding the equality operators will be compared using ==.
            // Note it's an intentional deviation from spec for readability purposes.
            if (BuiltinTypes.Contains(type.SpecialType) || type.TypeKind is TypeKind.Enum |
                type.GetMembers(WellKnownMemberNames.EqualityOperatorName)
                .Any(m => m is IMethodSymbol
                { MethodKind: MethodKind.UserDefinedOperator or MethodKind.BuiltinOperator, Parameters: [{ Type: var t1 }, { Type: var t2 }] } &&
                    SymbolEqualityComparer.Default.Equals(t1, t2) &&
                    SymbolEqualityComparer.Default.Equals(t1, type)))
            {
                equalityExpressions = equalityExpressions.Add(generator.ValueEqualsExpression(
                    generator.MemberAccessExpression(generator.ThisExpression(), f.Name),
                    generator.MemberAccessExpression(generator.IdentifierName("other"), f.Name)));
            }
            else
            {
                // Other types might be comparable using overrides of Equals(object) or IEquatable<T> so use EC.Default.
                usesEC = true;
                equalityExpressions = equalityExpressions.Add(generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.MemberAccessExpression(
                            GetEqualityComparerFor(generator, generator.TypeExpression(type, false)),
                            "Default"),
                        "Equals"),
                    [generator.MemberAccessExpression(generator.ThisExpression(), f.Name),
                    generator.MemberAccessExpression(generator.IdentifierName("other"), f.Name)]));
            }
        }

        // If EqualityComparer has been used at least once, import System.Collections.Generic namespace.
        if (usesEC)
        {
            AddNamespace("System.Collections.Generic");
        }

        // Create the equality expression by reducing constituent equality expressions. If there are none, expression is just "true".
        SyntaxNode equalityExpression = equalityExpressions.Count == 0 ?
            generator.TrueLiteralExpression() :
            equalityExpressions.Aggregate((left, right) =>
                FormatterUtils.FormatBinaryExpression(
                    (BinaryExpressionSyntax)generator.LogicalAndExpression(left, right),
                    _formatDepth));

        // Create equals method.
        var originalRecordType = generator.TypeExpression(originalRecordSymbol);
        var modifiers = originalRecordSymbol.IsValueType ?
            DeclarationModifiers.ReadOnly :
            originalRecordSymbol.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;
        var equals = generator.MethodDeclaration(
            EqualsMethod,
            accessibility: Accessibility.Public,
            modifiers: modifiers,
            returnType: generator.TypeExpression(SpecialType.System_Boolean),
            parameters: [
                generator.ParameterDeclaration(
                    "other",
                    originalRecordSymbol.IsValueType ? originalRecordType : generator.NullableTypeExpression(originalRecordType))
                    ],
            statements: [
                generator.ReturnStatement(equalityExpression),
            ])
        .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        return equals;
    }

    // Generates and returns the GetHashCode method.
    private SyntaxNode GenerateGetHashCode(SyntaxGenerator generator, ITypeSymbol originalRecordSymbol, bool inherited)
    {
        // Create a list of hash code expressions.
        // They will be turned into contents of GetHashCode later.
        SyntaxList<SyntaxNode> hashCodeExpressions = [];

        if (inherited)
        {
            // If record inherits from another record,, call up to GetHashCode of the base.
            hashCodeExpressions = hashCodeExpressions.Add(generator.InvocationExpression(
                generator.MemberAccessExpression(generator.BaseExpression(), "GetHashCode")));
        }
        else if (!originalRecordSymbol.IsValueType)
        {
            // If record doesn't inherit from anything and it's not a value type, hash EqualityContract.
            hashCodeExpressions = hashCodeExpressions.Add(
                generator.MemberAccessExpression(generator.ThisExpression(), _equalityContractSymbol!.Name));
        }

        // Add expressions for other members.
        foreach (var m in _comparableMembers)
        {
            hashCodeExpressions = hashCodeExpressions.Add(
                generator.MemberAccessExpression(generator.ThisExpression(), m.Name));
        }

        // Turn expressions into statements.
        SyntaxList<SyntaxNode> hashCodeStatements = [];
        if (hashCodeExpressions.Count == 0)
        {
            // If there are no hash code expressions in the list, just make the method return 0.
            hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(generator.LiteralExpression(0)));
        }
        else if (inherited && hashCodeExpressions.Count == 1)
        {
            // Special case, if we inherit from base record and have no members, we can just return the only expression directly as it calls base gethashcode.
            hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(hashCodeExpressions[0]));
        }
        else if (_hashCodeSymbol is not null)
        {
            // Add "System" to imported namespaces when System.HashCode is present.
            _namespacesToImport.Add("System");

            if (hashCodeExpressions.Count > 8)
            {
                // If there are more than 8 hash code expressions and System.HashCode is available, we need to turn them into HashCode.Add calls.
                hashCodeStatements = hashCodeStatements.Add(generator.LocalDeclarationStatement(
                    generator.TypeExpression(_hashCodeSymbol),
                    "h",
                    generator.DefaultExpression(generator.TypeExpression(_hashCodeSymbol))));

                foreach (var e in hashCodeExpressions)
                {
                    hashCodeStatements = hashCodeStatements.Add(generator.InvocationExpression(
                        generator.MemberAccessExpression(generator.IdentifierName("h"), "Add"),
                        [e]));
                }

                hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.IdentifierName("h"), "ToHashCode"), [])));
            }
            else
            {
                // If System.HashCode is available and we have less than 8 hash code expressions, turn them into an argument list and invoke HashCode.Combine.
                var combine = (InvocationExpressionSyntax)generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.MemberAccessExpression(generator.IdentifierName("System"), "HashCode"),
                        "Combine"),
                    hashCodeExpressions);
                hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(combine
                    .WithArgumentList(FormatterUtils.FormatFunctionArgumentList(combine.ArgumentList, _formatDepth))));
            }
        }
        else
        {
            if (hashCodeExpressions is [SyntaxNode e])
            {
                // If System.HashCode not available and we have only one expression...
                hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(e, "GetHashCode"), [])));
            }
            else
            {
                // If System.HashCode not available and there are more than one hash code expressions, just make a tuple and call GetHashCode on it.
                var tuple = (TupleExpressionSyntax)generator.TupleExpression(hashCodeExpressions);
                hashCodeStatements = hashCodeStatements.Add(generator.ReturnStatement(generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        FormatterUtils.FormatTupleExpression(tuple, _formatDepth),
                        "GetHashCode"))));
            }
        }

        // Create the GetHashCode method.
        var modifiers = originalRecordSymbol.IsValueType ?
            DeclarationModifiers.Override | DeclarationModifiers.ReadOnly :
            DeclarationModifiers.Override;
        var getHashCode = generator.MethodDeclaration(
    GetHashCodeMethod,
    accessibility: Accessibility.Public,
    modifiers: modifiers,
    returnType: generator.TypeExpression(SpecialType.System_Int32),
    parameters: [],
    statements: hashCodeStatements)
    .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, Formatter.Annotation);

        return getHashCode;
    }

    // Adds given namespace imports if imports not already present.
    // Useful for namespace imports that are not added by AddImports annotation.
    private SyntaxNode AddImports(SyntaxGenerator generator, SyntaxNode root, SyntaxNode record, CancellationToken cancellationToken)
    {
        // Note that we need to use root parameter instead of SyntaxRoot as it's the updated root.
        // Retrieve namespace imports in scope for the record declaration, then flatten and dedupe them.
        var currentNamespaceImports = SemanticModel.GetImportScopes(record.Span.Start, cancellationToken)
            .SelectMany(scope => scope.Imports)
            .Select(i => i.NamespaceOrType)
            .Distinct(SymbolEqualityComparer.Default)
            .OfType<INamespaceSymbol>()
            .Select(ns => ns.ToDisplayString())
            .ToList();

        SyntaxList<SyntaxNode> importsToAdd = [];
        foreach (var ns in _namespacesToImport.OrderBy(s => s))
        {
            if (!currentNamespaceImports.Contains(ns))
            {
                importsToAdd = importsToAdd.Add(generator.DottedName(ns));
            }
        }

        return generator.AddNamespaceImports(root, importsToAdd);
    }
}
