using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumToClass.Helpers;

internal static class FieldSymbolExtensions
{
    public static string GetMemberDocumentationComment(this IFieldSymbol symbol)
    {
        var syntaxReference = symbol?.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference == null || syntaxReference.GetSyntax() is not EnumMemberDeclarationSyntax syntaxNode)
        {
            return "";
        }
        var result = syntaxNode
            .GetLeadingTrivia()
            .FirstOrDefault(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .ToFullString();
        if (string.IsNullOrWhiteSpace(result) is false)
        {
            return result;
        }

        return $"""
                /// <summary>
                /// {symbol!.Name}
                /// </summary>
                """;
    }
}
