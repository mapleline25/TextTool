using Microsoft.CodeAnalysis.CSharp;

namespace TextTool.SourceGenerators.Models;

internal record HierarchyTypeInfo(string Name, SyntaxKind KeywordKind);
