namespace VRT.Generators.EnumToClass;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("ENUM_TO_CLASS_GENERATOR_ATTRIBUTES")]
internal sealed class EnumToClassAttribute<T> : global::System.Attribute
    where T : global::System.Enum
{
}