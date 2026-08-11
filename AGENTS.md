# AGENTS.md — EnumToClass (VRT.Generators.EnumToClass)

Durable context for humans and coding agents. **Update this file** when decisions, scope, or process change so a new session can continue without prior chat history.

## Project

| Item | Value |
|------|--------|
| Product | C# Roslyn **source generator**: enum → smart-enum style closed class/record |
| Package | Analyzer-only NuGet (`analyzers/dotnet/cs/EnumToClass.dll`), MinVer tags `v*` |
| Solution | `VRT.Generators.EnumToClass.sln` |
| Generator | `src/EnumToClass/` (`netstandard2.0`, `IIncrementalGenerator`) |
| Tests | `tests/EnumToClass.Tests.Integration` (runtime + analyzer project ref), `tests/EnumToClass.Tests.Snapshot` (Verify) |
| Plan (source of truth for multi-phase work) | `plans/enum-to-class-hardening.md` |
| Default branch | `master` (CI also listens to `main`) |
| Active work branch | `feature/enum-to-class-hardening` |

## Agent process (mandatory)

1. **Plan file** is authoritative for multi-phase work: checkboxes, phase status, Phase Summary, Final Recap, Deployment Plan (`plans/*.md`, real-work style).
2. **After each phase is Complete** (verification green + Phase Summary written):
   - Propose a **git commit** to the user (subject + body). Do not invent secrets; follow repo commit style.
   - Only create the commit when the user explicitly asks to commit (or confirms the proposed message).
   - Prefer **one commit per completed phase** when the working tree can be staged cleanly; if work was done in bulk, propose a practical split or a single rollup and note what belongs to which phase.
3. **Keep this `AGENTS.md` updated** in the same session when you:
   - lock a design decision,
   - finish or cancel a phase item,
   - discover a deferred gap,
   - change branch/release process.
4. Language for this file: **English**. User chat may be Polish; durable agent notes stay English.

## Product decisions (locked)

Do not re-litigate unless the user overrides.

1. **`IsEmpty`** ⇔ `Value == default(TEnum)` for every instance (including `Empty`).
2. **`Empty`**:
   - If a named enum member has value `default(TEnum)`, `Empty` is **reference-equal** to that map entry / `*Instance`.
   - If no named member is default, emit **one** shared `Empty` via `new(... default(TEnum) ...)` (not “first field”).
   - `GetByName` miss → `Empty` (unchanged contract).
3. **String literals** in generated C# must be escaped (`SymbolDisplay.FormatLiteral` / `Helpers/CodeLiteral.cs`).
4. **Enum members** only: `IFieldSymbol` with `IsStatic && HasConstantValue` (excludes metadata `value__`).
5. **Classes**: implement `IEquatable<T>`, `Equals`/`GetHashCode`, `operator ==` / `!=` by `Value`. **Records**: do not emit custom Equals (compiler/record equality).
6. **`TryGetByName`**: additive; returns `false` on miss and sets `out` to `Empty`. `GetByName` still returns `Empty` on miss.
7. **Out of scope** (unless explicitly added later): JsonConverter, TypeConverter, Flags-aware parsing, renaming `*Instance` API (breaking), nested host type generation.
8. **Nested host types**: unsupported; document only (no outer-type wrapping in generated partials).
9. **`[Flags]` / combined values**: enum→class uses `ToString()`; combined names are not map keys → `Empty`. Documented, not “fixed” as smart Flags.

## Diagnostics

| Id | Severity | When |
|----|----------|------|
| `ETC001` | Error | Host type is not `partial` — generation skipped |
| `ETC002` | Warning | Enum has no named members |

Defined in `src/EnumToClass/EnumToClassDiagnostics.cs`. RS2008 (analyzer release tracking) is suppressed in the generator csproj.

## Key implementation map

| Concern | Location |
|---------|----------|
| Generator pipeline | `EnumToClassGenerator.cs` |
| Model + Empty/construction/filter | `EnumToClassData.cs` |
| Marker attribute source | `EnumToClassAttributeDefinition.cs` |
| String literals | `Helpers/CodeLiteral.cs` |
| Partial / accessibility / attribute args | `Helpers/NamedTypeSymbolExtensions.cs` |
| XML docs / DescriptionAttribute | `Helpers/FieldSymbolExtensions.cs` |
| Snapshot harness (includes **generator** diagnostics via `driver.GetRunResult().Diagnostics`) | `tests/.../Tests.Default.cs` → `CheckSourceCode` |

### Generated code conventions

