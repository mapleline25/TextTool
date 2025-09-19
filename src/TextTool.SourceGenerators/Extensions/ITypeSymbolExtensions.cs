using CommunityToolkit.Mvvm.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using System;

namespace TextTool.SourceGenerators.Extensions;

internal static class ITypeSymbolExtensions
{
    public static string GetFullyMetadataName(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol == null || IsGlobalNamespace(typeSymbol))
        {
            return string.Empty;
        }

        using ArrayBufferWriter<INamespaceOrTypeSymbol> stack = new(32);

        ISymbol? current = typeSymbol;
        do
        {
            if (current is INamespaceOrTypeSymbol symbol)
            {
                stack.Write(symbol);
            }
            current = current.ContainingSymbol;
        }
        while (!IsGlobalNamespace(current));

        ReadOnlySpan<INamespaceOrTypeSymbol> symbols = stack.WrittenSpan;
        using ArrayBufferWriter<char> name = new();

        bool hasType = false;
        for (int i = symbols.Length - 1; i >= 0; i--)
        {
            current = symbols[i];
            char prefix = '.';

            if (current is ITypeSymbol)
            {
                if (!hasType)
                {
                    hasType = true;
                }
                else
                {
                    prefix = '+';
                }
            }

            if (name.WrittenCount > 0)
            {
                name.Write(prefix);
            }

            name.Write(current.MetadataName);
        }

        return name.WrittenSpan.ToString();
    }

    private static bool IsGlobalNamespace(ISymbol symbol)
    {
        return symbol is INamespaceSymbol namespaceSymbol && namespaceSymbol.IsGlobalNamespace;
    }
}
