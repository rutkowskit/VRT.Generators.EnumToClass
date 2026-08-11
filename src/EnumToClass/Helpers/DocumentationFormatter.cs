using System.Text;

namespace EnumToClass.Helpers;

/// <summary>
/// Normalizes XML / doc comments so each line is a clean <c>/// ...</c> token
/// without source-file indentation. Callers re-indent via generation joins.
/// </summary>
internal static class DocumentationFormatter
{
    private static readonly char[] NewLineChars = { '\r', '\n' };

    /// <summary>
    /// Builds a minimal <c>/// &lt;summary&gt;</c> block from plain text (no source indent).
    /// </summary>
    public static string FormatSummaryComment(string plainText)
    {
        var sb = new StringBuilder(plainText.Length + 40);
        sb.Append("/// <summary>");

        var lines = plainText.Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            sb.Append("\n/// ");
            sb.Append(line);
        }

        sb.Append("\n/// </summary>");
        return sb.ToString();
    }

    /// <summary>
    /// Yields one documentation line per entry (no leading whitespace).
    /// Empty or non-documentation input yields nothing.
    /// </summary>
    public static IEnumerable<string> ToDocumentationLines(string? documentationComment)
    {
        if (string.IsNullOrWhiteSpace(documentationComment))
        {
            yield break;
        }

        var lines = documentationComment!.Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("///", StringComparison.Ordinal))
            {
                // "///foo" → "/// foo"
                if (line.Length > 3 && line[3] != ' ' && line[3] != '/')
                {
                    yield return "/// " + line.Substring(3);
                }
                else
                {
                    yield return line;
                }

                continue;
            }

            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                yield return "/" + line;
                continue;
            }

            yield return "/// " + line;
        }
    }
}
