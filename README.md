# EnumToClass Generator

C# source generator that turns an enum into a **smart-enum style closed class/record**: constant string names, flyweight instances, lookup helpers, and conversions.

The type decorated with `EnumToClassAttribute<TEnum>` becomes a closed type with a private constructor and readonly properties.

## Attribute

```cs
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
```

### Optional properties

1. `WithDescription` — when `true`, generates a `Description` property. Value resolution order:
   1. `DescriptionAttribute` on the enum field, if present
   2. Full text of the XML documentation `<summary>` (all lines, newline-separated), if present
   3. The enum member name

### Projecting enum-member attributes as properties

Opt-in companion attribute (can be applied multiple times):

```cs
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[Conditional("ENUM_TO_CLASS_GENERATOR_ATTRIBUTES")]
internal sealed class EnumToClassPropertyAttribute<TAttribute> : Attribute
    where TAttribute : Attribute
{
    /// <summary>Generated property name. Default: TAttribute name without "Attribute" suffix.</summary>
    public string? Name { get; set; }

    /// <summary>
    /// When true, property type is an array (all applications on the member; empty array if none).
    /// When false (default), first application or null.
    /// </summary>
    public bool AsArray { get; set; }

    /// <summary>
    /// When set, project this member of TAttribute (property or ctor parameter) instead of the attribute instance.
    /// Single segment only (e.g. "Name").
    /// </summary>
    public string? Source { get; set; }
}
```

Example:

```cs
[EnumToClass<TestEnum>]
[EnumToClassProperty<Metadata1Attribute>]
[EnumToClassProperty<Metadata2Attribute>(Name = "Meta2")]
[EnumToClassProperty<PermissionAttribute>(AsArray = true, Name = "Permissions", Source = "Name")]
public sealed partial class TestEnumClass { }
```

Generates properties filled from each enum member’s attributes via object initializers on the static map (constructor stays slim):

| AsArray | Source | Host type | Value |
|---------|--------|-----------|--------|
| false | (null) | `TAttribute?` | instance / null |
| true | (null) | `TAttribute[]` | instances / empty |
| false | `"Name"` | e.g. `string?` | member value / null |
| true | `"Name"` | e.g. `string[]` | values / empty |

Only attributes / member values reconstructible from metadata (public ctor + constant args) are supported. Nested `Source` paths are not supported.

Equality still compares **`Value` only** — projected attribute properties do not participate in `Equals` / `==`.

## Diagnostics

| Id | Severity | Meaning |
|----|----------|---------|
| `ETC001` | Error | Host type is not `partial` |
| `ETC002` | Warning | Enum has no named members |
| `ETC003` | Error | Host type is nested (not supported; generation skipped) |
| `ETC010` | Warning | Attribute type cannot be reconstructed as a property value |
| `ETC011` | Error | Duplicate `EnumToClassProperty<T>` for the same `T` |
| `ETC012` | Error | Invalid or conflicting projected property name |
| `ETC014` | Error | Invalid `Source` member on `EnumToClassProperty` |

## Usage

```cs
namespace EnumToClass.Tests.Integration;

public enum TestElements
{
    None,
    Element1,
    Element2,
    Element3
}

[EnumToClass<TestElements>]
internal sealed partial class TestElementClass
{
}

[EnumToClass<TestElements>(WithDescription = true)]
public sealed partial record TestElementRecord
{
}
```

### Semantics

- **`IsEmpty`**: `true` when `Value == default(TEnum)`.
- **`Empty`**: shared instance for the default enum value. If a named member equals `default(TEnum)`, `Empty` is **the same reference** as that member’s `*Instance` (and map entry). Missing names from `GetByName` return `Empty`.
- **Flyweight**: named members are created once in a static map; conversions and `*Instance` properties return those instances.
- **`GetByName`**: ordinal, case-sensitive name match; unknown name → `Empty`.
- **`TryGetByName`**: returns `false` on miss (and sets `value` to `Empty`).
- **Classes** implement `IEquatable<T>` and `==` / `!=` by `Value`. Records use built-in record equality.
- **`[Flags]` / combined values**: conversion from enum uses `ToString()`; combined flag names are not map keys, so lookup yields `Empty`. Flags-aware parsing is out of scope.
- **Nested host types**: not supported — generator reports **`ETC003`** and skips code generation (partials would be emitted at namespace scope).

## Generated surface (illustrative)

For a partial class host the generator emits (among other members):

- `ValueByNameMap` (private)
- `Empty`, `Name`, `Value`, `IsEmpty`
- `public const string MemberName` and `public static T MemberNameInstance` per enum member
- `GetAll()`, `GetByName`, `TryGetByName`
- Implicit conversions: `string`, `TEnum`, underlying integral type ↔ host type
- When using `[EnumToClassProperty<T>]`: nullable properties for those attribute types

## Change Log

### Version 1.0.8
1. Hardening: Empty flyweight, escaped literals, constant-only enum members, class `IEquatable`/`==`, `TryGetByName`, `ETC001`–`ETC003`, `global::` BCL types, doc indent, full `<summary>` for `Description`.
2. **`EnumToClassPropertyAttribute<T>`**: project selected enum-member attributes as host properties (`Name`, **`AsArray`**, **`Source`**). Diagnostics `ETC010`–`ETC012`, `ETC014`.

### Version 1.0.7
1. Add implicit operator to convert `underlying enum type` value to `Class type`.

### Version 1.0.6
1. Add optional description property generation

### Version 1.0.5
1. Add documentation comments based on `DescriptionAttribute` value.
1. Fix full enum type name for external enums.
1. Fix documentation comments for external enums.

### Version 1.0.4
1. Add documentation comments to generated const values based on documentation comments for the enum element

### Version 1.0.3
1. Fix method names in generator.
1. Replace reflection generated to generator generated values dictionary.
1. Add GetAll() static function to the class with `EnumToClassAttribute` attribute.
1. Add implicit operator to convert enum type value to underlying enum type.
