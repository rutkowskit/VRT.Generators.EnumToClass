using Microsoft.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace VRT.Generators;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class EnumToClassDiagnostics
{
    public const string Category = "EnumToClass";

    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "ETC001",
        title: "Type must be partial",
        messageFormat: "Type '{0}' decorated with EnumToClassAttribute must be declared partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Source generation requires a partial class or record so members can be added in generated files.");

    public static readonly DiagnosticDescriptor EnumHasNoMembers = new(
        id: "ETC002",
        title: "Enum has no members",
        messageFormat: "Enum type used by '{0}' has no named members; only Empty will be available",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