- Hint names: `{ClassName}_Constants.g.cs`, `{ClassName}_Constructors.g.cs`, attribute post-init.
- BCL types in output: prefer `global::System.Collections.*` / `global::System.Linq.Enumerable` (no fragile usings).
- `Empty` is emitted in **Constants** file next to `ValueByNameMap` (ordering / flyweight).
- Class partial declaration includes `: global::System.IEquatable<{ClassName}>`.

## Hardening plan status

Plan file: `plans/enum-to-class-hardening.md`.

| Phase | Status | Theme |
|-------|--------|--------|
| 1 P0 correctness | **Complete** | filter members, escape, Empty/default flyweight + tests |
| 2 P1 smart-enum contract | **Complete** | IEquatable/==, TryGetByName, identity tests |
| 3 P2 quality/hygiene | **Complete** | ETC001/002, global::, dead code, CI, README, LangVersion |
| 4 P3 test matrix | **Complete** | byte underlying, case sensitivity, non-partial snapshot, docs for Flags/nested |

**Final Recap / Deployment Plan** are filled in the plan file. Working tree may still be **uncommitted** on `feature/enum-to-class-hardening` — see commit proposals below.

### Intentionally deferred

- Nested host type support or dedicated diagnostic beyond docs.
- Flags-aware parsing.
- XML documentation comment indentation polish in generated members.
- Analyzer release-tracking files (RS2008 suppressed instead).

## Verification commands

```bash
dotnet test
dotnet test -c Release
dotnet pack src/EnumToClass/EnumToClass.csproj -c Release -o ./artifacts
```

Expect: integration + snapshot all green; nupkg contains `analyzers/dotnet/cs/EnumToClass.dll` (+ README/icon), not Roslyn assemblies under `lib/`.

Accept Verify snapshots by renaming `*.received.*` → `*.verified.*` after intentional generator output changes.

## CI / release

- Build/test workflow: `.github/workflows/build_and_test.yml` — branches `master` and `main`, plus `workflow_dispatch`.
- Release: tag `v*.*.*` → `.github/workflows/release.yml` (MinVer prefix `v`).
- `LangVersion`: `latest` (root `Directory.Build.props`).

## Proposed git commits (post-phase)

Use these when the user asks to commit. If the tree is one mixed diff, either:

- **Option A (preferred if staging is easy):** four commits in order below, or  
- **Option B:** one rollup commit with a body listing all four phases.

### Phase 1 — correctness

```
fix(generator): fix Empty flyweight, enum filters, and string escaping

Filter enum members to static constant fields only. Escape generated
string literals via SymbolDisplay. Add Empty from default(TEnum) map
entry (or single shared instance). Integration and snapshot coverage
for non-zero-first enums and quoted descriptions.
```

### Phase 2 — smart-enum contract

```
feat(generator): add IEquatable, operators, and TryGetByName for classes

Emit IEquatable<T> and ==/!= for class hosts; keep record equality.
Add TryGetByName while GetByName still returns Empty on miss. Lock
flyweight identity and equality with integration tests.
```

### Phase 3 — quality and hygiene

```
chore(generator): diagnostics ETC001/ETC002, global:: BCL, repo hygiene

Report non-partial hosts and empty enums. Qualify BCL types with
global::. Remove dead helpers/props and unused test harness. CI on
master/main; LangVersion latest; refresh README.
```

### Phase 4 — edge tests and docs

```
test: expand edge coverage for byte enums, case sensitivity, ETC001

Add byte-backed and case-sensitive GetByName tests; snapshot for
non-partial ETC001; document Flags and nested-type limitations.
Merge generator diagnostics into snapshot harness.
```

### Rollup alternative (single commit)

```
feat(generator): harden EnumToClass Empty, equality, diagnostics, and tests

Implement review plan P0–P3: default Empty flyweight, escaped literals,
constant-only members, class IEquatable/==, TryGetByName, ETC001/ETC002,
global:: BCL types, CI/docs hygiene, and expanded integration/snapshot tests.
```

## Session changelog (agent notes)

| When | Note |
|------|------|
| Hardening session | Full technical review completed; plan written under `plans/enum-to-class-hardening.md`. |
| Hardening session | Branch `feature/enum-to-class-hardening` created; phases 1–4 implemented and verified (Release: 48 integration + 5 snapshot). |
| Hardening session | User asked: after each completed phase propose a git commit; create/maintain English `AGENTS.md` with agreements for new sessions. |

## How to resume in a new session

1. Read this file and `plans/enum-to-class-hardening.md`.
2. `git status` / current branch — expect `feature/enum-to-class-hardening` until merged.
3. If plan phases are Complete but commits missing → propose/create commits using messages above (user must confirm commit).
4. Next product work: only deferred items or new user scope; do not reopen locked decisions without asking.
