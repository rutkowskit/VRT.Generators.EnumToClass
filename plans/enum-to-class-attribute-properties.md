# EnumToClass — project selected enum-member attributes as instance properties

Opt-in generation of **nullable properties** on the smart-enum host for **explicitly selected** attribute types. Each instance holds the attribute object reconstructed for **that** enum member (or `null` if the member does not carry it). **No breaking change** for existing consumers.

## Locked decisions (do not re-litigate)

1. **API shape:** generic attribute, **`AllowMultiple = true`** — not a single attribute with `Type[]` allow-list.
2. **Type name:** `EnumToClassPropertyAttribute<TAttribute>`  
   - Usage: `[EnumToClassProperty<Metadata1Attribute>]`  
   - Constraint: `where TAttribute : System.Attribute`
3. **Optional `Name`:** named argument sets the generated property name.  
   - If omitted: property name = **short name of `TAttribute` without trailing `"Attribute"`** (if no such suffix, use the short name as-is).
4. **Opt-in only:** properties appear only for types listed via one or more `[EnumToClassProperty<...>]` on the host. No “project all attributes” in v1.
5. **Ctor stays slim:** attribute values assigned via **object initializer** + `{ get; private init; }` (or `private set`), not constructor parameters.
6. **Equality unchanged:** still by `Value` only; attribute properties do not affect `Equals` / `==`.
7. **Separate attribute** (not a flag on `EnumToClassAttribute<T>`) so the main marker does not bloat.

## Feasibility

**Yes** — Roslyn `AttributeData` on enum fields + emit `new TAttribute(...)` in map initializers.

| Concern | Verdict |
|---------|---------|
| Which types to project | Host applications of `EnumToClassPropertyAttribute<T>` |
| Per-member value | Match `T` on that field’s `GetAttributes()`; else `null` |
| Reconstruct instance | Constructor + named typed constants → C# expression |
| Accessibility | Property accessibility mirrors attribute type (`public` / `internal` / …) |
| Non-breaking | Feature off unless `[EnumToClassProperty<>]` present |

### Hard limits (document + diagnostics)

1. **Reconstructible attributes only** — accessible ctor + typed constants (primitives, `string`, `typeof`, enums, arrays). Otherwise diagnostic and skip that projection (or fail that property).
2. **Same `TAttribute` twice on host** → **error** (duplicate projection).
3. **Property name collision** (two projections → same `Name` / default name) → **error**.
4. **Multiple applications of `T` on one enum member** → v1: **first wins** (optional diagnostic later).
5. **Nested hosts** — still ETC003; this feature does not unlock nested types.
6. No default “exclude System.*” filter needed for v1: user only gets what they explicitly request via `T`.

## API (generated post-init, same Conditional pattern as today)

```csharp
namespace VRT.Generators.EnumToClass;

/// <summary>
/// Projects attribute <typeparamref name="TAttribute"/> from each enum member
/// onto a nullable property of the EnumToClass host.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[Conditional("ENUM_TO_CLASS_GENERATOR_ATTRIBUTES")]
internal sealed class EnumToClassPropertyAttribute<TAttribute> : Attribute
    where TAttribute : Attribute
{
    /// <summary>
    /// Generated property name. Default: type name of TAttribute without "Attribute" suffix.
    /// </summary>
    public string? Name { get; set; }
}
```

### Usage example

```csharp
enum TestEnum
{
    [Metadata1]
    [Metadata2]
    Element2 = 0,

    [Metadata2]
    Element1 = 1,
}

[EnumToClass<TestEnum>]
[EnumToClassProperty<Metadata1Attribute>]
[EnumToClassProperty<Metadata2Attribute>(Name = "Meta2")]
public sealed partial class TestEnumClass { }
```

### Generated shape (illustrative)

```csharp
public sealed partial class TestEnumClass
{
    private TestEnumClass(TestEnum value) { /* Name, Value, IsEmpty only */ }

    public Metadata1Attribute? Metadata1 { get; private init; }  // default name
    public Metadata2Attribute? Meta2 { get; private init; }       // Name = "Meta2"

    // ValueByNameMap:
    // Element2 → Metadata1 = new Metadata1Attribute(...), Meta2 = new Metadata2Attribute(...)
    // Element1 → Metadata1 = null, Meta2 = new Metadata2Attribute(...)
}
```

Map construction (ctor not bloated):

```csharp
["Element2"] = new TestEnumClass(TestEnum.Element2)
{
    Metadata1 = new global::MyNs.Metadata1Attribute(/* from AttributeData */),
    Meta2 = new global::MyNs.Metadata2Attribute(),
},
```

