using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using System.Text;

namespace EnumToClass.Helpers;

/// <summary>
/// Formats <see cref="AttributeData"/> as a C# <c>new AttributeType(...)</c> expression.
/// </summary>
internal static class AttributeConstructionEmitter
{
    public static bool TryFormat(AttributeData attributeData, out string expression, out string? error)
    {
        expression = string.Empty;
        error = null;

        if (attributeData.AttributeClass is null)
        {
            error = "Attribute type is missing.";
            return false;
        }

        if (attributeData.AttributeConstructor is null && attributeData.ConstructorArguments.Length > 0)
        {
            error = "Attribute constructor could not be resolved.";
            return false;
        }

        var typeName = attributeData.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sb = new StringBuilder();
        sb.Append("new ");
        sb.Append(typeName);
        sb.Append('(');

        for (var i = 0; i < attributeData.ConstructorArguments.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            if (!TryFormatTypedConstant(attributeData.ConstructorArguments[i], out var argExpr, out error))
            {
                return false;
            }

            sb.Append(argExpr);
        }

        sb.Append(')');

        if (attributeData.NamedArguments.Length > 0)
        {
            sb.Append(" { ");
            for (var i = 0; i < attributeData.NamedArguments.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                var named = attributeData.NamedArguments[i];
                if (!TryFormatTypedConstant(named.Value, out var valueExpr, out error))
                {
                    return false;
                }

                sb.Append(named.Key);
                sb.Append(" = ");
                sb.Append(valueExpr);
            }

            sb.Append(" }");
        }

        expression = sb.ToString();
        return true;
    }

    /// <summary>
    /// Formats a single member value of an attribute application (named argument or ctor parameter).
    /// <paramref name="sourceMemberName"/> is a single identifier (property or ctor parameter name).
    /// </summary>
    public static bool TryFormatMemberValue(
        AttributeData attributeData,
        string sourceMemberName,
        out string expression,
        out string? error)
    {
        expression = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(sourceMemberName) || sourceMemberName.Contains('.'))
        {
            error = "Source must be a single member name (nested paths are not supported).";
            return false;
        }

        // Prefer named arguments (property / named ctor arg as applied in source).
        foreach (var named in attributeData.NamedArguments)
        {
            if (string.Equals(named.Key, sourceMemberName, StringComparison.Ordinal))
            {
                return TryFormatTypedConstant(named.Value, out expression, out error);
            }
        }

        // Fall back to constructor parameter name → positional argument.
        var ctor = attributeData.AttributeConstructor;
        if (ctor is not null)
        {
            for (var i = 0; i < ctor.Parameters.Length && i < attributeData.ConstructorArguments.Length; i++)
            {
                if (string.Equals(ctor.Parameters[i].Name, sourceMemberName, StringComparison.Ordinal))
                {
                    return TryFormatTypedConstant(attributeData.ConstructorArguments[i], out expression, out error);
                }
            }
        }

        // Case-insensitive fallback for ctor parameter names (C# often uses camelCase params).
        if (ctor is not null)
        {
            for (var i = 0; i < ctor.Parameters.Length && i < attributeData.ConstructorArguments.Length; i++)
            {
                if (string.Equals(ctor.Parameters[i].Name, sourceMemberName, StringComparison.OrdinalIgnoreCase))
                {
                    return TryFormatTypedConstant(attributeData.ConstructorArguments[i], out expression, out error);
                }
            }
        }

        error = $"Member '{sourceMemberName}' was not found as a named argument or constructor parameter on the attribute application.";
        return false;
    }

    /// <summary>
    /// Resolves the type of a public property or field on <paramref name="attributeType"/> named <paramref name="sourceMemberName"/>.
    /// </summary>
    public static bool TryResolveSourceMemberType(
        INamedTypeSymbol attributeType,
        string sourceMemberName,
        out ITypeSymbol memberType,
        out string? error)
    {
        memberType = null!;
        error = null;

        if (string.IsNullOrWhiteSpace(sourceMemberName) || sourceMemberName.Contains('.'))
        {
            error = "Source must be a single member name (nested paths are not supported).";
            return false;
        }

        foreach (var member in attributeType.GetMembers(sourceMemberName))
        {
            if (member is IPropertySymbol { GetMethod: not null } property &&
                property.Parameters.Length == 0 &&
                property.DeclaredAccessibility != Accessibility.Private)
            {
                memberType = property.Type;
                return true;
            }

            if (member is IFieldSymbol { IsConst: false, IsStatic: false } field &&
                field.DeclaredAccessibility != Accessibility.Private)
            {
                memberType = field.Type;
                return true;
            }
        }

        // Ctor parameter only (no matching public property/field) — type from parameter.
        foreach (var ctor in attributeType.Constructors)
        {
            foreach (var parameter in ctor.Parameters)
            {
                if (string.Equals(parameter.Name, sourceMemberName, StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, sourceMemberName, StringComparison.OrdinalIgnoreCase))
                {
                    memberType = parameter.Type;
                    return true;
                }
            }
        }

        error = $"Type '{attributeType.ToDisplayString()}' has no accessible property, field, or constructor parameter named '{sourceMemberName}'.";
        return false;
    }

    private static bool TryFormatTypedConstant(TypedConstant constant, out string expression, out string? error)
    {
        expression = string.Empty;
        error = null;

        if (constant.IsNull)
        {
            expression = "null";
            return true;
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                expression = FormatPrimitive(constant.Value);
                return true;

            case TypedConstantKind.Enum:
                return TryFormatEnum(constant, out expression, out error);

            case TypedConstantKind.Type:
                if (constant.Value is ITypeSymbol typeSymbol)
                {
                    expression = $"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
                    return true;
                }

                error = "Type constant is invalid.";
                return false;

            case TypedConstantKind.Array:
                return TryFormatArray(constant, out expression, out error);

            default:
                error = "Attribute argument could not be encoded.";
                return false;
        }
    }

    private static bool TryFormatEnum(TypedConstant constant, out string expression, out string? error)
    {
        expression = string.Empty;
        error = null;

        if (constant.Type is null || constant.Value is null)
        {
            error = "Enum attribute argument is invalid.";
            return false;
        }

        var enumType = constant.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        expression = $"({enumType}){FormatPrimitive(constant.Value)}";
        return true;
    }

    private static bool TryFormatArray(TypedConstant constant, out string expression, out string? error)
    {
        expression = string.Empty;
        error = null;

        if (constant.Values.IsDefault)
        {
            error = "Array attribute argument is invalid.";
            return false;
        }

        var elementType = constant.Type is IArrayTypeSymbol arrayType
            ? arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : "object";

        var sb = new StringBuilder();
        sb.Append("new ");
        sb.Append(elementType);
        sb.Append("[] { ");

        for (var i = 0; i < constant.Values.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            if (!TryFormatTypedConstant(constant.Values[i], out var elementExpr, out error))
            {
                return false;
            }

            sb.Append(elementExpr);
        }

        sb.Append(" }");
        expression = sb.ToString();
        return true;
    }

    private static string FormatPrimitive(object? value)
    {
        return value switch
        {
            null => "null",
            string s => CodeLiteral.String(s),
            bool b => b ? "true" : "false",
            char c => SymbolDisplay.FormatLiteral(c, quote: true),
            byte or sbyte or short or ushort or int or uint or long or ulong
                => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            float f => f.ToString("G9", CultureInfo.InvariantCulture) + "F",
            double d => d.ToString("G17", CultureInfo.InvariantCulture) + "D",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
            _ => CodeLiteral.String(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
        };
    }
}
