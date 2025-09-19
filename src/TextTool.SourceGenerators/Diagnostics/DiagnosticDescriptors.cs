using Microsoft.CodeAnalysis;
using TextTool.SourceGenerators.ComponentModel;
using TextTool.SourceGenerators.Helpers;

namespace TextTool.SourceGenerators.Diagnostics;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor StaticTargetTypeError = new(
        id: "TS001",
        title: "Invalid target type declaration",
        messageFormat: "Cannot apply attribute to '{0}', as it is a static class",
        category: typeof(PropertyComparisonProviderGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTypeArgumentError = new(
        id: "TS003",
        title: "Invalid type argument",
        messageFormat: "Cannot apply attribute to '{0}', as the type argument '{1}' is not a valid type",
        category: typeof(PropertyComparisonProviderGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GetComparisonMethodCollisionError = new(
        id: "TS005",
        title: "Interface method name collision",
        messageFormat: $"Cannot apply attribute to '{{0}}', as it already declares the '{Resources.IPropertyComparisonProviderResource.FullyQualifiedNameWithoutGlobalNamespace}<{{0}}>.{Resources.IPropertyComparisonProviderResource.NameOfGetComparison}' method",
        category: typeof(PropertyComparisonProviderGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoComparablePropertyFoundWarning = new(
        id: "TS007",
        title: "Not found comparable property",
        messageFormat: "'{0}' does not have any accessible comparable property",
        category: typeof(PropertyComparisonProviderGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
