using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace EnumToClass.Helpers;

internal static class FieldSymbolExtensions
{
    public static string GetMemberDocumentationComment(this IFieldSymbol symbol)
    {
        return symbol.GetSymbolDocumentationCommentBySyntax()
            ?? symbol.GetSymbolDocumentationCommentByDescription()
            ?? "";
    }

    public static string? GetSymbolDocumentationCommentBySyntax(this IFieldSymbol? symbol)
    {
        return symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() switch
        {
            MemberDeclarationSyntax syntaxNode => syntaxNode.GetMemberDeclarationDocumentationComment(),
            _ => null
        };
    }
    public static string? GetSymbolDocumentationCommentByDescription(this IFieldSymbol? symbol)
    {
        if (symbol is null)
        {
            return null;
        }
        var documentationString = symbol.GetDocumentationCommentXml(expandIncludes: true).ConvertToSummary().NullIfEmpty()
            ?? symbol.GetDescriptionAttributeValue().NullIfEmpty()
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
        return result.NullIfEmpty();
    }

    private static string? GetMemberDeclarationDocumentationComment(this MemberDeclarationSyntax? memberSyntax)
    {
        var trivias = memberSyntax?.GetLeadingTrivia();
        if (trivias is null)
        {
            return null;
        }
        var result = GetDocumentation(trivias.Value)
            ?? GetDocumentationFromSingleLineComments(trivias.Value);
        return result.NullIfEmpty();
    }
    private static string? GetDocumentation(SyntaxTriviaList trivias)
    {
        var documentationTrivias = trivias
            .Where(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            .Select(f => f.ToFullString())
            .FirstOrDefault(f => string.IsNullOrWhiteSpace(f) is false);
        return documentationTrivias.NullIfEmpty();
    }
    private static string? GetDocumentationFromSingleLineComments(SyntaxTriviaList trivias)
    {
        var documentationTrivias = trivias
            .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            .Select(f => f.ToFullString())
            .ToArray();
        var result = string.Join("\n", documentationTrivias).NullIfEmpty();
        return result is null || result.Contains("summary") is false
            ? null
            : result;
    }

    private static string? NullIfEmpty(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

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
