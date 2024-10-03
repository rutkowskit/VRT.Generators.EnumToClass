﻿#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace VRT.Generators;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class EnumToClassAttributeDefinition
{
    public const string NamespaceName = "VRT.Generators.EnumToClass";
    public const string AttributeTypeName = "EnumToClassAttribute";
    public const string WithDescriptionPropertyName = "WithDescription";
    public const string FullyQualifiedName = $"{NamespaceName}.{AttributeTypeName}`1";

    public const string SourceCode =
        $$"""
        //<auto-generated />
        namespace {{NamespaceName}};

        /// <summary>
        /// Generates const string values for each enum field.
        /// Makes class/record decorated with the attribute a closed type.
        /// </summary>
        /// <typeparam name="T">Enum type parameter</typeparam>
        [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
        [global::System.Diagnostics.Conditional("ENUM_TO_CLASS_GENERATOR_ATTRIBUTES")]
        internal sealed class {{AttributeTypeName}}<T> : global::System.Attribute
            where T : global::System.Enum
        {
            public bool WithDescription { get; set; }
        }
        """;
}
