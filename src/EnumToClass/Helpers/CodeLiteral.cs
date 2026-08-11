using Microsoft.CodeAnalysis.CSharp;

namespace EnumToClass.Helpers;

internal static class CodeLiteral
{
    /// <summary>
    /// Formats a string as a C# string literal (quoted and escaped).
    /// </summary>
    public static string String(string? value)
        => SymbolDisplay.FormatLiteral(value ?? string.Empty, quote: true);
}
