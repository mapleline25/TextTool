using Microsoft.CodeAnalysis;
using System;

namespace TextTool.SourceGenerators.Extensions;

internal static class SyntaxNodeExtensions
{
    public static TNode? FirstAncestor<TNode>(this SyntaxNode node, Func<TNode, bool>? predicate = null, bool ascendOutOfTrivia = true)
        where TNode : SyntaxNode
    {
        return node.Parent?.FirstAncestorOrSelf(predicate, ascendOutOfTrivia);
    }
}
