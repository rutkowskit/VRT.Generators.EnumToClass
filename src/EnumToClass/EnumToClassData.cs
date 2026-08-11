using EnumToClass.Helpers;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace VRT.Generators;
#pragma warning restore IDE0130 // Namespace does not match folder structure
internal sealed record EnumToClassData
{
    private IReadOnlyCollection<EnumFieldData> _enumFields = [];
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

        var fields = enumTypeSymbol
            .GetMembers()
            .Select(EnumFieldData.FromSymbol)
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
            Location = classWithAttribute.Locations.FirstOrDefault() ?? Location.None,
            ClassPartialDeclaration = classWithAttribute.GetPartialDeclaration(),
            _enumFields = fields
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
    public Location Location { get; private set; } = Location.None;

    public IReadOnlyCollection<EnumFieldData> GetEnumFields() => _enumFields;

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

    public sealed record EnumFieldData
    {
        public static EnumFieldData? FromSymbol(ISymbol symbol)
        {
            return symbol switch
            {
                IFieldSymbol fieldSymbol => FromFieldSymbol(fieldSymbol),
                _ => null
            };
        }
        public string Name { get; private set; } = default!;
        public string FullName { get; private set; } = default!;
        public string? Description { get; private set; }
        public string? DocumentationComment { get; private set; }
        public bool IsDefaultValue { get; private set; }

        private static EnumFieldData? FromFieldSymbol(IFieldSymbol fieldSymbol)
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
                IsDefaultValue = IsDefaultEnumConstant(fieldSymbol.ConstantValue)
            };
            return result;
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

        private static string? GetCommentSummary(string? documentationComment)
        {
            var result = documentationComment?
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .SkipWhile(l => l.Contains("<summary>") is false)
                .Skip(1)
                .Select(l => l.TrimStart(['/', ' ', '\t']))
                .FirstOrDefault();
            return result;
        }
    };
}
