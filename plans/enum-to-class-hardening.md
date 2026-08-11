# EnumToClass — hardening after technical/substantive review

Close correctness gaps (Empty/flyweight, string escaping, enum member filtering), tighten smart-enum API contract, improve generator quality (diagnostics, hygiene), and extend tests so edge cases cannot regress. Scope is the existing `VRT.Generators.EnumToClass` source generator only; no new features beyond what the review marked P0–P3.

## For Future Agents
As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done,
set its status to `Complete` and write its **Phase Summary** (what was done, key
decisions, anything needed to continue with zero context); run the phase's
**Verification Plan** and record the result before moving on. When all phases are
done, fill in **Final Recap** and **Deployment Plan**.

**After each phase is Complete:** propose a **git commit message only** (subject + body)
for the user’s collective phase commit. **Agents must never run `git commit`** — the
human reviews and commits. Keep root **`AGENTS.md`** (English) updated with decisions
and session notes so new sessions can resume without chat history.

### Context (zero prior knowledge)
- Repo root: `VRT.Generators.EnumToClass` (branch historically `master`).
- Work branch: `feature/enum-to-class-hardening`.
- Generator: `src/EnumToClass/` — `EnumToClassGenerator` (`IIncrementalGenerator`), `EnumToClassData`, helpers under `Helpers/`.
- Tests: `tests/EnumToClass.Tests.Integration` (runtime + analyzer ref), `tests/EnumToClass.Tests.Snapshot` (Verify).
- Review findings (source of this plan): Empty is a separate instance from map; Empty uses first enum field not `default(TEnum)`; description/name strings not escaped; no `IsConst`/`HasConstantValue` filter for `value__`; aggressive implicits; missing diagnostics; dead code; CI triggers on `main` while default branch may be `master`; README stale.
- Package: analyzer-only NuGet (`analyzers/dotnet/cs`), MinVer tags `v*`.

### Decisions already taken (from review; do not re-litigate unless user overrides)
1. **Empty** must represent “missing / default” consistently with `IsEmpty == (Value == default(TEnum))`, and must be the **same instance** as the map entry for that default member when a named member equals `default`; if no named member has value `default`, still expose a single shared Empty instance (do not `new` a duplicate of first field).
2. **String literals** in generated C# must be escaped (`SymbolDisplay.FormatLiteral` or equivalent).
3. **Enum members**: only static fields with constant value (`IsStatic && HasConstantValue` / equivalent).
4. Prefer fixing **class** equality (`IEquatable<T>`, `==`/`!=`) over documenting inequality with `==`.
5. Out of scope for this plan unless explicitly added later: JsonConverter, TypeConverter, Flags-aware parsing, rename of `*Instance` API (breaking). Nested host types remain unsupported (documented).

## Phase 1: P0 — correctness
Status: Complete

