namespace EnumToClass.Helpers;

/// <summary>One <c>[EnumToClassProperty&lt;T&gt;]</c> projection on the host.</summary>
internal sealed class AttributePropertyProjection
{
    public AttributePropertyProjection(
        AttributeProjectionKind kind,
        string attributeTypeFullName,
        string elementTypeFullName,
        string propertyTypeDisplayName,
        string propertyName,
        string accessibility,
        string? sourceMemberName,
        bool elementIsNullableValueType)
    {
        Kind = kind;
        AttributeTypeFullName = attributeTypeFullName;
        ElementTypeFullName = elementTypeFullName;
        PropertyTypeDisplayName = propertyTypeDisplayName;
        PropertyName = propertyName;
        Accessibility = accessibility;
        SourceMemberName = sourceMemberName;
        ElementIsNullableValueType = elementIsNullableValueType;
    }

    public AttributeProjectionKind Kind { get; }
    public string AttributeTypeFullName { get; }
    public string ElementTypeFullName { get; }
    public string PropertyTypeDisplayName { get; }
    public string PropertyName { get; }
    public string Accessibility { get; }
    public string? SourceMemberName { get; }
    public bool ElementIsNullableValueType { get; }

    public bool UseNullableAnnotation =>
        Kind is AttributeProjectionKind.SingleInstance or AttributeProjectionKind.SingleMemberValue
        && ElementIsNullableValueType is false;
}

/// <summary>
/// Per-member assignment expression for a projected property
/// (null expression ⇒ assign <c>null</c> in single modes).
/// </summary>
internal sealed class AttributePropertyAssignment
{
    public AttributePropertyAssignment(string propertyName, string? creationExpression)
    {
        PropertyName = propertyName;
        CreationExpression = creationExpression;
    }

    public string PropertyName { get; }
    public string? CreationExpression { get; }
}

internal sealed class PendingDiagnostic
{
    public PendingDiagnostic(string descriptorId, params string[] messageArgs)
    {
        DescriptorId = descriptorId;
        MessageArgs = messageArgs ?? [];
    }

    public string DescriptorId { get; }
    public string[] MessageArgs { get; }
}
