using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace TextTool.SourceGenerators.Extensions;

internal static class INamedTypeSymbolExtensions
{
    public static ITypeSymbol? GetNullableUnderlyingType(this INamedTypeSymbol targetSymbol)
    {
        if (targetSymbol.IsValueType && targetSymbol.IsGenericType && targetSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return targetSymbol.TypeArguments[0];
        }

        return null;
    }
    
    public static bool HasMemberOrMethod(this INamedTypeSymbol targetSymbol, string memberName, int methodParamsLength = -1, Accessibility minimalAccessibility = Accessibility.Private)
    {
        ImmutableArray<ISymbol> members = targetSymbol.GetMembers(memberName);
        for (int i = 0; i < members.Length; i++)
        {
            // return true if the member is:
            // a non-method, or
            // a method with specific parameter length
            ISymbol member = members[i];
            if (member.DeclaredAccessibility >= minimalAccessibility &&
                (member is not IMethodSymbol methodSymbol || methodParamsLength >= 0 && methodSymbol.Parameters.Length == methodParamsLength))
            {
                return true;
            }
        }

        return false;
    }
}
