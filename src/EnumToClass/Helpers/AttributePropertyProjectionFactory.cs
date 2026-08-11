using Microsoft.CodeAnalysis;
using VRT.Generators;

namespace EnumToClass.Helpers;

/// <summary>
/// Builds one <see cref="AttributePropertyProjection"/> from a host attribute application.
/// </summary>
internal static class AttributePropertyProjectionFactory
{
    private static readonly SymbolDisplayFormat FullyQualified = SymbolDisplayFormat.FullyQualifiedFormat;

    public static bool TryCreate(
        AttributeData attr,
        HashSet<string> seenProjectionKeys,
        HashSet<string> seenPropertyNames,
        List<PendingDiagnostic> diagnostics,
        out AttributePropertyProjection? projection)
    {
        projection = null;

        if (!TryGetTargetAttributeType(attr, out var attributeType))
        {
            return false;
        }

        var options = ReadOptions(attr);
        if (!AttributeProjectionKindResolver.TryResolve(
                options.AsBoolean,
                options.AsArray,
                options.SourceMemberName,
                out var kind,
                out var conflictMessage))
        {
            diagnostics.Add(new PendingDiagnostic(
                EnumToClassDiagnostics.AttributePropertyModeConflict.Id,
                conflictMessage ?? "Conflicting EnumToClassProperty options."));
            return false;
        }

        var attributeTypeFullName = attributeType.ToDisplayString(FullyQualified);
        if (!TryRegisterKeys(
                attributeType,
                attributeTypeFullName,
                options.SourceMemberName,
                kind,
                seenProjectionKeys,
                diagnostics))
        {
            return false;
        }

        if (!TryResolveHostPropertyName(attr, attributeType.Name, seenPropertyNames, diagnostics, out var propertyName))
        {
            return false;
        }

        if (!TryResolveElementType(
                kind,
                attributeType,
                attributeTypeFullName,
                options.SourceMemberName,
                diagnostics,
                out var elementTypeFullName,
                out var elementIsNullableValueType))
        {
            return false;
        }

        var propertyTypeDisplayName = IsArrayKind(kind)
            ? elementTypeFullName + "[]"
            : elementTypeFullName;

        var accessibility = attributeType.GetAccessibility();
        if (accessibility.Length == 0)
        {
            accessibility = "public";
        }

        projection = new AttributePropertyProjection(
            kind,
            attributeTypeFullName,
            elementTypeFullName,
            propertyTypeDisplayName,
            propertyName,
            accessibility,
            options.SourceMemberName,
            elementIsNullableValueType);
        return true;
    }

    private static bool TryGetTargetAttributeType(AttributeData attr, out INamedTypeSymbol attributeType)
    {
        attributeType = null!;
        if (attr.AttributeClass is null ||
            attr.AttributeClass.Name != EnumToClassAttributeDefinition.PropertyAttributeTypeName ||
            attr.AttributeClass.ContainingNamespace.ToDisplayString() != EnumToClassAttributeDefinition.NamespaceName ||
            attr.AttributeClass.Arity != 1)
        {
            return false;
        }

        if (attr.AttributeClass.TypeArguments[0] is not INamedTypeSymbol typeArg)
        {
            return false;
        }

        attributeType = typeArg;
        return true;
    }

    private static ProjectionOptions ReadOptions(AttributeData attr)
    {
        string? source = null;
        if (attr.TryGetNamedArgument<string>(
                EnumToClassAttributeDefinition.PropertyAttributeSourcePropertyName,
                out var sourceArg) &&
            string.IsNullOrWhiteSpace(sourceArg) is false)
        {
            source = sourceArg.Trim();
        }

        var asArray = attr.TryGetNamedArgument<bool>(
            EnumToClassAttributeDefinition.PropertyAttributeAsArrayPropertyName,
            out var arrayFlag) && arrayFlag;

        var asBoolean = attr.TryGetNamedArgument<bool>(
            EnumToClassAttributeDefinition.PropertyAttributeAsBooleanPropertyName,
            out var boolFlag) && boolFlag;

        return new ProjectionOptions(source, asArray, asBoolean);
    }

