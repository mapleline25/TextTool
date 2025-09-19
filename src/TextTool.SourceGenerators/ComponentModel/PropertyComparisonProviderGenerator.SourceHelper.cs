using System;
using TextTool.SourceGenerators.Helpers;
using TextTool.SourceGenerators.Models;
using static TextTool.SourceGenerators.Helpers.Resources;

namespace TextTool.SourceGenerators.ComponentModel;

public partial class PropertyComparisonProviderGenerator
{
    internal static class SourceHelper
    {
        public static string GetOutputSource(TargetTypeInfo typeInfo)
        {
            using SourceBuilder builder = new();

            builder.AppendSourceHeader();
            builder.BeginHierarchyBlocks(typeInfo.Hierarchy.Namespace, typeInfo.Hierarchy.TypeHierarchy.AsSpan());

            // generate source type
            AppendTargetClass(builder, typeInfo);

            builder.BeginBlock();
            
            // generate GetComparison()
            AppendTargetClassMembers(builder, typeInfo);

            // generate inner helper class
            AppendHelperClass(builder);

            builder.BeginBlock();
                
            // generate inner helper class members
            AppendHelperClassMembers(builder, typeInfo);
            
            builder.EndAllBlocks();
            
            return builder.ToString();
        }

        private static void AppendTargetClass(SourceBuilder builder, TargetTypeInfo typeInfo)
        {
            builder.AppendLine($$"""
partial class {{typeInfo.Name}} : {{IPropertyComparisonProviderResource.FullyQualifiedName}}<{{typeInfo.ArgumentFullyQualifiedName}}>
""");
        }

        private static void AppendTargetClassMembers(SourceBuilder builder, TargetTypeInfo typeInfo)
        {
            builder.AppendDeclarationHeader();
            builder.AppendLine($$"""
public {{TypeResource.PropertyComparison}}<{{typeInfo.ArgumentFullyQualifiedName}}>? GetComparison(string propertyName)
{
    if ({{typeInfo.FullyQualifiedName}}.{{IPropertyComparisonProviderResource.NameOfComparisonHelper}}.Table.TryGetValue(propertyName, out var comparison))
    {
        return comparison;
    }
    return null;
}
""");
        }

        private static void AppendHelperClass(SourceBuilder writer)
        {
            writer.AppendDeclarationHeader();
            writer.AppendLine($$"""
private static class {{IPropertyComparisonProviderResource.NameOfComparisonHelper}}
""");
        }

        private static void AppendHelperClassMembers(SourceBuilder builder, TargetTypeInfo typeInfo)
        {
            builder.AppendLine($$"""
internal static readonly {{TypeResource.Dictionary}}<string, {{TypeResource.PropertyComparison}}<{{typeInfo.ArgumentFullyQualifiedName}}>> Table = new()
{
""");

            ReadOnlySpan<PropertyInfo> properties = typeInfo.Properties.AsSpan();
            for (int i = 0; i < properties.Length; i++)
            {
                string propertyName = properties[i].Name;
                builder.AppendLine($$"""
    {"{{propertyName}}", Compare{{propertyName}} },
""");
            }

            builder.AppendLine($$"""
};
""");

            string nullableGetValue = typeInfo.IsNullableValueType ? "Value." : string.Empty;

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                PropertyCompareInfo info = property.Info;
                string propertyName = property.Name;

                builder.AppendDeclarationHeader();
                builder.AppendLine($$"""
internal static int Compare{{propertyName}}({{typeInfo.ArgumentFullyQualifiedName}} x, {{typeInfo.ArgumentFullyQualifiedName}} y, {{TypeResource.CompareInfo}} compareInfo)
{
""");

                if (property.TypeKind == PropertyTypeKind.ValueType)
                {
                    builder.AppendLine($$"""
    var a = x.{{nullableGetValue}}{{propertyName}};
    var b = y.{{nullableGetValue}}{{propertyName}};
""");
                }
                else if (property.TypeKind == PropertyTypeKind.NullableValueType)
                {
                    builder.AppendLine($$"""
    var na = x.{{nullableGetValue}}{{propertyName}};
    var nb = y.{{nullableGetValue}}{{propertyName}};
    if (na == null) return nb == null ? 0 : -1;
    if (nb == null) return 1;
    var a = na.Value;
    var b = nb.Value;
""");
                }
                else
                {
                    builder.AppendLine($$"""
    var a = x.{{nullableGetValue}}{{propertyName}};
    var b = y.{{nullableGetValue}}{{propertyName}};
    if (a == null) return b == null ? 0 : -1;
    if (b == null) return 1;
""");
                }

                if (info.CompareKind == CompareKind.IComparisonOperators)
                {
                    builder.AppendLine($$"""
    return a < b ? -1 : (a > b ? 1 : 0);
}
""");
                }
                else if (info.CompareKind == CompareKind.Boolean)
                {
                    builder.AppendLine($$"""
    return a == b ? 0 : (!a ? -1 : 1);
}
""");
                }
                else if (info.CompareKind == CompareKind.String)
                {
                    builder.AppendLine($$"""
    return compareInfo.Compare(a.AsSpan(), b.AsSpan());
}
""");
                }
                else if (info.CompareKind == CompareKind.IComparable)
                {
                    builder.AppendLine($$"""
    return a.CompareTo(b);
}
""");
                }
                else if (info.CompareKind == CompareKind.IComparable_T)
                {
                    builder.AppendLine($$"""
    return a.CompareTo(({{info.TypeArguments.AsSpan()[0]}})b);
}
""");
                }
                else // TypeParameter with CompareKind.Unknown
                {
                    string argument = info.TypeArguments.AsSpan()[0];
                    builder.AppendLine($$"""
    if (a is string sa && b is string sb) return compareInfo.Compare(sa.AsSpan(), sb.AsSpan());
    if (a is {{TypeResource.IComparable}}<{{argument}}> ta) return ta.CompareTo(({{argument}})b);
    if (a is {{TypeResource.IComparable}} ia) return ia.CompareTo(b);
    throw new {{TypeResource.NotImplementedException}}("{{propertyName}}");
}
""");
                }
            }
        }
    }
}
