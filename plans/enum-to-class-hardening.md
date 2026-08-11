# EnumToClass — hardening after technical/substantive review

Close correctness gaps (Empty/flyweight, string escaping, enum member filtering), tighten smart-enum API contract, improve generator quality (diagnostics, hygiene), and extend tests so edge cases cannot regress. Scope is the existing `VRT.Generators.EnumToClass` source generator only; no new features beyond what the review marked P0–P3.

## For Future Agents
As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done,
set its status to `Complete` and write its **Phase Summary** (what was done, key
decisions, anything needed to continue with zero context); run the phase's
**Verification Plan** and record the result before moving on. When all phases are
done, fill in **Final Recap** and **Deployment Plan**.

### Context (zero prior knowledge)
- Repo root: `VRT.Generators.EnumToClass` (branch historically `master`).
- Generator: `src/EnumToClass/` — `EnumToClassGenerator` (`IIncrementalGenerator`), `EnumToClassData`, helpers under `Helpers/`.
- Tests: `tests/EnumToClass.Tests.Integration` (runtime + analyzer ref), `tests/EnumToClass.Tests.Snapshot` (Verify).
- Review findings (source of this plan): Empty is a separate instance from map; Empty uses first enum field not `default(TEnum)`; description/name strings not escaped; no `IsConst`/`HasConstantValue` filter for `value__`; aggressive implicits; missing diagnostics; dead code; CI triggers on `main` while default branch may be `master`; README stale.
- Package: analyzer-only NuGet (`analyzers/dotnet/cs`), MinVer tags `v*`.

### Decisions already taken (from review; do not re-litigate unless user overrides)
1. **Empty** must represent “missing / default” consistently with `IsEmpty == (Value == default(TEnum))`, and must be the **same instance** as the map entry for that default member when a named member equals `default`; if no named member has value `default`, still expose a single shared Empty instance (do not `new` a duplicate of first field).
2. **String literals** in generated C# must be escaped (`SymbolDisplay.FormatLiteral` or equivalent).
3. **Enum members**: only static fields with constant value (`IsStatic && HasConstantValue` / equivalent).
4. Prefer fixing **class** equality (`IEquatable<T>`, `==`/`!=`) over documenting inequality with `==`.
5. Out of scope for this plan unless explicitly added later: JsonConverter, TypeConverter, Flags-aware parsing, rename of `*Instance` API (breaking).

## Phase 1: P0 — correctness
Status: Not started

