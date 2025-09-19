using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TextTool.SourceGenerators.Models;

internal ref struct TypeDeclarationSyntaxInfo
{
    private readonly TypeDeclarationSyntax _syntax;
    private Location? _locaction;
    private string? _identifier;
    private string? _identifierWithTypeParameters;

    public TypeDeclarationSyntaxInfo(TypeDeclarationSyntax syntax)
    {
        _syntax = syntax;
    }

    public string Identifier
    {
        get
        {
            if (_identifier == null)
            {
                _identifier = _syntax.Identifier.ToString();
            }
            return _identifier;
        }
    }

    public string IdentifierWithTypeParameters
    {
        get
        {
            if (_identifierWithTypeParameters == null)
            {
                foreach (SyntaxNode node in _syntax.ChildNodes())
                {
                    if (node is TypeParameterListSyntax list)
                    {
                        _identifierWithTypeParameters = Identifier + list.ToString();
                        return _identifierWithTypeParameters;
                    }
                }
                _identifierWithTypeParameters = Identifier;
            }
            return _identifierWithTypeParameters;
        }
    }

    public Location Location
    {
        get
        {
            if (_locaction == null)
            {
                _locaction = _syntax.GetLocation();
            }

            return _locaction;
        }
    }

    public void Dispose()
    {

    }
}