### Default property name algorithm

```
shortName = TAttribute.Name  // metadata/short name, not namespace
if shortName ends with "Attribute" and length > "Attribute".Length
    propertyName = shortName without that suffix
else
    propertyName = shortName
if Name is non-null/non-whitespace
    propertyName = Name.Trim()
// validate propertyName is a valid C# identifier → diagnostic if not
```

## Implementation outline

### Model (`EnumToClassData` / related)

- Read all `EnumToClassPropertyAttribute\`1` applications on the host (`GetAttributes` / metadata name).
- For each: `TAttribute` full name, accessibility, resolved property name, constructibility.
- Per enum field: for each projection, creation expression string or null.
- Incremental-safe: strings only in cached model.

### Codegen

1. Property declarations next to `Description` (Constructors partial).
2. Extend map / `GetClassConstruction` with object initializers when any projection exists.
3. Helper: `AttributeData` → C# `new` expression (`CodeLiteral` for strings).
4. Empty: use default member’s attribute payload when Empty aliases map entry; else nulls.

### Diagnostics (suggested)

| Id | When |
|----|------|
| ETC010 | `TAttribute` not constructible / inaccessible from host |
| ETC011 | Duplicate `EnumToClassProperty<T>` for same `T` on host |
| ETC012 | Property name collision or invalid identifier |
| ETC013 | (optional) Multiple `T` on one enum member — first used |

Register in `AnalyzerReleases.Unshipped.md` when implementing.

### Tests

- Integration: member with both / one / none of two attributes; custom `Name`.
- Default naming: `FooAttribute` → property `Foo`.
- Snapshot: properties + map initializers.
- Accessibility: `internal` attribute type → `internal` property.
- Regression: no `[EnumToClassProperty<>]` → unchanged output vs current snapshots.
- Diagnostics: duplicate `T`, name collision.

### Docs

- README usage + changelog; equality note; reconstructibility limits.

## Non-goals (v1)

- Nested host generation.
- Flags-aware parsing.
- JsonConverter / TypeConverter.
- `params Type[]` / “project all member attributes”.
- Attribute properties participating in equality.
- Emitting C# attributes on generated members (only **values as properties**).

## Breaking-change analysis

| Change | Breaking? |
|--------|-----------|
| New optional `[EnumToClassProperty<T>]` (AllowMultiple) | **No** |
| Properties only when that attribute is present | **No** |
| New diagnostics when feature misused | **No** |

## For Future Agents

Mark checkboxes; write Phase Summary when complete; **propose commit message only** (never `git commit`). Update `AGENTS.md` when implementing.

When **this entire plan** is Complete: remove plan details/phase noise from `AGENTS.md`; keep only backlog leftovers with a link to this plan file if needed (see AGENTS process: plans vs this file).

## Phase 1: Attribute definition + model
Status: Not started

- [ ] Emit `EnumToClassPropertyAttribute<TAttribute>` via post-initialization (`Conditional`, same namespace as `EnumToClassAttribute`).
- [ ] Discover host applications; resolve `T`, `Name`, default property name (strip `Attribute`).
- [ ] Validate duplicates / name collisions / invalid identifiers → diagnostics (may land fully in Phase 3).
- [ ] Per enum member: find matching `AttributeData` for each `T`; store creation expr or null.
- [ ] Wire into `EnumToClassData` without changing behavior when no projections.

### Verification Plan
- `dotnet test` — existing suite green; no snapshot churn for hosts without the new attribute.

### Phase Summary
_(write when phase completes)_

## Phase 2: Codegen
Status: Not started

- [ ] Emit nullable properties (`private init` / `private set`) with correct accessibility.
- [ ] Map entries: `new Host(enumValue) { Prop = ..., ... }` (and description overload unchanged).
- [ ] Empty / flyweight still correct with initializers.
- [ ] Integration + snapshot tests for the metadata example and `Name` override.

### Verification Plan
- Integration asserts property values/nulls; snapshots match expected API.
- `dotnet test -c Release` green.

### Phase Summary
_(write when phase completes)_

## Phase 3: Diagnostics polish + docs + release tracking
Status: Not started

- [ ] Finalize ETC010–012 (013 optional); Unshipped analyzer releases.
- [ ] README + changelog; update AGENTS “planned feature” → implemented when done.
- [ ] Edge cases: non-constructible attribute, internal attribute type.

### Verification Plan
- `dotnet test -c Release`; pack analyzer-only.
- Snapshot or diagnostic test for at least one error id.

### Phase Summary
_(write when phase completes)_

## Final Recap
_(write when all phases complete)_

## Deployment Plan
_(write when all phases complete)_
