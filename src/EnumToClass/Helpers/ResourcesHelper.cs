using System.Reflection;

namespace EnumToClass.Helpers;

internal static class ResourcesHelper
{
    public static Stream GetStream(this Assembly assembly, string resourceName)
    {
        var manifestResourceName = assembly
            .GetManifestResourceNames()
            .Single(r => r.EndsWith($".{resourceName}", StringComparison.InvariantCultureIgnoreCase));

        return assembly.GetManifestResourceStream(manifestResourceName)
            ?? throw new ArgumentException($"Embeded resource: \"{resourceName}\" not found");

    }
    public static string GetString(this Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetStream(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
