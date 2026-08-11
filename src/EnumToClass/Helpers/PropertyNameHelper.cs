using Microsoft.CodeAnalysis.CSharp;

namespace EnumToClass.Helpers;

internal static class PropertyNameHelper
{
    private const string AttributeSuffix = "Attribute";

    /// <summary>
    /// Default property name: type short name without trailing "Attribute".
    /// </summary>
    public static string FromAttributeTypeName(string attributeTypeName)
    {
        if (attributeTypeName.Length > AttributeSuffix.Length &&
            attributeTypeName.EndsWith(AttributeSuffix, StringComparison.Ordinal))
        {
            return attributeTypeName.Substring(0, attributeTypeName.Length - AttributeSuffix.Length);
        }

        return attributeTypeName;
    }

    public static bool IsValidIdentifier(string name)
        => SyntaxFacts.IsValidIdentifier(name);
}
