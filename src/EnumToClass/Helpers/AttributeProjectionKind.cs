namespace EnumToClass.Helpers;

internal enum AttributeProjectionKind
{
    SingleInstance,
    ArrayOfInstances,
    SingleMemberValue,
    ArrayOfMemberValues,
    BooleanPresence,
}

internal static class AttributeProjectionKindResolver
{
    public static bool TryResolve(
        bool asBoolean,
        bool asArray,
        string? sourceMemberName,
        out AttributeProjectionKind kind,
        out string? conflictMessage)
    {
        conflictMessage = null;
        kind = default;

        if (asBoolean && (asArray || sourceMemberName is not null))
        {
            conflictMessage =
                "AsBoolean cannot be combined with AsArray or Source on the same EnumToClassPropertyAttribute.";
            return false;
        }

        if (asBoolean)
        {
            kind = AttributeProjectionKind.BooleanPresence;
            return true;
        }

        if (asArray)
        {
            kind = sourceMemberName is not null
                ? AttributeProjectionKind.ArrayOfMemberValues
                : AttributeProjectionKind.ArrayOfInstances;
            return true;
        }

        kind = sourceMemberName is not null
            ? AttributeProjectionKind.SingleMemberValue
            : AttributeProjectionKind.SingleInstance;
        return true;
    }

    public static string ModeKey(AttributeProjectionKind kind) => kind switch
    {
        AttributeProjectionKind.BooleanPresence => "bool",
        AttributeProjectionKind.ArrayOfInstances or AttributeProjectionKind.ArrayOfMemberValues => "arr",
        _ => "one",
    };

    public static bool StopAfterFirstMatch(AttributeProjectionKind kind) => kind switch
    {
        AttributeProjectionKind.ArrayOfInstances or AttributeProjectionKind.ArrayOfMemberValues => false,
        _ => true,
    };
}
