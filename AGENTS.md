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
2. **Git commits — agent NEVER creates commits.**
   - Do **not** run `git commit`, `git commit --amend`, or any command that creates/amends commits.
   - Do **not** stage-and-commit even if the user says “commit this” in a generic way aimed at other tools; for this repo the human owns all commits.
   - **After each plan phase is Complete** (verification green + Phase Summary written): propose only the **commit message** (subject + body) for a **single collective commit covering that phase**.
   - The user reviews the work and creates that phase commit themselves.
   - Message style: complete sentences, repo-conventional prefixes (`fix`/`feat`/`chore`/`test`) when helpful; no secrets; no co-author trailers unless the user asks.
3. **Keep this `AGENTS.md` updated** in the same session when you:
   - lock a design decision,
   - finish or cancel a phase item,
   - discover a deferred gap,
   - change branch/release process,
   - change commit/process rules (like this section).
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
7. **Out of scope** (unless explicitly added later): JsonConverter, TypeConverter, Flags-aware parsing, renaming `*Instance` API (breaking), nested host type **generation** (full support).
8. **Nested host types**: unsupported for generation; generator reports **`ETC003`** and skips output (no outer-type wrapping in generated partials).
9. **`[Flags]` / combined values**: enum→class uses `ToString()`; combined names are not map keys → `Empty`. Documented, not “fixed” as smart Flags.

## Diagnostics

| Id | Severity | When |
|----|----------|------|
| `ETC001` | Error | Host type is not `partial` — generation skipped |
| `ETC002` | Warning | Enum has no named members |
| `ETC003` | Error | Host type is nested — generation skipped |

Defined in `src/EnumToClass/EnumToClassDiagnostics.cs`. Release tracking: `AnalyzerReleases.Shipped.md` + `AnalyzerReleases.Unshipped.md` (AdditionalFiles).

## Key implementation map

| Concern | Location |
|---------|----------|
| Generator pipeline | `EnumToClassGenerator.cs` |
| Model + Empty/construction/filter | `EnumToClassData.cs` |
| Marker attribute source | `EnumToClassAttributeDefinition.cs` |
| String literals | `Helpers/CodeLiteral.cs` |
| Partial / accessibility / attribute args | `Helpers/NamedTypeSymbolExtensions.cs` |
| XML docs / DescriptionAttribute | `Helpers/FieldSymbolExtensions.cs` |
| Doc comment line normalization for emission | `Helpers/DocumentationFormatter.cs` |
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
| 4 P3 test matrix | **Complete** | byte underlying, case sensitivity, non-partial snapshot, docs for Flags |
| 5 Nested diagnostic | **Complete** | ETC003 on nested hosts; skip generation; snapshot + README |
| 6 Doc indent polish | **Complete** | `DocumentationFormatter`; consistent `///` indent in generated members |
| 7 Description summary + RS2008 | **Complete** | Full multiline `<summary>` for Description; analyzer release tracking files |

**Hardening plan status: COMPLETE** (phases 1–7). Final Recap / Deployment Plan are in the plan file. No further phases are scheduled unless the user opens new scope.

### Intentionally deferred (require explicit user go-ahead)

- Nested host type **full generation** (outer partial wrapping) — only ETC003 today.
- Flags-aware parsing.
- JsonConverter / TypeConverter.

### Potential future features (backlog — not scheduled)

Recorded for later product decisions. Do **not** implement unless the user opens a new plan/phase.

1. **Full nested host support** (discussed, design notes locked in):
   - **Not possible** to extend `Outer.Nested` without **nested partials** in generated code (namespace-level `partial class Nested` is a different type).
   - Roslyn `AddSource` cannot inject into the user’s existing file; only a second partial part with the same nesting path works.
   - C# rule: if a nested type is partial across files, **containing types must also be partial**.
   - Implementation sketch if approved: walk `ContainingType` chain → emit nested `partial` wrappers → diagnostics when any ancestor is not `partial` (e.g. ETC004) → keep or refine ETC003 → snapshots for 1–2 nesting levels.
   - Today: **ETC003** + skip generation remains correct for “unsupported”.

2. **Flags-aware parsing** — map combined flag `ToString()` / bit combinations; currently unknown combined values → `Empty`.

3. **JsonConverter / TypeConverter** — serialize smart-enum types as name or underlying value.

4. **Other smart-enum API** (only if requested): e.g. `GetByValue` / `TryGetByValue`, optional case-insensitive name lookup — not designed yet.

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

## Proposed commit messages (agent proposes only; human commits)

**Rule:** Agent never runs `git commit`. After a phase is done, paste a ready-to-use message for the user’s **one collective commit for that phase**.

If several phases landed in one working tree before the first human commit, the user may instead make one rollup commit; agent still only suggests text.

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

### Phase 5 — nested host diagnostic

```
feat(generator): report ETC003 and skip generation for nested hosts

Detect nested EnumToClass hosts (ContainingType), emit ETC003 error,
and do not emit partial sources that would land at namespace scope.
Add snapshot coverage and document ETC003 in README.
```

### Phase 6 — documentation indentation

```
fix(generator): normalize XML doc comment indentation in generated members

Format each documentation line as a clean /// token and emit line-by-line
so class-member indent applies consistently. Update Constants snapshots.
```

### Phase 7 — full summary Description + release tracking

```
feat(generator): use full XML summary for Description; track analyzer releases

Parse all lines inside <summary> for WithDescription values (newline-separated).
Add AnalyzerReleases.Shipped/Unshipped for ETC001–ETC003 and drop RS2008 suppress.
```

### Rollup (historical — phases 1–4 already committed by user)

```
feat(generator): harden EnumToClass Empty, equality, diagnostics, and tests

Implement review plan P0–P3: default Empty flyweight, escaped literals,
constant-only members, class IEquatable/==, TryGetByName, ETC001/ETC002,
global:: BCL types, CI/docs hygiene, and expanded integration/snapshot tests.
Also add AGENTS.md for multi-session agent handoff.
```

## Session changelog (agent notes)

| When | Note |
|------|------|
| Hardening session | Full technical review completed; plan written under `plans/enum-to-class-hardening.md`. |
| Hardening session | Branch `feature/enum-to-class-hardening` created; phases 1–4 implemented and verified (Release: 48 integration + 5 snapshot). |
| Hardening session | User asked: after each completed phase propose a git commit message; create/maintain English `AGENTS.md`. |
| Hardening session | User clarified: **agent never creates commits** — only propose the message; user reviews and makes a **collective commit per plan phase**. |
| Hardening session | User committed rollup for phases 1–4 on `feature/enum-to-class-hardening`. |
| Hardening session | Phase 5: ETC003 nested host diagnostic implemented and verified (48 integration + 6 snapshot Release). |
| Hardening session | Phase 6: XML doc indent polish via DocumentationFormatter; snapshots updated; Release tests green. |
| Hardening session | Phase 7: full multiline summary for Description; analyzer release tracking; Release 49+6 green. |
| Hardening session | Confirmed: no more scheduled plan phases; remaining items are deferred product features only. |
| Hardening session | User: remember nested full support (and related) as **potential future features** only; design note: nested support requires nested partials + partial containers. |

## How to resume in a new session

1. Read this file and `plans/enum-to-class-hardening.md`.
2. `git status` / current branch — expect `feature/enum-to-class-hardening` until merged.
3. If plan phases are Complete but commits missing → propose commit **messages** only; user commits after review.
4. Next product work: only deferred items or new user scope; do not reopen locked decisions without asking.
5. Never create git commits in this repository on the user’s behalf.
