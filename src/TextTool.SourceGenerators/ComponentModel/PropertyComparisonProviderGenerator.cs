using CommunityToolkit.Mvvm.SourceGenerators.Helpers;
using CommunityToolkit.Mvvm.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StronglyTypedIds;
using TextTool.SourceGenerators.Helpers;
using TextTool.SourceGenerators.Models;

namespace TextTool.SourceGenerators.ComponentModel;

[Generator]
public partial class PropertyComparisonProviderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<TargetTypeInfo?>> resultProvider =
            context.SyntaxProvider.ForAttributeWithMetadataName(
                Resources.PropertyComparisonProviderAttributeResource.FullyQualifiedMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: Execute.GetResult)
            .Where(static (result) => result != null)!;

        // filter out the errors
        IncrementalValuesProvider<EquatableArray<DiagnosticInfo>> errorProvider = 
            resultProvider
            .Select(static (result, _) => result.Errors);

        context.RegisterSourceOutput(errorProvider, static (productionContext, errors) =>
        {
            for (int i = 0; i < errors.Length; i++)
            {
                productionContext.ReportDiagnostic(errors[i].ToDiagnostic());
            }
        });

        // filter out the non-null TargetTypeInfo
        IncrementalValuesProvider<TargetTypeInfo> typeInfoProvider = 
            resultProvider
            .Where(static result => result.Value != null)
            .Select(static (result, _) => result.Value!);

        context.RegisterSourceOutput(typeInfoProvider, static (productionContext, typeInfo) =>
        {
            productionContext.AddSource($"{typeInfo.FullyMetadataName}.g.cs", SourceHelper.GetOutputSource(typeInfo));
        });
    }
}