- [x] Filter enum fields to static constant members only in `EnumToClassData.FromClass` (exclude `value__` and non-members); centralize in one place used by all generation paths.
- [x] Escape all generated string literals (`Description`, dictionary keys if needed, any other embedded strings) so `"`, `\`, newlines, and unicode do not break `.g.cs`.
- [x] Fix `Empty` construction: do not use “first field”; resolve the field whose constant value equals `default(TEnum)` when present; otherwise construct Empty for `default(TEnum)` once and reuse that instance (same reference returned from `GetByName` miss path and `Empty` property).
- [x] Ensure `Empty` and the map entry for the default-named member (e.g. `None`) are **reference-equal** when that member’s value is `default`.
- [x] Align `IsEmpty` with `Value == default(TEnum)` for every generated instance (including Empty).
- [x] Add integration tests: (a) enum whose first member ≠ 0; (b) description containing quotes and newlines; (c) `ReferenceEquals(Empty, defaultMemberInstance)` when default is named; (d) external/metadata enum still matches `Enum.GetValues` count (no `value__`).
- [x] Add/update snapshot tests covering escaped description and non-zero-first enum if feasible in snapshot harness.

### Verification Plan
- `dotnet test` from repo root — expect all tests green (0 failed).
- `dotnet build src/EnumToClass/EnumToClass.csproj -c Release` — 0 errors/warnings of note.
- Manually inspect or assert generated output for a description like `He said "hi"` contains a valid C# string literal (snapshot or integration compile).
- Integration: type with enum `A = 1, B = 0` — `Empty.Value == default`, `Empty.IsEmpty == true`, missing name resolves to same reference as `Empty`.

### Phase Summary
Implemented in `EnumToClassData` / `CodeLiteral` / generator Constants output. Members require `IsStatic && HasConstantValue`. Literals use `SymbolDisplay.FormatLiteral`. `Empty` lives next to `ValueByNameMap` and is either `ValueByNameMap[defaultMember]` or a single `new(... default(TEnum) ...)`. Integration + snapshots for non-zero-first and escaped description. Verified green via full test run.

## Phase 2: P1 — smart-enum contract
Status: Complete

- [x] For generated **class** types: implement `IEquatable<T>`, override `Equals`/`GetHashCode` consistently, add `operator ==` and `operator !=` based on `Value` (records keep compiler equality; do not double-generate Equals for records).
- [x] Guarantee single instance per named enum member (flyweight): all factories (`GetByName`, implicit from enum/string/underlying, `*Instance`, Empty when applicable) return map instances only — no ad-hoc `new` outside static initialization of the map.
- [x] Add `TryGetByName(string name, out T result)` (or equivalent) returning false on miss; document whether `GetByName` keeps returning Empty on miss (current behavior) or changes — **keep GetByName → Empty** unless user asks breaking change; Try* is additive.
- [x] Integration tests: `==`/`!=` for classes; `ReferenceEquals` across conversion paths for same enum value; `TryGetByName` success/failure.
- [x] Snapshot update for new members on class (if snapshots cover class; if only record today, add a class snapshot case).

### Verification Plan
- `dotnet test` — all green.
- Integration asserts: `TestElementClass.Element1 == (TestElementClass)TestElements.Element1` and `ReferenceEquals` across `GetByName`, `*Instance`, and implicit enum conversion.
- No new public breaking changes to existing method signatures (additive API only).

### Phase Summary
Classes get `: IEquatable<T>` plus `==`/`!=`. `TryGetByName` added; `GetByName` still returns `Empty` on miss. Flyweight covered by Empty-from-map + existing map instances; integration tests lock identity and operators. Class snapshots: `EnumToClass_WithEscapedDescription`, `EnumToClass_NonZeroFirstMember`.

## Phase 3: P2 — generator quality and repo hygiene
Status: Complete

- [x] Emit Roslyn diagnostics (stable ids e.g. `ETC001`…) at least for: target type not `partial`; optionally empty enum; document IDs in README.
- [x] Prefix BCL types in generated code with `global::` (`System.Collections.Generic.Dictionary`, etc.) to avoid name clashes.
- [x] Remove `Console.WriteLine` from `FieldSymbolExtensions.ConvertToSummary` (silent empty fallback or diagnostic only).
- [x] Delete or wire up dead code: `ResourcesHelper`, unused `TypeExtensions` members, leftover `EnumToClass.props` (`RecognizeFramework_*`) if still unused; remove unused test harness types if truly dead.
- [x] Fix CI branch trigger in `.github/workflows/build_and_test.yml` to match default branch (`master` and/or `main`).
- [x] Prefer fixed `LangVersion` over `preview` in `Directory.Build.props` if no preview feature is required (confirm before changing).
- [x] Refresh `README.md`: fix “Generatror” typo; regenerate usage samples from real output (`*Instance`, Empty, usings); fix list numbering for `WithDescription` sources.
- [x] Normalize indentation of copied XML doc comments in generated const/instance members (optional polish if cheap).

### Verification Plan
- `dotnet test` — all green.
- `dotnet pack src/EnumToClass/EnumToClass.csproj -c Release -o ./artifacts` — nupkg contains only `analyzers/dotnet/cs/EnumToClass.dll` (+ readme/icon/nuspec), no Roslyn dependency assemblies in lib/.
- Grep generated samples / snapshots for `global::System.Collections` (or agreed pattern).
- Confirm workflow YAML lists the branch actually used on origin.

### Phase Summary
`ETC001` (not partial, error) and `ETC002` (empty enum, warning) in `EnumToClassDiagnostics`. Generated code uses `global::System.Collections.*` / `global::System.Linq.Enumerable` without usings. Removed `Console.WriteLine`, dead helpers/props, unused `CSharpSourceGeneratorVerifier`. CI triggers on `master` and `main`. `LangVersion` → `latest`. README rewritten (changelog 1.0.8, diagnostics, semantics). Doc-comment indentation polish deferred (optional; still uneven for external enums). Pack verified: analyzer-only nupkg. RS2008 suppressed on generator project.

## Phase 4: P3 — test matrix and edge coverage
Status: Complete

- [x] Integration: underlying type `byte` or `long` (implicit operators compile and convert correctly).
- [x] Integration: non-`partial` host produces diagnostic (not only cryptic CS errors), if Phase 3 diagnostics landed.
- [x] Integration or snapshot: nested type host — either supported with correct partial nesting or diagnostic “not supported”.
- [x] Snapshot for **class** host (not only record) including Equals/`==` surface after Phase 2.
- [x] Flags enum: document current behavior (ToString miss → Empty) in README; optional test locking that behavior.
- [x] Case sensitivity of `GetByName`: test + document (exact ordinal match).

### Verification Plan
- `dotnet test -c Release` — all green.
- Snapshot tests: no pending diff (`*.received.*` absent after run).
- `dotnet test -c Release --no-build` after a clean Release build — same pass (CI parity).

### Phase Summary
Byte-backed enum + case-sensitivity integration tests. Snapshot `EnumToClass_NonPartial_ReportsETC001` locks `ETC001` (snapshot harness now merges `driver.GetRunResult().Diagnostics`). Class hosts in escaped/non-zero snapshots. Flags documented in README. Nested-type diagnostic completed in **Phase 5** (`ETC003`).

**Release verification (2026-03-24 session):** `dotnet test -c Release` → 48 integration + 5 snapshot passed. Pack → `analyzers/dotnet/cs/EnumToClass.dll` only (+ readme/icon).

## Phase 5: Nested host diagnostic (deferred from Phase 4)
Status: Complete

- [x] Detect nested host types (`ContainingType is not null`).
- [x] Emit **ETC003** error and **skip** code generation (no broken namespace-level partials).
- [x] Snapshot test locking ETC003 message/location shape.
- [x] Document ETC003 in README diagnostics + semantics.

### Verification Plan
- `dotnet test -c Release` — all green (integration + snapshots including NestedType).
- Snapshot `EnumToClass_NestedType_ReportsETC003` shows ETC003; no Constants/Constructors sources for nested host.

### Phase Summary
`IsNested` / `ContainingTypeName` on `EnumToClassData`. Generator reports `ETC003` before partial checks and returns without `AddSource`. Snapshot + README updated. Full nested-type **support** remains out of scope.

**Verification:** Release tests 48 integration + 6 snapshot passed.

## Final Recap
Hardening of `EnumToClass` on branch `feature/enum-to-class-hardening` delivered P0–P3 plus Phase 5 nested diagnostic:

1. **Correctness:** constant-only enum members, escaped literals, Empty flyweight aligned with `default(TEnum)`.
2. **API:** `IEquatable`/`==` for classes, `TryGetByName`, identity-preserving lookups.
3. **Quality:** `ETC001`/`ETC002`/`ETC003`, `global::` BCL, hygiene, CI branches, README, LangVersion, doc indent polish (Phase 6).
4. **Tests:** edge cases (non-zero first, escapes, byte underlying, case sensitivity, non-partial, nested host).

Remaining known gaps (explicitly deferred): nested host **generation** (only diagnostic), Flags-aware parsing, Json/Type converters.

## Phase 6: XML documentation indentation polish
Status: Complete

- [x] Normalize documentation trivia/lines to clean `/// ...` without source indentation (`DocumentationFormatter`).
- [x] Emit each doc line as a separate join segment so member indent (`        `) applies to every line.
- [x] Simplify description-built summaries to unindented `///` lines before format.
- [x] Update Verify snapshots for Constants files with correct doc alignment.

