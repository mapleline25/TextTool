using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CommunityToolkit.Mvvm.SourceGenerators.Helpers;
using TextTool.SourceGenerators.Helpers;
using static TextTool.SourceGenerators.Helpers.Resources;
using TextTool.SourceGenerators.Extensions;

namespace TextTool.SourceGenerators.Models;

internal ref struct PropertyInfoBuilder
{
    private readonly Dictionary<string, PropertyCompareInfo> _propertyTypeCache;
    private readonly ArrayBufferWriter<string> _arguments;
    private SearchResultFlags _flags;

    public PropertyInfoBuilder()
    {
        _propertyTypeCache = [];
        _arguments = new();
        _flags = SearchResultFlags.None;
    }

    public PropertyInfo? Create(IPropertySymbol propertySymbol, out bool isErrorType)
    {
        if (propertySymbol is IErrorTypeSymbol)
        {
            isErrorType = true;
            return null;
        }
        
        isErrorType = false;
        ITypeSymbol propertyType = propertySymbol.Type;
        string propertyName = propertySymbol.Name;

        if (propertyType.SpecialType == SpecialType.System_Object
            || propertyType is IArrayTypeSymbol
            || propertyType is IDynamicTypeSymbol
            || propertyType is IFunctionPointerTypeSymbol
            || propertyType is IPointerTypeSymbol
            || propertyName.AsSpan() is "this[]")
        {
            return null;
        }

        PropertyTypeKind kind = GetPropertyTypeKind(propertyType, out ITypeSymbol? nullableUnderlyingType);
        if (nullableUnderlyingType != null)
        {
            propertyType = nullableUnderlyingType;
        }

        INamedTypeSymbol? namedType = propertyType as INamedTypeSymbol;
        if (namedType != null && FindInSpecialType(namedType, out PropertyCompareInfo? info))
        {
            return new(propertyName, kind, info!);
        }

        string targetTypeFullNameString = propertyType.ToDisplayString(Formats.FullyQualified);
        if (namedType != null && FindInOtherNumericType(targetTypeFullNameString, out info))
        {
            return new(propertyName, kind, info!);
        }

        if (TryGetInfo(targetTypeFullNameString, out info))
        {
            return new(propertyName, kind, info);
        }

        _flags = SearchResultFlags.None;
        _arguments.ResetWrittenCount();

        if (namedType != null)
        {
            _ = FindInNamedType(string.Empty, namedType, targetTypeFullNameString);
        }
        else if (propertyType is ITypeParameterSymbol typeParameter)
        {
            _ = FindInTypeParameter(targetTypeFullNameString, typeParameter, typeParameter, 0);
        }

        // check result flags
        if (HasFlags(SearchResultFlags.IComparisonOperators))
        {
            info = PropertyCompareInfo.IComparisonOperatorsInfo;
        }
        else if (HasFlags(SearchResultFlags.IComparable_T))
        {
            info = PropertyCompareInfo.IComparable_TInfo with
            {
                TypeArguments = GetArguments()
            };
        }
        else if (HasFlags(SearchResultFlags.IComparable))
        {
            info = PropertyCompareInfo.IComparableInfo;
        }
        else if (kind == PropertyTypeKind.TypeParameter)
        {
            info = PropertyCompareInfo.Default with
            {
                TypeArguments = new([targetTypeFullNameString])
            };
        }
        else // ReferenceType / ValueType / NullableValueType with CompareKind.Unknown
        {
            info = null;
        }

        SaveInfo(targetTypeFullNameString, info);

        return info == null ? null : new(propertyName, kind, info);
    }

    public void Dispose()
    {
        _propertyTypeCache.Clear();
        _arguments.Dispose();
    }

    private static PropertyTypeKind GetPropertyTypeKind(ITypeSymbol typeSymbol, out ITypeSymbol? nullableUnderlyingType)
    {
        nullableUnderlyingType = null;

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            nullableUnderlyingType = namedType.GetNullableUnderlyingType();

            if (nullableUnderlyingType != null)
            {
                return PropertyTypeKind.NullableValueType;
            }
            else
            {
                return namedType.IsReferenceType ? PropertyTypeKind.ReferenceType : PropertyTypeKind.ValueType;
            }    
        }
        else if (typeSymbol is ITypeParameterSymbol typeParameter)
        {
            // always first check IsValueType to determine whether it is a nullable value type
            if (typeParameter.IsValueType)
            {
                if (typeParameter.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    nullableUnderlyingType = typeParameter.OriginalDefinition;
                    return PropertyTypeKind.NullableValueType;
                }
                else
                {
                    return PropertyTypeKind.ValueType;
                }    
            }
            else if (typeParameter.IsReferenceType)
            {
                return PropertyTypeKind.ReferenceType;
            }
        }

        return PropertyTypeKind.TypeParameter;
    }

    private static bool FindInSpecialType(INamedTypeSymbol targetType, out PropertyCompareInfo? info)
    {
        SpecialType type = targetType.SpecialType;

        if (type == SpecialType.System_String)
        {
            info = PropertyCompareInfo.StringInfo;
            return true;
        }

        if (type == SpecialType.System_Boolean)
        {
            info = PropertyCompareInfo.BooleanInfo;
            return true;
        }

        // check the type is numeric
        for (int i = 0; i < SystemTypeCache.SpecialNumericTypes.Length; i++)
        {
            if (SystemTypeCache.SpecialNumericTypes[i] == type)
            {
                info = PropertyCompareInfo.IComparisonOperatorsInfo;
                return true;
            }
        }

        info = null;
        return false;
    }

    private static bool FindInOtherNumericType(string targetTypeFullNameString, out PropertyCompareInfo? info)
    {
        ReadOnlySpan<char> fullNameWithoutGenerics = RemoveGenerics(targetTypeFullNameString);

        // check the type is other possible numeric type
        for (int i = 0; i < SystemTypeCache.SystemNumericTypes.Length; i++)
        {
            if (fullNameWithoutGenerics.SequenceEqual(SystemTypeCache.SystemNumericTypes[i]))
            {
                info = PropertyCompareInfo.IComparisonOperatorsInfo;
                return true;
            }
        }

        info = null;
        return false;
    }

    private bool FindInNamedType(
        string targetTypeFullNameString, INamedTypeSymbol currentNamedType, string currentNamedTypeFullNameString)
    {
        // check the type 'itself is' IComparisonOperator<TSelf, TOther, TResult> or IComparable or IComparable<T>
        if (FindInInterface(targetTypeFullNameString, currentNamedType, currentNamedTypeFullNameString))
        {
            return true;
        }
        
        // check the type 'implements' IComparisonOperator<TSelf, TOther, TResult> or IComparable or IComparable<T>
        ImmutableArray<INamedTypeSymbol> interfaces = currentNamedType.AllInterfaces;
        for (int i = 0; i < interfaces.Length; i++)
        {
            INamedTypeSymbol baseInterface = interfaces[i];
            if (FindInInterface(currentNamedTypeFullNameString, baseInterface, baseInterface.ToDisplayString(Formats.FullyQualified)))
            {
                return true;
            }
        }

        // check the type has op_GreaterThan method
        if (ContainsIComparisonOperatorsMethod(currentNamedTypeFullNameString, currentNamedType.GetMembers("op_GreaterThan")))
        {
            SetFlags(SearchResultFlags.IComparisonOperators);
            return true;
        }

        // check any base type has op_GreaterThan method
        ITypeSymbol? type = currentNamedType;
        while (true)
        {
            type = type.BaseType;
            if (type == null)
            {
                break;
            }

            ImmutableArray<ISymbol> targetTypeMembers = type.GetMembers("op_GreaterThan");
            if (targetTypeMembers.Length == 0)
            {
                continue;
            }
            
            if (ContainsIComparisonOperatorsMethod(type.ToDisplayString(Formats.FullyQualified), targetTypeMembers))
            {
                SetFlags(SearchResultFlags.IComparisonOperators);
                return true;
            }
        }
        
        return false;
    }

    private bool FindInTypeParameter(
        string targetTypeFullNameString, ITypeParameterSymbol topType, ITypeParameterSymbol currentType, int currentRecursionDepth)
    {
        // avoid constraint loop
        if (currentRecursionDepth > 0 && topType.Name.AsSpan().SequenceEqual(currentType.Name))
        {
            return false;
        }

        ImmutableArray<ITypeSymbol> constraints = currentType.ConstraintTypes;

        for (int i = 0; i < constraints.Length; i++)
        {
            ITypeSymbol constraint = constraints[i];
            
            if (constraint is INamedTypeSymbol namedType)
            {
                if (FindInNamedType(targetTypeFullNameString, namedType, namedType.ToDisplayString(Formats.FullyQualified)))
                {
                    return true;
                }
            }
            else if (constraint is ITypeParameterSymbol typeParameter
                && FindInTypeParameter(targetTypeFullNameString, topType, typeParameter, ++currentRecursionDepth))
            {
                return true;
            }
        }

        return false;
    }

    private bool FindInInterface(string targetTypeFullNameString, INamedTypeSymbol interfaceType, string interfaceTypeFullNameString)
    {
        TypeKind typeKind = interfaceType.TypeKind;

        if (typeKind == TypeKind.Interface)
        {
            return FindInIComparable(targetTypeFullNameString, interfaceType, interfaceTypeFullNameString);
        }
        
        if (targetTypeFullNameString.Length > 0)
        {
            return FindInIComparisonOperators(targetTypeFullNameString, interfaceType);
        }

        return false;
    }

    private bool FindInIComparable(string targetTypeFullNameString, INamedTypeSymbol interfaceType, string interfaceTypeFullNameString)
    {
        ReadOnlySpan<char> targetTypeFullName = targetTypeFullNameString;
        ReadOnlySpan<char> interfaceTypeFullName = RemoveGenerics(interfaceTypeFullNameString);

        if (interfaceType.IsGenericType)
        {
            if (interfaceTypeFullName.SequenceEqual(TypeResource.IComparable))
            {
                ImmutableArray<ITypeSymbol> args = interfaceType.TypeArguments;

                if (args.Length != 1)
                {
                    return false;
                }

                string arg = args[0].ToDisplayString(Formats.FullyQualified);
                if (targetTypeFullName.IsEmpty || targetTypeFullName.SequenceEqual(arg.AsSpan()))
                {
                    SetFlags(SearchResultFlags.IComparable_T);
                    AddArgument(arg);
                }
            }
        }
        else if (interfaceTypeFullName.SequenceEqual(TypeResource.IComparable))
        {
            SetFlags(SearchResultFlags.IComparable);
        }

        return false;
    }

    private bool FindInIComparisonOperators(string targetTypeFullNameString, INamedTypeSymbol interfaceType)
    {
        if (interfaceType.IsGenericType && interfaceType.MetadataName.AsSpan().SequenceEqual(TypeResource.IComparisonOperators))
        {
            ReadOnlySpan<char> targetTypeFullName = targetTypeFullNameString;

            ImmutableArray<ITypeSymbol> args = interfaceType.TypeArguments;
            if (args[2].SpecialType == SpecialType.System_Boolean
                && args[0].ToDisplayString(Formats.FullyQualified).AsSpan().SequenceEqual(targetTypeFullName)
                && args[1].ToDisplayString(Formats.FullyQualified).AsSpan().SequenceEqual(targetTypeFullName))
            {
                SetFlags(SearchResultFlags.IComparisonOperators);
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIComparisonOperatorsMethod(ReadOnlySpan<char> targetTypeFullName, ImmutableArray<ISymbol> targetTypeMembers)
    {
        for (int i = 0; i < targetTypeMembers.Length; i++)
        {
            ISymbol member = targetTypeMembers[i];

            if (member is not IMethodSymbol method
                || method.DeclaredAccessibility < Accessibility.Public
                || method.MethodKind != MethodKind.BuiltinOperator && method.MethodKind != MethodKind.UserDefinedOperator
                || method.ReturnType.SpecialType != SpecialType.System_Boolean)
            {
                continue;
            }

            ImmutableArray<IParameterSymbol> parameters = method.Parameters;
            if (parameters.Length == 2
                && parameters[0].Type.ToDisplayString(Formats.FullyQualified).SequenceEqual(targetTypeFullName)
                && parameters[1].Type.ToDisplayString(Formats.FullyQualified).SequenceEqual(targetTypeFullName))
            {
                return true;
            }
        }

        return false;
    }

    private static ReadOnlySpan<char> RemoveGenerics(ReadOnlySpan<char> typeName)
    {
        int index = typeName.IndexOf('<');
        return index > 0 ? typeName.Slice(0, index) : typeName;
    }

    private void AddArgument(string argument)
    {
        _arguments.Write(argument);
    }

    private ImmutableArray<string> GetArguments()
    {
        return _arguments.WrittenSpan.ToImmutableArray();
    }

    private bool TryGetInfo(string propertyTypeFullName, out PropertyCompareInfo info)
    {
        return _propertyTypeCache!.TryGetValue(propertyTypeFullName, out info);
    }

    private void SaveInfo(string propertyTypeFullName, PropertyCompareInfo info)
    {
        _propertyTypeCache![propertyTypeFullName] = info;
    }

    private void SetFlags(SearchResultFlags flags)
    {
        _flags |= flags;
    }

    private bool HasFlags(SearchResultFlags flags)
    {
        return (_flags & flags) != 0;
    }

    [Flags]
    private enum SearchResultFlags
    {
        None = 0,
        IComparable = 1,
        IComparable_T = 2,
        IComparisonOperators = 4,
    }
}

// private helper class
file static class SystemTypeCache
{
    internal static readonly SpecialType[] SpecialNumericTypes =
    [
        SpecialType.System_Byte,
        SpecialType.System_Char,
        SpecialType.System_DateTime,
        SpecialType.System_Decimal,
        SpecialType.System_Double,
        SpecialType.System_Enum,
        SpecialType.System_Int16,
        SpecialType.System_Int32,
        SpecialType.System_Int64,
        SpecialType.System_IntPtr,
        SpecialType.System_SByte,
        SpecialType.System_Single,
        SpecialType.System_UInt16,
        SpecialType.System_UInt32,
        SpecialType.System_UInt64,
        SpecialType.System_UIntPtr
    ];

    internal static readonly string[] SystemNumericTypes =
    [
        "global::System.Int128",
        "global::System.UInt128",
        "global::System.Numerics.BigInteger",
        "global::System.Half",
        "NFloat"
    ];
}
