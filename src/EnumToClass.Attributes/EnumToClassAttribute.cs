﻿namespace VRT.Generators.EnumToClass;

/// <summary>
/// Generates const string values for each enum field.
/// Makes class/record decorated with the attribute a closed type.
/// </summary>
/// <typeparam name="T">Enum type parameter</typeparam>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("EnumToClassGenerator_Attributes")]
internal sealed class EnumToClassAttribute<T> : global::System.Attribute
    where T : global::System.Enum
{
    public bool WithDescription { get; set; }
}