### Verification Plan
- `dotnet test -c Release` — all green.
- Inspect Constants snapshots: every `///` line aligned with `public const` / `*Instance` members.

### Phase Summary
Root cause was multi-line doc strings joined with only inter-item newlines, so internal lines kept source indent. Fixed by line-wise yield + trim. Release: 48 integration + 6 snapshot OK.

## Phase 7: Full summary Description + analyzer release tracking
Status: Complete

- [x] Extract **full** XML `<summary>` text for `Description` (all lines, newline-separated), not only the first line.
- [x] Efficient single-pass parser (StringBuilder), no LINQ chain.
- [x] Integration coverage for multiline summary without `DescriptionAttribute`.
- [x] Add `AnalyzerReleases.Shipped.md` / `Unshipped.md` for ETC001–ETC003; remove RS2008 suppress.
- [x] Document Description cascade in README.

### Verification Plan
- `dotnet test -c Release` — all green (no RS2008 warnings).
- Integration: multiline summary member Description matches both lines.

### Phase Summary
`GetCommentSummary` rewritten; Element7 test. Analyzer release tracking files wired as AdditionalFiles. Release: 49 integration + 6 snapshot OK.

## Deployment Plan
1. Review diff on `feature/enum-to-class-hardening`; merge to `master` (or PR).
2. On clean tree: `dotnet test -c Release`.
3. Tag release when ready: `git tag v1.0.8` (or next MinVer-compatible version) and `git push --tags` to run `release.yml`, **or** pack locally and push nupkg.
4. Confirm NuGet package content: `analyzers/dotnet/cs/EnumToClass.dll`, README, icon — no Roslyn deps in `lib/`.
5. Changelog already notes 1.0.8 hardening items in `README.md` for package consumers.
