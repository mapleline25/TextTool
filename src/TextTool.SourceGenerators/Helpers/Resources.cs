using Microsoft.CodeAnalysis.CSharp;

namespace TextTool.SourceGenerators.Helpers;

internal static class Resources
{
    internal static class SystemResource
    {
        internal static readonly string NewLine = SyntaxFactory.ElasticCarriageReturnLineFeed.ToString();
    }
    
    internal static class PropertyComparisonProviderAttributeResource
    {
        internal const string Name = "PropertyComparisonProviderAttribute";
        internal const string FullyQualifiedName = "global::TextTool.SourceGenerators.Attributes.PropertyComparisonProviderAttribute";
        internal const string FullyQualifiedMetadataName = "TextTool.SourceGenerators.Attributes.PropertyComparisonProviderAttribute";
        internal const string FullyQualifiedNameWithoutGlobalNamespace = "TextTool.SourceGenerators.Attributes.PropertyComparisonProviderAttribute";
    }

    internal static class IPropertyComparisonProviderResource
    {
        internal const string Name = "IPropertyComparisonProvider";
        internal const string FullyQualifiedName = "global::TextTool.Library.ComponentModel.IPropertyComparisonProvider";
        internal const string FullyQualifiedMetadataName = "TextTool.Library.ComponentModel.IPropertyComparisonProvider";
        internal const string FullyQualifiedNameWithoutGlobalNamespace = "TextTool.Library.ComponentModel.IPropertyComparisonProvider";
        internal const string NameOfGetComparison = "GetComparison";
        internal const string NameOfComparisonHelper = "ComparisonHelper";
    }

    internal static class HeaderAttributeResource
    {
        internal const string GeneratedCodeAttributeContent = """[global::System.CodeDom.Compiler.GeneratedCode("TextTool.SourceGenerators", "1.0.0")]""";
        internal const string DebuggerNonUserCodeAttributeContent = "[global::System.Diagnostics.DebuggerNonUserCode]";
        internal const string ExcludeFromCodeCoverageAttributeContent = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";
    }

    internal static class TypeResource
    {
        internal const string PropertyComparison = "global::TextTool.Library.ComponentModel.PropertyComparison";
        internal const string Dictionary = "global::System.Collections.Generic.Dictionary";
        internal const string Type = "global::System.Type";
        internal const string ModuleInitializer = "global::System.Runtime.CompilerServices.ModuleInitializer";
        internal const string IComparisonOperators = "IComparisonOperators`3";
        internal const string IComparable = "global::System.IComparable";
        internal const string CompareInfo = "global::System.Globalization.CompareInfo";
        internal const string ArgumentOutOfRangeException = "global::System.ArgumentOutOfRangeException";
        internal const string NotImplementedException = "global::System.NotImplementedException";
        internal const string MemberAccessException = "global::System.MemberAccessException";
        internal const string MissingMemberException = "global::System.MissingMemberException";
    }
}
