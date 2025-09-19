using Microsoft.CodeAnalysis;

namespace TextTool.SourceGenerators.Helpers;

internal static class Formats
{
    public static readonly SymbolDisplayFormat MinimallyQualified =
            SymbolDisplayFormat.MinimallyQualifiedFormat;

    public static readonly SymbolDisplayFormat MinimallyQualifiedWithoutGenerics =
        MinimallyQualified.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public static readonly SymbolDisplayFormat FullyQualified =
        SymbolDisplayFormat.FullyQualifiedFormat;

    public static readonly SymbolDisplayFormat FullyQualifiedWithoutGenerics =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public static readonly SymbolDisplayFormat FullyQualifiedWithoutGlobalNamespace =
        FullyQualified.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public static readonly SymbolDisplayFormat FullyQualifiedWithoutGlobalNamespaceAndGenerics =
        FullyQualifiedWithoutGlobalNamespace.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public static readonly SymbolDisplayFormat NameAndContainingTypesAndNamespaces = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static readonly SymbolDisplayFormat NameAndContainingTypesAndNamespacesWithoutGenerics =
        NameAndContainingTypesAndNamespaces.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public static readonly SymbolDisplayFormat InterfaceProperty = new(
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeOptionalBrackets);
}
