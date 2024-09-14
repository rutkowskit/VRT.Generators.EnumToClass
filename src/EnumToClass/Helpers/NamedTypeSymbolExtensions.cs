using Microsoft.CodeAnalysis;

namespace EnumToClass;

internal static class NamedTypeSymbolExtensions
{
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
}
