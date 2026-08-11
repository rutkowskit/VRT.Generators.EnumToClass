# EnumToClassProperty — presence flag (`AsBoolean` / presence)

Optional mode on `[EnumToClassProperty<TAttribute>]` that emits a **boolean** host property: whether **any** application of `TAttribute` exists on that enum member (not the attribute instance / not `Source` values).

## Goal (example)

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class AdminOnlyAttribute : Attribute { }

public enum FeatureFlags
{
    [AdminOnly]
    Sensitive,
    Public = 1,
}

[EnumToClass<FeatureFlags>]
[EnumToClassProperty<AdminOnlyAttribute>(AsBoolean = true)]
// default Name → "AdminOnly" (strip Attribute); optional Name = "HasAdminOnly"
public sealed partial class FeatureFlagClass { }

// Desired:
// public bool AdminOnly { get; private init; }   // or HasAdminOnly / IsAdminOnlyDefined
// Sensitive → true
// Public    → false
```

With existing value projection still available on a **separate** application (different host `Name`):

```csharp
[EnumToClassProperty<PermissionAttribute>(AsArray = true, Name = "Permissions", Source = "Name")]
[EnumToClassProperty<PermissionAttribute>(AsBoolean = true, Name = "HasPermissions")]
```

## Feasibility & invasiveness

**Very low invasiveness** — simpler than `Source` / `AsArray` value emission.

| Area | Impact | Work |
|------|--------|------|
| Generated attribute API | Small | `bool AsBoolean { get; set; }` default `false` — **non-breaking** |
| Projection model | Small | Flag `AsBoolean`; property type forced to `bool`; ignore Source for value type when AsBoolean |
| Assignment build | **Trivial** | `matches.Count > 0` → `"true"` / `"false"` (no `AttributeConstructionEmitter` for values) |
| Declarations | Small | `bool Name { get; private init; }` (non-nullable; default `false` optional for CS8618) |
| Diagnostics | Small | Mutual exclusion: `AsBoolean` + `AsArray` and/or `AsBoolean` + `Source` → error (ETC015?) |
| Tests / docs | Small | Integration + snapshot + README |

**Overall:** ~half-day PR; no new complex typed-constant logic.

### Interaction matrix (recommended rules)

| AsBoolean | AsArray | Source | Result |
|-----------|---------|--------|--------|
| false | * | * | Current behavior |
| true | false | null | **`bool`** presence |
| true | true | * | **Invalid** (diagnostic) |
| true | * | set | **Invalid** (diagnostic) — presence does not need Source |

Presence for multi-applied attributes (`AllowMultiple` on the attribute type): still **one bool** = “at least one application” (not a count). Counting would be a different feature (`AsCount`).

## Naming

| Candidate | Host property default idea | Notes |
|-----------|----------------------------|--------|
| **`AsBoolean`** | Keep `Name` / strip `Attribute` → `AdminOnly` | Parallel to **`AsArray`** — **recommended parameter name** |
| `IsDefined` | Auto-prefix? `IsAdminOnlyDefined` | XML-serializer vibe; longer; less parallel to AsArray |
| `AsPresence` / `PresenceOnly` | `HasAdminOnly` | Clear but less consistent with As* |

**Default host property name when `AsBoolean` and `Name` omitted:**

1. **v1 simple (recommended):** same as today — strip `Attribute` → `AdminOnly` (user can set `Name = "HasAdminOnly"`).
2. Optional later: auto `Has` + stripped name when AsBoolean — more magic, can surprise.

Do **not** auto-rename unless `Name` is omitted **and** we document it; prefer explicit `Name = "HasAdminOnly"` for XML-style clarity.

## Locked proposal (when implementing)

```csharp
public string? Name { get; set; }
public bool AsArray { get; set; }
public string? Source { get; set; }
/// <summary>
/// When true, emit a bool host property: true if TAttribute is applied on the enum member.
/// Incompatible with AsArray and Source.
/// </summary>
public bool AsBoolean { get; set; }
```

Codegen sketch:

```csharp
public bool AdminOnly { get; private init; } = false;

// map:
["Sensitive"] = new Host(...) { AdminOnly = true },
["Public"] = new Host(...) { AdminOnly = false },
```

## Phases

### Phase 1: API + model + mutual exclusion
Status: Complete

- [x] Add `AsBoolean` to generated attribute + constant name.
- [x] Projection: `AsBoolean` → element/property type `bool`, not attribute type.
- [x] Diagnostics when combined with `AsArray` or `Source` (ETC015).
- [x] Assignment: `true`/`false` from match count (any vs none).

### Phase 2: Codegen polish + tests + docs
Status: Complete

- [x] Declaration `bool`.
- [x] Integration + snapshot + ETC015 conflict snapshot.
- [x] README matrix + AGENTS decision; ETC015 in Unshipped.

### Verification Plan
- `dotnet test -c Release`
- Regression: existing AttributeProperties / Source / AsArray tests.

## Non-goals

- Count of applications (`int`).
- Separate property per application index.
- Nested Source (still out of scope).

## For Future Agents

Propose commit messages only; never `git commit`. On plan complete, clear active plan noise from `AGENTS.md`.

## Final Recap
Delivered **`AsBoolean`**: presence flag for `TAttribute` on enum members as `bool` host property. Mutual exclusion with `AsArray`/`Source` (ETC015). Branch `feature/enum-to-class-property-as-boolean`.

## Deployment Plan
1. PR → merge to `master`.
2. `dotnet test -c Release`.
3. Tag/publish when ready.
