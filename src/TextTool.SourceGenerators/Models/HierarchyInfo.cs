using CommunityToolkit.Mvvm.SourceGenerators.Helpers;

namespace TextTool.SourceGenerators.Models;

internal record HierarchyInfo(string Namespace, EquatableArray<HierarchyTypeInfo> TypeHierarchy)
{
    public static readonly HierarchyInfo Default = new(string.Empty, EquatableArray<HierarchyTypeInfo>.Empty);
}
