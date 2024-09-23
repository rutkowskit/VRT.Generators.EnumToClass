using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace EnumToClass.Helpers;

internal static class FieldSymbolExtensions
{
    public static string GetMemberDocumentationComment(this IFieldSymbol symbol)
    {
        if (symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is MemberDeclarationSyntax syntaxNode)
        {
            return syntaxNode.GetMemberDeclarationDocumentationComment()
                ?? symbol.GetSymbolDocumentationCommentByDescription()
                ?? "";
        }
        return symbol.GetSymbolDocumentationCommentByDescription() ?? "";
    }
    public static string? GetSymbolDocumentationCommentByDescription(this IFieldSymbol? symbol)
    {
        if (symbol is null)
        {
            return null;
        }
        var documentationString = symbol.GetDocumentationCommentXml().ConvertToSummary().NullIfEmtpy()
            ?? symbol.GetDescriptionAttributeValue().NullIfEmtpy()
            ?? symbol.Name;

        return $"""
                /// <summary>
                /// {documentationString!.Replace("\n", "\n                /// ")}
                /// </summary>
                """;
    }

    public static string? GetDescriptionAttributeValue(this IFieldSymbol? symbol)
    {
        var descriptionAttribute = symbol?
            .GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute");
        var result = descriptionAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
        return result.NullIfEmtpy();
    }

    private static string? GetMemberDeclarationDocumentationComment(this MemberDeclarationSyntax? memberSyntax)
    {
        var result = memberSyntax?
            .GetLeadingTrivia()
            .Where(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .Select(f => f.ToFullString())
            .FirstOrDefault(f => string.IsNullOrWhiteSpace(f) is false);

        return result.NullIfEmtpy();
    }

    private static string? NullIfEmtpy(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ConvertToSummary(this string? xmlDocumentation)
    {
        if (string.IsNullOrEmpty(xmlDocumentation))
        {
            return string.Empty;
        }

        try
        {
            var doc = XDocument.Parse(xmlDocumentation);

            var summaryElement = doc.Root.Element("summary");
            if (summaryElement != null)
            {
                return summaryElement.Value.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing XML documentation: {ex.Message}");
        }

        return string.Empty;
    }
}
