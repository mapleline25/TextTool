using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Threading;
using CommunityToolkit.Mvvm.SourceGenerators.Models;
using TextTool.SourceGenerators.Extensions;
using TextTool.SourceGenerators.Models;
using static TextTool.SourceGenerators.Helpers.Resources;
using StronglyTypedIds;
using TextTool.SourceGenerators.Diagnostics;
using TextTool.SourceGenerators.Helpers;
using CommunityToolkit.Mvvm.SourceGenerators.Helpers;

namespace TextTool.SourceGenerators.ComponentModel;

public partial class PropertyComparisonProviderGenerator
{
    internal static class Execute
    {
        public static Result<TargetTypeInfo?>? GetResult(GeneratorAttributeSyntaxContext context, CancellationToken ct)
        {
            TypeDeclarationSyntax targetNode = (TypeDeclarationSyntax)context.TargetNode;
            INamedTypeSymbol targetType = (INamedTypeSymbol)context.TargetSymbol;
            
            ct.ThrowIfCancellationRequested();

            // check target type:
            // 1. no error
            // 2. no duplicate attributes
            // 3. argument has no error
            // 4. must be public/internal when it is defined in a namespace
            if (targetNode.ContainsDiagnostics || context.Attributes.Length > 1 || !targetNode.HasValidAccessibility())
            {
                return null;
            }

            if (GetIPropertyComparisonProviderInterface(targetType) is not INamedTypeSymbol targetInterface)
            {
                return null;
            }

            ImmutableArray<ITypeSymbol> typeArguments = targetInterface.TypeArguments;
            if (typeArguments.IsEmpty)
            {
                return null;
            }

            using ArrayBufferWriter<DiagnosticInfo> errors = new();
            using TypeDeclarationSyntaxInfo targetNodeInfo = new(targetNode);

            // check target type is not static
            if (targetType.IsStatic)
            {
                errors.Write((DiagnosticInfo)new(
                    DiagnosticDescriptors.StaticTargetTypeError, targetNodeInfo.Location, [targetNodeInfo.IdentifierWithTypeParameters]));
            }

            ITypeSymbol candidateTypeArgument = typeArguments[0];

            // check type argument is substituded with <T> or Nullable<T>
            INamedTypeSymbol? typeArgument = ValidateTypeArgument(candidateTypeArgument, out INamedTypeSymbol? underlyingType);
            INamedTypeSymbol type = typeArgument;
            bool isNullableValueType = false;

            if (typeArgument == null)
            {
                errors.Write((DiagnosticInfo)new(
                    DiagnosticDescriptors.InvalidTypeArgumentError, targetNodeInfo.Location, [targetNodeInfo.IdentifierWithTypeParameters, candidateTypeArgument.Name]));
            }
            else if (underlyingType != null)
            {
                isNullableValueType = true;
                type = underlyingType;
            }

            // check target type does not have GetComparison(parameter)
            if (HasPropertySortComparerMethod(targetType, ct))
            {
                errors.Write((DiagnosticInfo)new(
                    DiagnosticDescriptors.GetComparisonMethodCollisionError, targetNodeInfo.Location, [targetNodeInfo.IdentifierWithTypeParameters]));
            }
            
            if (errors.WrittenCount > 0)
            {
                // if there is any error, skip computing hierarchy/property info and return an empty result with errors
                return new(null, new(errors.WrittenSpan.ToImmutableArray()));
            }

            if (!TryGetHierarchyInfo(targetNode, targetType, ct, out HierarchyInfo hierarchy))
            {
                return null;
            }

            if (!TryGetProperties(type!, ct, out ImmutableArray<PropertyInfo> properties))
            {
                return null;
            }

            if (properties.Length == 0)
            {
                errors.Write((DiagnosticInfo)new(
                    DiagnosticDescriptors.NoComparablePropertyFoundWarning, targetNodeInfo.Location, [targetNodeInfo.IdentifierWithTypeParameters]));
            }

            // create result
            TargetTypeInfo typeInfo = new(
                targetType.ToDisplayString(Formats.MinimallyQualified),
                targetType.ToDisplayString(Formats.MinimallyQualifiedWithoutGenerics),
                targetType.ToDisplayString(Formats.FullyQualified),
                targetType.GetFullyMetadataName(),
                isNullableValueType,
                hierarchy,
                typeArgument!.ToDisplayString(Formats.FullyQualified),
                properties);

            return new(typeInfo, errors.WrittenSpan.ToImmutableArray());
        }

        private static INamedTypeSymbol? GetIPropertyComparisonProviderInterface(ITypeSymbol targetSymbol)
        {
            ReadOnlySpan<char> IPropertyComparisonProviderName = IPropertyComparisonProviderResource.FullyQualifiedName;

            ImmutableArray<INamedTypeSymbol> baseInterfaces = targetSymbol.AllInterfaces;
            for (int i = 0; i < baseInterfaces.Length; i++)
            {
                INamedTypeSymbol baseInterface = baseInterfaces[i];
                if (baseInterface.IsGenericType &&
                    IPropertyComparisonProviderName.SequenceEqual(baseInterface.ToDisplayString(Formats.FullyQualifiedWithoutGenerics)))
                {
                    return baseInterface;
                }
            }
            return null;
        }