    private static bool TryRegisterKeys(
        INamedTypeSymbol attributeType,
        string attributeTypeFullName,
        string? sourceMemberName,
        AttributeProjectionKind kind,
        HashSet<string> seenProjectionKeys,
        List<PendingDiagnostic> diagnostics)
    {
        var key = attributeTypeFullName + "\u001f" +
                  (sourceMemberName ?? string.Empty) + "\u001f" +
                  AttributeProjectionKindResolver.ModeKey(kind);

        if (seenProjectionKeys.Add(key))
        {
            return true;
        }

        diagnostics.Add(new PendingDiagnostic(
            EnumToClassDiagnostics.DuplicateAttributeProperty.Id,
            attributeType.ToDisplayString() + (sourceMemberName is null ? string.Empty : $" (Source = {sourceMemberName})")));
        return false;
    }

    private static bool TryResolveHostPropertyName(
        AttributeData attr,
        string attributeTypeName,
        HashSet<string> seenPropertyNames,
        List<PendingDiagnostic> diagnostics,
        out string propertyName)
    {
        propertyName = attr.TryGetNamedArgument<string>(
            EnumToClassAttributeDefinition.PropertyAttributeNamePropertyName,
            out var name) && string.IsNullOrWhiteSpace(name) is false
            ? name.Trim()
            : PropertyNameHelper.FromAttributeTypeName(attributeTypeName);

        if (!PropertyNameHelper.IsValidIdentifier(propertyName))
        {
            diagnostics.Add(new PendingDiagnostic(
                EnumToClassDiagnostics.AttributePropertyNameInvalid.Id,
                $"Property name '{propertyName}' for attribute '{attributeTypeName}' is not a valid C# identifier."));
            return false;
        }

        if (!seenPropertyNames.Add(propertyName))
        {
            diagnostics.Add(new PendingDiagnostic(
                EnumToClassDiagnostics.AttributePropertyNameInvalid.Id,
                $"Property name '{propertyName}' is used by more than one EnumToClassPropertyAttribute."));
            return false;
        }

        return true;
    }

    private static bool TryResolveElementType(
        AttributeProjectionKind kind,
        INamedTypeSymbol attributeType,
        string attributeTypeFullName,
        string? sourceMemberName,
        List<PendingDiagnostic> diagnostics,
        out string elementTypeFullName,
        out bool elementIsNullableValueType)
    {
        elementTypeFullName = attributeTypeFullName;
        elementIsNullableValueType = false;

        switch (kind)
        {
            case AttributeProjectionKind.BooleanPresence:
                elementTypeFullName = "bool";
                return true;

            case AttributeProjectionKind.SingleMemberValue:
            case AttributeProjectionKind.ArrayOfMemberValues:
                string? sourceError = null;
                if (sourceMemberName is null ||
                    !AttributeConstructionEmitter.TryResolveSourceMemberType(
                        attributeType,
                        sourceMemberName,
                        out var memberType,
                        out sourceError))
                {
                    diagnostics.Add(new PendingDiagnostic(
                        EnumToClassDiagnostics.AttributePropertySourceInvalid.Id,
                        sourceError ?? $"Invalid Source '{sourceMemberName}'."));
                    return false;
                }

                elementTypeFullName = memberType.ToDisplayString(FullyQualified);
                elementIsNullableValueType = memberType is INamedTypeSymbol named &&
                    named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                return true;

            default:
                elementTypeFullName = attributeTypeFullName;
                return true;
        }
    }

    private static bool IsArrayKind(AttributeProjectionKind kind) =>
        kind is AttributeProjectionKind.ArrayOfInstances or AttributeProjectionKind.ArrayOfMemberValues;

    private readonly struct ProjectionOptions
    {
        public ProjectionOptions(string? sourceMemberName, bool asArray, bool asBoolean)
        {
            SourceMemberName = sourceMemberName;
            AsArray = asArray;
            AsBoolean = asBoolean;
        }

        public string? SourceMemberName { get; }
        public bool AsArray { get; }
        public bool AsBoolean { get; }
    }
}
