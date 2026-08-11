using EnumToClass.Helpers;
using Microsoft.CodeAnalysis;
using System.Text;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace VRT.Generators;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal sealed record EnumToClassData
{
    private IReadOnlyCollection<EnumFieldData> _enumFields = [];
    private IReadOnlyList<AttributePropertyProjection> _attributeProperties = [];
    private IReadOnlyList<PendingDiagnostic> _pendingDiagnostics = [];

    public static EnumToClassData? FromClass(INamedTypeSymbol? classWithAttribute)
    {
        var attributeData = classWithAttribute?.GetAttribute(
            EnumToClassAttributeDefinition.NamespaceName,
            EnumToClassAttributeDefinition.AttributeTypeName);

        if (classWithAttribute is null ||
            attributeData is null ||
            attributeData.AttributeClass is null ||
            attributeData.AttributeClass.TypeArguments.Length == 0 ||
            attributeData.AttributeClass.TypeArguments[0] is not INamedTypeSymbol enumTypeSymbol)
        {
            return null;
        }

        var (projections, pendingDiagnostics) = CollectAttributePropertyProjections(classWithAttribute);

        var fields = enumTypeSymbol
            .GetMembers()
            .Select(m => EnumFieldData.FromSymbol(m, projections, pendingDiagnostics))
            .Where(static f => f is not null)
            .Select(static f => f!)
            .ToArray();

        return new EnumToClassData
        {
            EnumTypeFullName = enumTypeSymbol.ToDisplayString(),
            EnumTypeUnderlyingTypeName = enumTypeSymbol.EnumUnderlyingType?.ToDisplayString() ?? "int",
            ClassNamespace = classWithAttribute.ContainingNamespace.ToDisplayString(),
            ClassName = classWithAttribute.Name,
            GenerateDescription = attributeData
                .TryGetNamedArgument<bool>(EnumToClassAttributeDefinition.WithDescriptionPropertyName, out var withDescription) && withDescription,
            IsRecord = classWithAttribute.IsRecord,
            IsPartial = classWithAttribute.IsDeclaredPartial(),
            IsNested = classWithAttribute.ContainingType is not null,
            ContainingTypeName = classWithAttribute.ContainingType?.ToDisplayString() ?? string.Empty,
            Location = classWithAttribute.Locations.FirstOrDefault() ?? Location.None,
            ClassPartialDeclaration = classWithAttribute.GetPartialDeclaration(),
            _enumFields = fields,
            _attributeProperties = projections,
            _pendingDiagnostics = pendingDiagnostics
        };
    }

    public string EnumTypeFullName { get; private set; } = default!;
    public string EnumTypeUnderlyingTypeName { get; private set; } = "int";
    public string ClassName { get; private set; } = default!;
    public string ClassPartialDeclaration { get; private set; } = default!;
    public string ClassNamespace { get; private set; } = default!;
    public bool GenerateDescription { get; private set; }
    public bool IsRecord { get; private set; }
    public bool IsPartial { get; private set; }
    public bool IsNested { get; private set; }
    public string ContainingTypeName { get; private set; } = string.Empty;
    public Location Location { get; private set; } = Location.None;

    public IReadOnlyCollection<EnumFieldData> GetEnumFields() => _enumFields;
    public IReadOnlyList<AttributePropertyProjection> AttributeProperties => _attributeProperties;
    public IReadOnlyList<PendingDiagnostic> PendingDiagnostics => _pendingDiagnostics;
    public bool HasAttributeProperties => _attributeProperties.Count > 0;

    private static (IReadOnlyList<AttributePropertyProjection> Projections, List<PendingDiagnostic> Diagnostics)
        CollectAttributePropertyProjections(INamedTypeSymbol host)
    {
        var diagnostics = new List<PendingDiagnostic>();
        var projections = new List<AttributePropertyProjection>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var attr in host.GetAttributes())
        {
            if (AttributePropertyProjectionFactory.TryCreate(attr, seenKeys, seenNames, diagnostics, out var projection) &&
                projection is not null)
            {
                projections.Add(projection);
            }
        }

        return (projections, diagnostics);
    }

    /// <summary>
    /// Named enum member whose constant value equals default(TEnum), if any.
    /// </summary>
    public EnumFieldData? DefaultEnumField => GetEnumFields().FirstOrDefault(static f => f.IsDefaultValue);

    public string GetConstructorDeclaration()
    {
        return GenerateDescription
            ? $"private {ClassName}({EnumTypeFullName} value, string description)"
            : $"private {ClassName}({EnumTypeFullName} value)";
    }

    public string GetDescriptionFieldDeclaration()
    {
        return GenerateDescription ? "public string Description { get; }" : "";
    }

    public string GetDescriptionFieldInitialization()
    {
        return GenerateDescription ? "Description = description;" : "";
    }

    public string GetClassConstruction(EnumFieldData member)
    {
        var construction = GenerateDescription
            ? $"new {ClassName}({member.FullName}, {CodeLiteral.String(member.Description ?? member.Name)})"
            : $"new {ClassName}({member.FullName})";

        if (HasAttributeProperties is false || member.AttributeAssignments.Count == 0)
        {
            return construction;
        }

        var assignments = new StringBuilder();
        for (var i = 0; i < member.AttributeAssignments.Count; i++)
        {
            if (i > 0)
            {
                assignments.Append(", ");
            }

            var assignment = member.AttributeAssignments[i];
            assignments.Append(assignment.PropertyName);
            assignments.Append(" = ");
            assignments.Append(assignment.CreationExpression ?? "null");
        }

        return $"{construction} {{ {assignments} }}";
    }

    public IEnumerable<string> GetAttributePropertyDeclarationLines()
    {
        foreach (var property in AttributeProperties)
        {
            yield return AttributeProjectionEmitter.FormatDeclaration(property);
        }
    }

    /// <summary>
    /// Empty must share identity with the map entry for default(TEnum) when that value is a named member;
    /// otherwise a single shared instance for default(TEnum). Emitted next to ValueByNameMap (Constants file).
    /// </summary>
    public string GetEmptyItemDefinition()
    {
        var defaultField = DefaultEnumField;
        if (defaultField is not null)
        {
            return $"public static {ClassName} Empty {{ get; }} = ValueByNameMap[{CodeLiteral.String(defaultField.Name)}];";
        }

        return GenerateDescription
            ? $"public static {ClassName} Empty {{ get; }} = new {ClassName}(default({EnumTypeFullName}), {CodeLiteral.String(string.Empty)});"
            : $"public static {ClassName} Empty {{ get; }} = new {ClassName}(default({EnumTypeFullName}));";
    }

    public string GetPartialDeclarationWithInterfaces()
    {
        if (IsRecord)
        {
            return ClassPartialDeclaration;
        }

        return $"{ClassPartialDeclaration} : global::System.IEquatable<{ClassName}>";
    }
}
