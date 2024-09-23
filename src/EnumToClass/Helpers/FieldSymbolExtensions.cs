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

        var documentationString = symbol.GetDescriptionAttributeValue() ?? symbol!.Name;

        return $"""
                /// <summary>
                /// {documentationString}
                /// </summary>
                """;
    }
    public static string? GetDescriptionAttributeValue(this IFieldSymbol? symbol)
    {
        var descriptionAttribute = symbol?
            .GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute");

        return descriptionAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
    }
}
