using EnumToClass.Helpers;
using Microsoft.CodeAnalysis;
using System.Text;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace VRT.Generators;
#pragma warning restore IDE0130 // Namespace does not match folder structure
internal sealed record EnumToClassData
{
    private static readonly SymbolDisplayFormat FullyQualified =
        SymbolDisplayFormat.FullyQualifiedFormat;

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

        var result = new EnumToClassData
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
        return result;
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
        var seenAttributeTypes = new HashSet<string>(StringComparer.Ordinal);
        var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var attr in host.GetAttributes())
        {
            if (!IsEnumToClassPropertyAttribute(attr.AttributeClass))
            {
                continue;
            }

            if (attr.AttributeClass!.TypeArguments.Length != 1 ||
                attr.AttributeClass.TypeArguments[0] is not INamedTypeSymbol attributeType)
            {
                continue;
            }

            var attributeTypeFullName = attributeType.ToDisplayString(FullyQualified);
            if (!seenAttributeTypes.Add(attributeTypeFullName))
            {
                diagnostics.Add(new PendingDiagnostic(
                    EnumToClassDiagnostics.DuplicateAttributeProperty.Id,
                    attributeType.ToDisplayString()));
                continue;
            }

            var propertyName = ResolvePropertyName(attr, attributeType.Name);
            if (!PropertyNameHelper.IsValidIdentifier(propertyName))
            {
                diagnostics.Add(new PendingDiagnostic(
                    EnumToClassDiagnostics.AttributePropertyNameInvalid.Id,
                    $"Property name '{propertyName}' for attribute '{attributeType.ToDisplayString()}' is not a valid C# identifier."));
                continue;
            }

            if (!seenPropertyNames.Add(propertyName))
            {
                diagnostics.Add(new PendingDiagnostic(
                    EnumToClassDiagnostics.AttributePropertyNameInvalid.Id,
                    $"Property name '{propertyName}' is used by more than one EnumToClassPropertyAttribute."));
                continue;
            }

            projections.Add(new AttributePropertyProjection(
                attributeTypeFullName: attributeTypeFullName,
                propertyTypeDisplayName: attributeTypeFullName,
                propertyName: propertyName,
                accessibility: attributeType.GetAccessibility() is { Length: > 0 } acc ? acc : "public"));
        }

        return (projections, diagnostics);
    }

    private static string ResolvePropertyName(AttributeData enumToClassPropertyAttr, string attributeTypeName)
    {
        if (enumToClassPropertyAttr.TryGetNamedArgument<string>(
                EnumToClassAttributeDefinition.PropertyAttributeNamePropertyName,
                out var name) &&
            string.IsNullOrWhiteSpace(name) is false)
        {
            return name.Trim();
        }

        return PropertyNameHelper.FromAttributeTypeName(attributeTypeName);
    }

    private static bool IsEnumToClassPropertyAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass is null)
        {
            return false;
        }

        return attributeClass.Name == EnumToClassAttributeDefinition.PropertyAttributeTypeName &&
               attributeClass.ContainingNamespace.ToDisplayString() == EnumToClassAttributeDefinition.NamespaceName &&
               attributeClass.Arity == 1;
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
        return GenerateDescription
            ? $"new {ClassName}({member.FullName}, {CodeLiteral.String(member.Description ?? member.Name)})"
            : $"new {ClassName}({member.FullName})";
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

        // No named member for default — one shared instance (not duplicated on each miss).
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

        // IEquatable on the generated partial so ==/Equals share a single contract for class hosts.
        return $"{ClassPartialDeclaration} : global::System.IEquatable<{ClassName}>";
    }

    /// <summary>
    /// One <c>[EnumToClassProperty&lt;T&gt;]</c> projection on the host.
    /// </summary>
    public sealed class AttributePropertyProjection
    {
        public AttributePropertyProjection(
            string attributeTypeFullName,
            string propertyTypeDisplayName,
            string propertyName,
            string accessibility)
        {
            AttributeTypeFullName = attributeTypeFullName;
            PropertyTypeDisplayName = propertyTypeDisplayName;
            PropertyName = propertyName;
            Accessibility = accessibility;
        }

        public string AttributeTypeFullName { get; }
        public string PropertyTypeDisplayName { get; }
        public string PropertyName { get; }
        public string Accessibility { get; }
    }

    /// <summary>
    /// Per-member value for a projected attribute property (<see cref="CreationExpression"/> null ⇒ assign null).
    /// </summary>
    public sealed class AttributePropertyAssignment
    {
        public AttributePropertyAssignment(string propertyName, string? creationExpression)
        {
            PropertyName = propertyName;
            CreationExpression = creationExpression;
        }

        public string PropertyName { get; }
        public string? CreationExpression { get; }
    }

    public sealed class PendingDiagnostic
    {
        public PendingDiagnostic(string descriptorId, params string[] messageArgs)
        {
            DescriptorId = descriptorId;
            MessageArgs = messageArgs ?? [];
        }

        public string DescriptorId { get; }
        public string[] MessageArgs { get; }
    }

    public sealed record EnumFieldData
    {
        private IReadOnlyList<AttributePropertyAssignment> _attributeAssignments = [];

        public static EnumFieldData? FromSymbol(
            ISymbol symbol,
            IReadOnlyList<AttributePropertyProjection> projections,
            List<PendingDiagnostic> diagnostics)
        {
            return symbol switch
            {
                IFieldSymbol fieldSymbol => FromFieldSymbol(fieldSymbol, projections, diagnostics),
                _ => null
            };
        }

        public string Name { get; private set; } = default!;
        public string FullName { get; private set; } = default!;
        public string? Description { get; private set; }
        public string? DocumentationComment { get; private set; }
        public bool IsDefaultValue { get; private set; }
        public IReadOnlyList<AttributePropertyAssignment> AttributeAssignments => _attributeAssignments;

        private static EnumFieldData? FromFieldSymbol(
            IFieldSymbol fieldSymbol,
            IReadOnlyList<AttributePropertyProjection> projections,
            List<PendingDiagnostic> diagnostics)
        {
            // Enum members only (excludes instance field value__ from metadata enums).
            if (fieldSymbol.IsStatic is false || fieldSymbol.HasConstantValue is false)
            {
                return null;
            }

            var documentationComment = fieldSymbol.GetMemberDocumentationComment();
            var result = new EnumFieldData()
            {
                Name = fieldSymbol.Name,
                FullName = fieldSymbol.ToDisplayString(),
                Description = fieldSymbol.GetDescriptionAttributeValue()
                    ?? GetCommentSummary(documentationComment)
                    ?? fieldSymbol.Name,
                DocumentationComment = documentationComment,
                IsDefaultValue = IsDefaultEnumConstant(fieldSymbol.ConstantValue),
                _attributeAssignments = BuildAttributeAssignments(fieldSymbol, projections, diagnostics)
            };
            return result;
        }

        private static IReadOnlyList<AttributePropertyAssignment> BuildAttributeAssignments(
            IFieldSymbol fieldSymbol,
            IReadOnlyList<AttributePropertyProjection> projections,
            List<PendingDiagnostic> diagnostics)
        {
            if (projections.Count == 0)
            {
                return [];
            }

            var fieldAttributes = fieldSymbol.GetAttributes();
            var list = new List<AttributePropertyAssignment>(projections.Count);

            foreach (var projection in projections)
            {
                AttributeData? match = null;
                foreach (var candidate in fieldAttributes)
                {
                    if (candidate.AttributeClass is null)
                    {
                        continue;
                    }

                    if (candidate.AttributeClass.ToDisplayString(FullyQualified) == projection.AttributeTypeFullName)
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match is null)
                {
                    list.Add(new AttributePropertyAssignment(projection.PropertyName, creationExpression: null));
                    continue;
                }

                if (AttributeConstructionEmitter.TryFormat(match, out var expression, out var error))
                {
                    list.Add(new AttributePropertyAssignment(projection.PropertyName, expression));
                }
                else
                {
                    diagnostics.Add(new PendingDiagnostic(
                        EnumToClassDiagnostics.AttributePropertyNotConstructible.Id,
                        projection.AttributeTypeFullName,
                        error ?? "unknown error"));
                    list.Add(new AttributePropertyAssignment(projection.PropertyName, creationExpression: null));
                }
            }

            return list;
        }

        private static bool IsDefaultEnumConstant(object? constantValue)
        {
            return constantValue switch
            {
                null => false,
                byte b => b == 0,
                sbyte sb => sb == 0,
                short s => s == 0,
                ushort us => us == 0,
                int i => i == 0,
                uint ui => ui == 0,
                long l => l == 0,
                ulong ul => ul == 0,
                char c => c == 0,
                _ => false
            };
        }

        private static readonly char[] NewLineChars = { '\r', '\n' };

        /// <summary>
        /// Extracts full <c>&lt;summary&gt;</c> text (all lines), not only the first line.
        /// </summary>
        private static string? GetCommentSummary(string? documentationComment)
        {
            if (string.IsNullOrWhiteSpace(documentationComment))
            {
                return null;
            }

            var lines = documentationComment!.Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            var inSummary = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = StripDocPrefix(lines[i]);
                if (!inSummary)
                {
                    const string open = "<summary>";
                    var openIndex = IndexOfIgnoreCase(line, open);
                    if (openIndex < 0)
                    {
                        continue;
                    }

                    inSummary = true;
                    var afterOpen = line.Substring(openIndex + open.Length).Trim();
                    if (TryTakeUntilCloseSummary(afterOpen, sb))
                    {
                        break;
                    }

                    AppendSummaryLine(sb, afterOpen);
                    continue;
                }

                if (TryTakeUntilCloseSummary(line, sb))
                {
                    break;
                }

                AppendSummaryLine(sb, line);
            }

            return sb.Length == 0 ? null : sb.ToString();
        }

        private static string StripDocPrefix(string rawLine)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("///", StringComparison.Ordinal))
            {
                return line.Substring(3).TrimStart();
            }

            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                return line.Substring(2).TrimStart();
            }

            return line;
        }

        /// <summary>
        /// If <paramref name="line"/> contains <c>&lt;/summary&gt;</c>, append text before it and return true (done).
        /// </summary>
        private static bool TryTakeUntilCloseSummary(string line, StringBuilder sb)
        {
            const string close = "</summary>";
            var closeIndex = IndexOfIgnoreCase(line, close);
            if (closeIndex < 0)
            {
                return false;
            }

            AppendSummaryLine(sb, line.Substring(0, closeIndex).TrimEnd());
            return true;
        }

        private static void AppendSummaryLine(StringBuilder sb, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(line.Trim());
        }

        private static int IndexOfIgnoreCase(string text, string value)
            => text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
    };
}
