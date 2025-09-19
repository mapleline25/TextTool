using CommunityToolkit.Mvvm.SourceGenerators.Helpers;

namespace TextTool.SourceGenerators.Models;

internal record PropertyCompareInfo(CompareKind CompareKind, EquatableArray<string> TypeArguments)
{
    public static readonly PropertyCompareInfo Default = new(CompareKind.Unknown, EquatableArray<string>.Empty);
    public static readonly PropertyCompareInfo BooleanInfo = new(CompareKind.Boolean, EquatableArray<string>.Empty);
    public static readonly PropertyCompareInfo StringInfo = new(CompareKind.String, EquatableArray<string>.Empty);
    public static readonly PropertyCompareInfo IComparableInfo = new(CompareKind.IComparable, EquatableArray<string>.Empty);
    public static readonly PropertyCompareInfo IComparable_TInfo = new(CompareKind.IComparable_T, EquatableArray<string>.Empty);
    public static readonly PropertyCompareInfo IComparisonOperatorsInfo = new(CompareKind.IComparisonOperators, EquatableArray<string>.Empty);
}
