using CommunityToolkit.Mvvm.SourceGenerators.Helpers;

namespace TextTool.SourceGenerators.Models;

internal record TargetTypeInfo(
    string Name,
    string ConstructorName,
    string FullyQualifiedName,
    string FullyMetadataName,
    bool IsNullableValueType,
    HierarchyInfo Hierarchy,
    string ArgumentFullyQualifiedName,
    EquatableArray<PropertyInfo> Properties
    )
{
    public static readonly TargetTypeInfo Default = new(
        default!, default!, default!, default!, default!, HierarchyInfo.Default, default!, EquatableArray<PropertyInfo>.Empty);
}
