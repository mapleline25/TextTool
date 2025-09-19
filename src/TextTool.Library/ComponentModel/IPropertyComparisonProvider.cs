using System.Globalization;

namespace TextTool.Library.ComponentModel;

public delegate int PropertyComparison<in T>(T x, T y, CompareInfo compareInfo);

public interface IPropertyComparisonProvider<T>
{
    public PropertyComparison<T>? GetComparison(string propertyName);
}
