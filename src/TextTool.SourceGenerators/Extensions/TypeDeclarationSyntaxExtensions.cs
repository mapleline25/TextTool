using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace TextTool.SourceGenerators.Extensions;

internal static class TypeDeclarationSyntaxExtensions
{
    public static bool HasValidAccessibility(this TypeDeclarationSyntax node)
    {
        SyntaxNode? parent = node.Parent;
        if (parent is not NamespaceDeclarationSyntax && parent is not CompilationUnitSyntax)
        {
            return true;
        }

        SyntaxTokenList modifiers = node.Modifiers;
        int length = modifiers.Count;
        for (int i = 0; i < length; i++)
        {
            SyntaxToken token = modifiers[i];
            if (token.IsKind(SyntaxKind.PublicKeyword) || token.IsKind(SyntaxKind.InternalKeyword) || token.IsKind(SyntaxKind.FileKeyword))
            {
                return true;
            }
        }

        return false;
    }
}