        private static INamedTypeSymbol? ValidateTypeArgument(ITypeSymbol typeArgument, out INamedTypeSymbol? underlyingType)
        {
            underlyingType = null;

            if (typeArgument is not INamedTypeSymbol namedType)
            {
                return null;
            }

            if (namedType.GetNullableUnderlyingType() is ITypeSymbol type)
            {
                underlyingType = type as INamedTypeSymbol;

                // type argument is valid Nullable<T>
                if (underlyingType != null && underlyingType.IsValueType)
                {
                    return namedType;
                }

                // underlying type is not valid value type
                return null;
            }

            // type argument is valid <T>
            return namedType;
        }

        // check method: GetComparison(parameter)
        private static bool HasPropertySortComparerMethod(INamedTypeSymbol targetSymbol, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            // check target type methods with all accessibility
            Accessibility accessibility = Accessibility.NotApplicable;
            if (targetSymbol.HasMemberOrMethod(IPropertyComparisonProviderResource.NameOfGetComparison, 1, accessibility))
            {
                return true;
            }

            // check base type methods with accessibility higher than private
            accessibility = Accessibility.ProtectedAndInternal;
            targetSymbol = targetSymbol.BaseType;
            while (targetSymbol != null)
            {
                ct.ThrowIfCancellationRequested();

                if (targetSymbol.HasMemberOrMethod(IPropertyComparisonProviderResource.NameOfGetComparison, 1, accessibility))
                {
                    return true;
                }

                targetSymbol = targetSymbol.BaseType;
            }

            return false;
        }

        private static bool TryGetHierarchyInfo(
            TypeDeclarationSyntax targetNode, INamedTypeSymbol targetType, CancellationToken ct, out HierarchyInfo info)
        {
            using ArrayBufferWriter<HierarchyTypeInfo> hierarchy = new();
            info = HierarchyInfo.Default;

            // check all containing types
            INamedTypeSymbol? currentType = targetType.ContainingType;
            TypeDeclarationSyntax? currentNode = targetNode.FirstAncestor<TypeDeclarationSyntax>();
            while (currentType != null && currentNode != null && currentType.Name.AsSpan().SequenceEqual(currentNode.Identifier.ValueText))
            {
                ct.ThrowIfCancellationRequested();

                if (currentNode.ContainsDiagnostics || !currentNode.HasValidAccessibility())
                {
                    return false;
                }

                hierarchy.Write((HierarchyTypeInfo)new(
                    currentType.ToDisplayString(Formats.MinimallyQualified),
                    currentNode.Keyword.Kind()));

                currentType = currentType.ContainingType;
                currentNode = currentNode.FirstAncestor<TypeDeclarationSyntax>();
            }

            // check all containing namespaces
            NamespaceDeclarationSyntax? namespaceNode = currentNode?.FirstAncestor<NamespaceDeclarationSyntax>();
            while (namespaceNode != null)
            {
                if (namespaceNode.ContainsDiagnostics)
                {
                    return false;
                }
                namespaceNode = namespaceNode?.FirstAncestor<NamespaceDeclarationSyntax>();
            }

            INamespaceSymbol nameSpaceSymbol = targetType.ContainingNamespace;
            
            string nameSpace = nameSpaceSymbol.IsGlobalNamespace
                    ? string.Empty
                    : nameSpaceSymbol.ToDisplayString(Formats.NameAndContainingTypesAndNamespaces);

            info = new(nameSpace, new(hierarchy.WrittenSpan.ToImmutableArray()));
            
            return true;
        }

        private static bool TryGetProperties(INamedTypeSymbol targetSymbol, CancellationToken ct, out ImmutableArray<PropertyInfo> properties)
        {
            properties = [];

            // If targetSymbol is Nullable<T>, use T as the targetSymbol to search properties
            if (targetSymbol.IsValueType && targetSymbol.IsGenericType 
                && targetSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                if (targetSymbol.TypeArguments[0] is INamedTypeSymbol underlyingType)
                {
                    targetSymbol = underlyingType;
                }
                else
                {
                    return false;
                }
            }
            
            using ArrayBufferWriter<PropertyInfo> propertiesBuilder = new();
            using PropertyInfoBuilder propertyInfoBuilder = new();
            
            Accessibility targetAccessibility = Accessibility.Public;

            while (targetSymbol != null)
            {
                ImmutableArray<ISymbol> members = targetSymbol.GetMembers();
                for (int i = 0; i < members.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    ISymbol memberSymbol = members[i];
                    if (memberSymbol is not IPropertySymbol propertySymbol
                        || propertySymbol.DeclaredAccessibility < targetAccessibility)
                    {
                        continue;
                    }

                    if (propertyInfoBuilder.Create(propertySymbol, out bool isErrorType) is PropertyInfo info)
                    {
                        propertiesBuilder.Write(info);
                    }
                    else if (isErrorType)
                    {
                        return false;
                    }
                }

                targetSymbol = targetSymbol.BaseType;
            }

            if (propertiesBuilder.WrittenCount > 0)
            {
                properties = propertiesBuilder.WrittenSpan.ToImmutableArray();
            }
            
            return true;
        }
    }
}

