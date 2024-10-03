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

        var result = new EnumToClassData
        {
            EnumTypeFullName = enumTypeSymbol.ToDisplayString(),
            EnumTypeUnderlyingTypeName = enumTypeSymbol.EnumUnderlyingType?.ToDisplayString() ?? "int",
            ClassNamespace = classWithAttribute.ContainingNamespace.ToDisplayString(),
            ClassName = classWithAttribute.Name,
            GenerateDescription = attributeData
                .TryGetNamedArgument<bool>(EnumToClassAttributeDefinition.WithDescriptionPropertyName, out var withDescription) && withDescription,
            IsRecord = classWithAttribute.IsRecord,
            ClassPartialDeclaration = classWithAttribute.GetPartialDeclaration(),
            _enumFields = enumTypeSymbol
                .GetMembers()
                .Select(EnumFieldData.FromSymbol)
                .Where(f => f is not null)
                .Select(f => f!)
                .ToArray()
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

    public IReadOnlyCollection<EnumFieldData> GetEnumFields() => _enumFields;
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
    public string GetClassContruction(EnumFieldData member)
    {
        return GenerateDescription
            ? $"new {ClassName}({member.FullName},\"{member.Description ?? member.Name}\")"
            : $"new {ClassName}({member.FullName})";
    }
    public string GetEmptyItemDefinition()
    {
        var firstEnumField = GetEnumFields().FirstOrDefault();
        return firstEnumField is null
            ? ""
            : $"public static {ClassName} Empty {{ get; }} = {GetClassContruction(firstEnumField)};";
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
        private static EnumFieldData FromFieldSymbol(IFieldSymbol fieldSymbol)
        {
            var documentationComment = fieldSymbol.GetMemberDocumentationComment();
            var result = new EnumFieldData()
            {
                Name = fieldSymbol.Name,
                FullName = fieldSymbol.ToDisplayString(),
                Description = fieldSymbol.GetDescriptionAttributeValue()
                    ?? GetCommentSummary(documentationComment)
                    ?? fieldSymbol.Name,
                DocumentationComment = documentationComment
            };
            return result;
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
