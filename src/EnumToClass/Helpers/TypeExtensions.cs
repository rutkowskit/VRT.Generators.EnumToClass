namespace EnumToClass.Helpers;

internal static class TypeExtensions
{
    public static string GetSimpleTypeName(this Type type)
        => type.IsGenericType ? type.Name.Split('`')[0] : type.Name;
}
