using Microsoft.CodeAnalysis;
using System.Text;
using VRT.Generators;

namespace EnumToClass.Helpers;

/// <summary>
/// Emits property declarations and per-member assignment expressions for a projection kind.
/// </summary>
internal static class AttributeProjectionEmitter
{
    private static readonly SymbolDisplayFormat FullyQualified = SymbolDisplayFormat.FullyQualifiedFormat;

    public static string FormatDeclaration(AttributePropertyProjection property) => property.Kind switch
    {
        AttributeProjectionKind.BooleanPresence =>
            $"{property.Accessibility} bool {property.PropertyName} {{ get; private init; }}",

        AttributeProjectionKind.ArrayOfInstances or AttributeProjectionKind.ArrayOfMemberValues =>
            $"{property.Accessibility} {property.PropertyTypeDisplayName} {property.PropertyName} {{ get; private init; }} = global::System.Array.Empty<{property.ElementTypeFullName}>();",

        _ when property.UseNullableAnnotation =>
            $"{property.Accessibility} {property.PropertyTypeDisplayName}? {property.PropertyName} {{ get; private init; }}",

        _ =>
            $"{property.Accessibility} {property.PropertyTypeDisplayName} {property.PropertyName} {{ get; private init; }}",
    };

    public static AttributePropertyAssignment BuildAssignment(
        IEnumerable<AttributeData> fieldAttributes,
        AttributePropertyProjection projection,
        List<PendingDiagnostic> diagnostics)
    {
        var matches = CollectMatches(fieldAttributes, projection);

        return projection.Kind switch
        {
            AttributeProjectionKind.BooleanPresence =>
                new AttributePropertyAssignment(projection.PropertyName, matches.Count > 0 ? "true" : "false"),

            AttributeProjectionKind.ArrayOfInstances or AttributeProjectionKind.ArrayOfMemberValues =>
                BuildArrayAssignment(projection, matches, diagnostics),

            _ => BuildSingleAssignment(projection, matches, diagnostics),
        };
    }

    private static List<AttributeData> CollectMatches(
        IEnumerable<AttributeData> fieldAttributes,
        AttributePropertyProjection projection)
    {
        var matches = new List<AttributeData>();
        var stopAfterFirst = AttributeProjectionKindResolver.StopAfterFirstMatch(projection.Kind);

        foreach (var candidate in fieldAttributes)
        {
            if (candidate.AttributeClass is null)
            {
                continue;
            }

            if (candidate.AttributeClass.ToDisplayString(FullyQualified) != projection.AttributeTypeFullName)
            {
                continue;
            }

            matches.Add(candidate);
            if (stopAfterFirst)
            {
                break;
            }
        }

        return matches;
    }

    private static AttributePropertyAssignment BuildSingleAssignment(
        AttributePropertyProjection projection,
        List<AttributeData> matches,
        List<PendingDiagnostic> diagnostics)
    {
        if (matches.Count == 0)
        {
            return new AttributePropertyAssignment(projection.PropertyName, creationExpression: null);
        }

        if (TryFormatValue(matches[0], projection, out var expression, out var error))
        {
            return new AttributePropertyAssignment(projection.PropertyName, expression);
        }

        diagnostics.Add(new PendingDiagnostic(
            EnumToClassDiagnostics.AttributePropertyNotConstructible.Id,
            projection.AttributeTypeFullName,
            error ?? "unknown error"));
        return new AttributePropertyAssignment(projection.PropertyName, creationExpression: null);
    }

    private static AttributePropertyAssignment BuildArrayAssignment(
        AttributePropertyProjection projection,
        List<AttributeData> matches,
        List<PendingDiagnostic> diagnostics)
    {
        if (matches.Count == 0)
        {
            return EmptyArray(projection);
        }

        var elements = new List<string>(matches.Count);
        foreach (var match in matches)
        {
            if (TryFormatValue(match, projection, out var expression, out var error))
            {
                elements.Add(expression);
            }
            else
            {
                diagnostics.Add(new PendingDiagnostic(
                    EnumToClassDiagnostics.AttributePropertyNotConstructible.Id,
                    projection.AttributeTypeFullName,
                    error ?? "unknown error"));
            }
        }

        if (elements.Count == 0)
        {
            return EmptyArray(projection);
        }

        return new AttributePropertyAssignment(
            projection.PropertyName,
            $"new {projection.ElementTypeFullName}[] {{ {string.Join(", ", elements)} }}");
    }

    private static AttributePropertyAssignment EmptyArray(AttributePropertyProjection projection)
        => new(projection.PropertyName, $"global::System.Array.Empty<{projection.ElementTypeFullName}>()");

    private static bool TryFormatValue(
        AttributeData match,
        AttributePropertyProjection projection,
        out string expression,
        out string? error)
    {
        if (projection.SourceMemberName is not null)
        {
            return AttributeConstructionEmitter.TryFormatMemberValue(
                match, projection.SourceMemberName, out expression, out error);
        }

        return AttributeConstructionEmitter.TryFormat(match, out expression, out error);
    }
}