- [ ] Filter enum fields to static constant members only in `EnumToClassData.FromClass` (exclude `value__` and non-members); centralize in one place used by all generation paths.
- [ ] Escape all generated string literals (`Description`, dictionary keys if needed, any other embedded strings) so `"`, `\`, newlines, and unicode do not break `.g.cs`.
- [ ] Fix `Empty` construction: do not use “first field”; resolve the field whose constant value equals `default(TEnum)` when present; otherwise construct Empty for `default(TEnum)` once and reuse that instance (same reference returned from `GetByName` miss path and `Empty` property).
- [ ] Ensure `Empty` and the map entry for the default-named member (e.g. `None`) are **reference-equal** when that member’s value is `default`.
- [ ] Align `IsEmpty` with `Value == default(TEnum)` for every generated instance (including Empty).
- [ ] Add integration tests: (a) enum whose first member ≠ 0; (b) description containing quotes and newlines; (c) `ReferenceEquals(Empty, defaultMemberInstance)` when default is named; (d) external/metadata enum still matches `Enum.GetValues` count (no `value__`).
- [ ] Add/update snapshot tests covering escaped description and non-zero-first enum if feasible in snapshot harness.

### Verification Plan
- `dotnet test` from repo root — expect all tests green (0 failed).
- `dotnet build src/EnumToClass/EnumToClass.csproj -c Release` — 0 errors/warnings of note.
- Manually inspect or assert generated output for a description like `He said "hi"` contains a valid C# string literal (snapshot or integration compile).
- Integration: type with enum `A = 1, B = 0` — `Empty.Value == default`, `Empty.IsEmpty == true`, missing name resolves to same reference as `Empty`.

### Phase Summary
_(write when phase completes)_

## Phase 2: P1 — smart-enum contract
Status: Not started

- [ ] For generated **class** types: implement `IEquatable<T>`, override `Equals`/`GetHashCode` consistently, add `operator ==` and `operator !=` based on `Value` (records keep compiler equality; do not double-generate Equals for records).
- [ ] Guarantee single instance per named enum member (flyweight): all factories (`GetByName`, implicit from enum/string/underlying, `*Instance`, Empty when applicable) return map instances only — no ad-hoc `new` outside static initialization of the map.
- [ ] Add `TryGetByName(string name, out T result)` (or equivalent) returning false on miss; document whether `GetByName` keeps returning Empty on miss (current behavior) or changes — **keep GetByName → Empty** unless user asks breaking change; Try* is additive.
- [ ] Integration tests: `==`/`!=` for classes; `ReferenceEquals` across conversion paths for same enum value; `TryGetByName` success/failure.
- [ ] Snapshot update for new members on class (if snapshots cover class; if only record today, add a class snapshot case).

### Verification Plan
- `dotnet test` — all green.
- Integration asserts: `TestElementClass.Element1 == (TestElementClass)TestElements.Element1` and `ReferenceEquals` across `GetByName`, `*Instance`, and implicit enum conversion.
- No new public breaking changes to existing method signatures (additive API only).

### Phase Summary
_(write when phase completes)_

## Phase 3: P2 — generator quality and repo hygiene
Status: Not started

- [ ] Emit Roslyn diagnostics (stable ids e.g. `ETC001`…) at least for: target type not `partial`; optionally empty enum; document IDs in README.
- [ ] Prefix BCL types in generated code with `global::` (`System.Collections.Generic.Dictionary`, etc.) to avoid name clashes.
- [ ] Remove `Console.WriteLine` from `FieldSymbolExtensions.ConvertToSummary` (silent empty fallback or diagnostic only).
- [ ] Delete or wire up dead code: `ResourcesHelper`, unused `TypeExtensions` members, leftover `EnumToClass.props` (`RecognizeFramework_*`) if still unused; remove unused test harness types if truly dead.
- [ ] Fix CI branch trigger in `.github/workflows/build_and_test.yml` to match default branch (`master` and/or `main`).
- [ ] Prefer fixed `LangVersion` over `preview` in `Directory.Build.props` if no preview feature is required (confirm before changing).
- [ ] Refresh `README.md`: fix “Generatror” typo; regenerate usage samples from real output (`*Instance`, Empty, usings); fix list numbering for `WithDescription` sources.
- [ ] Normalize indentation of copied XML doc comments in generated const/instance members (optional polish if cheap).

### Verification Plan
- `dotnet test` — all green.
- `dotnet pack src/EnumToClass/EnumToClass.csproj -c Release -o ./artifacts` — nupkg contains only `analyzers/dotnet/cs/EnumToClass.dll` (+ readme/icon/nuspec), no Roslyn dependency assemblies in lib/.
- Grep generated samples / snapshots for `global::System.Collections` (or agreed pattern).
- Confirm workflow YAML lists the branch actually used on origin.

### Phase Summary
_(write when phase completes)_

## Phase 4: P3 — test matrix and edge coverage
Status: Not started

- [ ] Integration: underlying type `byte` or `long` (implicit operators compile and convert correctly).
- [ ] Integration: non-`partial` host produces diagnostic (not only cryptic CS errors), if Phase 3 diagnostics landed.
- [ ] Integration or snapshot: nested type host — either supported with correct partial nesting or diagnostic “not supported”.
- [ ] Snapshot for **class** host (not only record) including Equals/`==` surface after Phase 2.
- [ ] Flags enum: document current behavior (ToString miss → Empty) in README; optional test locking that behavior.
- [ ] Case sensitivity of `GetByName`: test + document (exact ordinal match).

### Verification Plan
- `dotnet test -c Release` — all green.
- Snapshot tests: no pending diff (`*.received.*` absent after run).
- `dotnet test -c Release --no-build` after a clean Release build — same pass (CI parity).

### Phase Summary
_(write when phase completes)_

## Final Recap
_(write when all phases complete: summary of the entire piece of work)_

## Deployment Plan
_(write when all phases complete: step-by-step deployment instructions)_

<!-- Suggested deployment steps when filling in later:
1. Ensure all phases Complete and Final Recap filled.
2. `dotnet test -c Release` on clean tree.
3. Bump via git tag `vX.Y.Z` (MinVer); push tag to trigger `release.yml` or pack locally.
4. Publish nupkg to configured NuGet source; verify package contents.
5. Note breaking vs additive changes in README Change Log.
-->
