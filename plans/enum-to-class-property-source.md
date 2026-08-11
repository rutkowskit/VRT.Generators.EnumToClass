# EnumToClassProperty — project a single attribute member value (not the whole attribute)

Optional parameter on `[EnumToClassProperty<TAttribute>]` that selects **which member of the attribute** becomes the host property value (e.g. `PermissionAttribute.Name` → `string` on the smart-enum instance).

## Goal (example)

```csharp
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class PermissionAttribute : Attribute
{
    public PermissionAttribute(string name) => Name = name;
    public string Name { get; }
}

public enum RoleTypes
{
    [Permission("Orders.Edit")]
    [Permission("Orders.View")]
    Editor,
}

[EnumToClass<RoleTypes>]
[EnumToClassProperty<PermissionAttribute>(AsArray = true, Name = "Permissions", Source = "Name")]
// or ForProperty / Member / From — name TBD
public sealed partial class RoleTypeClass { }

// Desired:
// public string[] Permissions { get; private init; }
// Editor → Permissions = new string[] { "Orders.Edit", "Orders.View" }
// without Source: PermissionAttribute[] as today
```

## Feasibility

**Yes — low-to-medium invasiveness** for **flat, single segment** names (`"Name"`).

Roslyn already exposes constructor args + named args as `TypedConstant` on `AttributeData`. We do **not** need a runtime instance of the attribute to read `Name` if we resolve the value from metadata the same way we already format constants in `AttributeConstructionEmitter`.

Nested paths (`"Foo.Bar"`) are **harder** (need property chain on constructed attribute or nested typed constants — attributes rarely have nested objects as property types that are themselves attribute-like). **v1 recommendation: single identifier only.**

## Invasiveness assessment

| Area | Impact | Notes |
|------|--------|--------|
| Generated attribute API | **Small** | New optional `string? Source` (or `Member` / `From`); default `null` = current behavior (whole attribute). **Non-breaking.** |
| `AttributePropertyProjection` model | **Small** | Add `SourceMemberName?`, change meaning of `PropertyTypeDisplayName` (element type of property: attribute type vs member type). |
| `BuildOneAssignment` / multi | **Medium** | Today: emit `new TAttribute(...)`. With Source: emit **value expression only** (`"Orders.Edit"`), or for array `new string[] { "...", "..." }`. |
| `AttributeConstructionEmitter` | **Medium** | New API: `TryFormatMemberValue(AttributeData, string memberName, out expression)` — resolve member from named args **or** constructor parameter (by parameter name / `[Caller]` / positional match). Reuse `TryFormatTypedConstant`. |
| Property declaration codegen | **Small** | Type becomes `string?` / `string[]` instead of `PermissionAttribute?` / `PermissionAttribute[]`; accessibility: use **member** accessibility if public get, else attribute type accessibility (usually public). |
| Default host property `Name` | **Small / design** | Still user `Name` or strip `Attribute`. With Source, default host name could stay as today (`Permission`) or switch to source member name (`Name`) — **recommend keep current default** (strip Attribute); user sets `Name = "Permissions"`. |
| Diagnostics | **Small** | New: source member not found / not a property or ctor param / not constant-encodable (ETC014?). |
| Tests | **Medium** | Integration Role/Permission with Source; snapshot; regression without Source. |
| README / AGENTS | **Small** | Docs only. |

**Overall:** ~1 focused PR; touches mainly `EnumToClassAttributeDefinition`, `EnumToClassData` (projection + assignment builders), `AttributeConstructionEmitter` (+ maybe thin helper), tests, docs. **No change** to core enum map, Empty, equality, or `EnumToClassAttribute<T>`.

### Hard parts (design, not architecture blockers)

1. **Ctor parameter vs property**  
   `[Permission("Orders.Edit")]` often sets ctor param `name` mapped to property `Name`. Metadata may expose **named argument** `Name` after compilation, or only **positional** ctor arg. Resolver must:
   - prefer named argument matching source name (case-sensitive C#),
   - else match constructor parameter name (Roslyn `AttributeConstructor.Parameters`),
   - else fail diagnostic.

2. **Fields vs properties** on attribute type  
   Support **public properties** first; optional public fields. Ignore methods/indexers.

3. **`AsArray` + Source**  
   Natural: `string[]` of each application’s member value. Empty → `Array.Empty<string>()`.

4. **Without AsArray + Source**  
   `string?` — first application’s member value or null.

5. **Nested path (`"A.B"`)**  
   **Out of v1.** Would need recursive typed constant or emit `(new TAttribute(...)).A.B` which may not be valid if A is not accessible at compile time the same way / if only ctor-set. Defer unless proven need.

6. **Nullable reference types**  
   Element type from symbol (`ITypeSymbol`); append `?` for single reference types when appropriate; arrays non-null with empty default (same as AsArray today).

## Naming recommendation for the parameter

| Candidate | Pros | Cons |
|-----------|------|------|
| **`Source`** | Short; “source of the projected value” | Slightly vague |
| **`Member`** | Matches “member of attribute type” | Could mean enum member |
| **`From`** | Fluent English | Too vague alone |
| **`ForProperty`** | Matches user wording | Implies only properties, not ctor params |
| **`Path`** | Allows nested later | Overpromises if v1 is flat only |

**Recommendation:** **`Source`** (v1 = single C# identifier). Document as: property or constructor parameter name on `TAttribute`. If nested paths are added later, `Source` can accept `"A.B"` without rename.

## Locked proposal (for implementation when approved)

```csharp
public string? Name { get; set; }
public bool AsArray { get; set; }
/// <summary>
/// When set, project this member of TAttribute (property or ctor parameter) instead of the attribute instance.
/// Single segment only (e.g. "Name").
/// </summary>
public string? Source { get; set; }
```

Semantics matrix:

| AsArray | Source | Host property type | Value |
|---------|--------|--------------------|--------|
| false | null | `TAttribute?` | instance / null |
| true | null | `TAttribute[]` | instances / empty |
| false | `"Name"` | `typeof(Name)?` (e.g. `string?`) | member value / null |
| true | `"Name"` | `typeof(Name)[]` (e.g. `string[]`) | values / empty |

## Non-goals (v1)

- Nested `Source` paths.
- Instance methods / computed properties without constant backing in metadata.
- Changing equality to include projected values.

## Phases (when executing)

### Phase 1: API + resolve member value from AttributeData
Status: Not started

- [ ] Add `Source` to generated `EnumToClassPropertyAttribute<T>`.
- [ ] Plumb into `AttributePropertyProjection` (`SourceMemberName`, element type FQN).
- [ ] `TryFormatAttributeMemberValue` in emitter (named arg / ctor param).
- [ ] Diagnostics if Source invalid.
- [ ] Existing tests green when Source omitted.

### Phase 2: Codegen + tests + docs
Status: Not started

- [ ] Declaration / assignment paths for Source ± AsArray.
- [ ] Integration + snapshot (Permission Name → string / string[]).
- [ ] README + AGENTS product decision.

### Verification Plan
- `dotnet test -c Release`
- Regression: all current AttributeProperties tests without Source unchanged in shape.

## For Future Agents

Propose commit messages only; never commit. On full plan complete, strip plan noise from `AGENTS.md` per process rules.

## Final Recap
_(when done)_

## Deployment Plan
_(when done)_
