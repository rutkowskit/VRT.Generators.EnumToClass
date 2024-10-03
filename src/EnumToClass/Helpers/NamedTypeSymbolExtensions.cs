using Microsoft.CodeAnalysis;

namespace EnumToClass.Helpers;

internal static class NamedTypeSymbolExtensions
{
    public static string GetPartialDeclaration(this INamedTypeSymbol symbol)
        => $"{symbol.GetAccessibility()} partial {(symbol.IsRecord ? "record" : "class")} {symbol.Name}";
    public static string GetAccessibility(this INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Internal => "internal",

            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.ProtectedOrInternal => "private protected",
            _ => "",
        };
    }
    public static AttributeData? GetAttribute(this INamedTypeSymbol symbol,
        string attributeTypeNamespace, string attributeTypeName)
    {
        var attributeData = symbol?
            .GetAttributes()
            .Where(a => a.AttributeClass != null)
            .Where(a => a.AttributeClass!.ContainingNamespace.ToDisplayString() == attributeTypeNamespace)
            .FirstOrDefault(a => a.AttributeClass!.Name == attributeTypeName);
        return attributeData;
    }

    public static bool TryGetNamedArgument<T>(this AttributeData attributeData, string name, out T value)
    {
        value = default!;
        var data = attributeData.NamedArguments
            .FirstOrDefault(n => n.Key == name);

        if (string.IsNullOrEmpty(data.Key) is false && data.Value.Value is T dataValue)
        {
            value = dataValue;
            return true;
        }
        return false;
    }
}
