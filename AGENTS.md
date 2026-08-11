# AGENTS.md — EnumToClass (VRT.Generators.EnumToClass)

Durable context for humans and coding agents. **Update this file** when decisions, scope, or process change so a new session can continue without prior chat history.

Keep this file **lean**: process rules, stable product facts, **current plan pointer**, and **backlog**. Full plan text lives under `plans/*.md` only.

## Project

| Item | Value |
|------|--------|
| Product | C# Roslyn **source generator**: enum → smart-enum style closed class/record |
| Package | Analyzer-only NuGet (`analyzers/dotnet/cs/EnumToClass.dll`), MinVer tags `v*` |
| Solution | `VRT.Generators.EnumToClass.sln` |
| Generator | `src/EnumToClass/` (`netstandard2.0`, `IIncrementalGenerator`) |
| Tests | `tests/EnumToClass.Tests.Integration`, `tests/EnumToClass.Tests.Snapshot` (Verify) |
| Default branch | `master` (CI also listens to `main`) |

## Agent process (mandatory)

1. **Plan files** (`plans/*.md`, real-work style) are authoritative for multi-phase work: checkboxes, phase status, summaries, verification, deployment.
2. **Plans vs this file**
   - **`AGENTS.md` holds at most:**
     - **Active plan:** path + one-line status (e.g. Not started / In progress). No phase tables, no long design dumps here — those stay in the plan file.
     - **Backlog:** short bullets of future work; each item **may link** to a plan file when one exists or when deferred from a completed plan (`plans/...`).
   - **When a plan becomes fully Complete** (all phases done, Final Recap filled):
     - **Remove** that plan’s details, phase lists, and commit-message drafts from `AGENTS.md`.
     - Do **not** keep a “completed plans” section of narratives here.
     - Leave durable product decisions in *Product decisions* if they still apply.
     - Move any remaining follow-ups into **Backlog**, with a **reference to the plan** that owned them (if useful).
3. **Git commits — agent NEVER creates commits.**
   - Do **not** run `git commit` / `commit --amend` or otherwise create commits.
   - After each plan **phase** is Complete: propose only the **commit message** (subject + body); the human reviews and commits.
4. **Keep this file updated** when you lock product decisions, change process, switch active plan, complete a plan (cleanup per rule 2), or change backlog.
5. Language for this file: **English**. User chat may be Polish; durable agent notes stay English.

## Active plan

_None._ Last completed: [`plans/enum-to-class-property-as-boolean.md`](plans/enum-to-class-property-as-boolean.md) (`AsBoolean` presence flag).

## Backlog

Do **not** implement unless the user opens scope (new or existing plan).

| Item | Notes / plan ref |
|------|------------------|
| Full nested host generation | Nested partials + partial containers; today **ETC003**. See `plans/enum-to-class-hardening.md` |
| Flags-aware parsing | Combined flags → `Empty` today. See `plans/enum-to-class-hardening.md` |
| JsonConverter / TypeConverter | Not designed |
| Other smart-enum API | e.g. `GetByValue` / case-insensitive names — only if requested |


## Product decisions (locked)

Do not re-litigate unless the user overrides.

1. **`IsEmpty`** ⇔ `Value == default(TEnum)` (including `Empty`).
2. **`Empty`**: same instance as map entry for default-named member when present; else one shared `default(TEnum)` instance. `GetByName` miss → `Empty`.
3. Generated string literals escaped (`Helpers/CodeLiteral.cs` / `SymbolDisplay.FormatLiteral`).
4. Enum members: `IsStatic && HasConstantValue` only.
5. Classes: `IEquatable<T>` + `==`/`!=` by `Value`. Records: no custom Equals. Attribute properties do **not** affect equality.
6. **`TryGetByName`**: additive; miss → `false` + `Empty`.
7. Nested hosts: **ETC003**, no generation (until backlog nested work is planned and done).
8. Flags: `ToString()` lookup only; combined values → `Empty` unless Flags work is implemented.
9. **`EnumToClassPropertyAttribute<TAttribute>`** (`AllowMultiple` on host applications): opt-in projection; optional `Name`, **`AsArray`**, **`Source`**, **`AsBoolean`** (presence `bool`; not with AsArray/Source); slim ctor + object initializers.

## Diagnostics (current)

| Id | Severity | When |
|----|----------|------|
| `ETC001` | Error | Host not `partial` |
| `ETC002` | Warning | Enum has no named members |
| `ETC003` | Error | Nested host |
| `ETC010` | Warning | Attribute cannot be reconstructed for property |
| `ETC011` | Error | Duplicate `EnumToClassProperty<T>` for same `T` |
| `ETC012` | Error | Invalid / conflicting property name |
| `ETC014` | Error | Invalid `Source` on `EnumToClassProperty` |
| `ETC015` | Error | Conflicting `EnumToClassProperty` options |

`EnumToClassDiagnostics.cs`; release tracking via `AnalyzerReleases.*.md`.

## Key paths

| Concern | Location |
|---------|----------|
| Generator | `EnumToClassGenerator.cs` |
| Model | `EnumToClassData.cs` |
| Marker attributes | `EnumToClassAttributeDefinition.cs` |
| Attribute → C# expression | `Helpers/AttributeConstructionEmitter.cs` |
| Property naming | `Helpers/PropertyNameHelper.cs` |
| Docs / Description | `Helpers/FieldSymbolExtensions.cs`, `DocumentationFormatter.cs` |
| Snapshots harness | `tests/EnumToClass.Tests.Snapshot/Tests.Default.cs` |

## Verification

```bash
dotnet test
dotnet test -c Release
dotnet pack src/EnumToClass/EnumToClass.csproj -c Release -o ./artifacts
```

Accept Verify: rename `*.received.*` → `*.verified.*` after intentional output changes.

## CI / release

- Build/test: `.github/workflows/build_and_test.yml` (`master`, `main`)
- Release: tag `v*.*.*` → `release.yml` (MinVer prefix `v`)

## How to resume

1. Read **Active plan** (if any) and open that `plans/*.md` file.
2. Scan **Backlog** for deferred product work (follow plan refs when present).
3. Never create git commits; propose phase commit messages only.
4. On plan completion: strip plan details from this file per process rule 2.
