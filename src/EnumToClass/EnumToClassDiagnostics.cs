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

    public static readonly DiagnosticDescriptor NestedTypeNotSupported = new(
        id: "ETC003",
        title: "Nested type is not supported",
        messageFormat: "Type '{0}' is nested inside '{1}'; EnumToClassAttribute is not supported on nested types",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Generated partials are emitted at namespace scope and cannot correctly extend nested host types.");

    public static readonly DiagnosticDescriptor AttributePropertyNotConstructible = new(
        id: "ETC010",
        title: "Enum member attribute cannot be projected",
        messageFormat: "Attribute type '{0}' cannot be reconstructed as a property value: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateAttributeProperty = new(
        id: "ETC011",
        title: "Duplicate EnumToClassProperty attribute type",
        messageFormat: "EnumToClassPropertyAttribute is applied more than once for attribute type '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AttributePropertyNameInvalid = new(
        id: "ETC012",
        title: "Invalid or conflicting attribute property name",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AttributePropertySourceInvalid = new(
        id: "ETC014",
        title: "Invalid EnumToClassProperty Source",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AttributePropertyModeConflict = new(
        id: "ETC015",
        title: "Conflicting EnumToClassProperty options",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor? TryGetDescriptor(string id) => id switch
    {
        "ETC001" => TypeMustBePartial,
        "ETC002" => EnumHasNoMembers,
        "ETC003" => NestedTypeNotSupported,
        "ETC010" => AttributePropertyNotConstructible,
        "ETC011" => DuplicateAttributeProperty,
        "ETC012" => AttributePropertyNameInvalid,
        "ETC014" => AttributePropertySourceInvalid,
        "ETC015" => AttributePropertyModeConflict,
        _ => null
    };
}
