﻿//HintName: EnumToClassAttribute.g.cs
//<auto-generated />
namespace VRT.Generators.EnumToClass;

/// <summary>
/// Generates const string values for each enum field.
/// Makes class/record decorated with the attribute a closed type.
/// </summary>
/// <typeparam name="T">Enum type parameter</typeparam>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("ENUM_TO_CLASS_GENERATOR_ATTRIBUTES")]
internal sealed class EnumToClassAttribute<T> : global::System.Attribute
    where T : global::System.Enum
{
    public bool WithDescription { get; set; }
}