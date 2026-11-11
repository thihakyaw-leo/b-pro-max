# AGENTS.md — b-pro-max

## What this repo is
Modern WinUI 3 reimplementation of DD.exe (VB6 lottery/agent management). .NET 9 + C#, Windows App SDK / WinUI 3, PostgreSQL via Npgsql + Supabase REST, EPPlus/CsvHelper/QuestPDF for export/reporting, JSON config (`appsettings.json` + `.env` secrets via DotNetEnv), xUnit tests.

## Toolchain
- `global.json`: pins .NET SDK `10.0.115`, `rollForward: latestMajor`. Installed SDKs: 8.0, 9.0, 10.0.403, 11.0-rc. `dotnet --version` resolves to the latest.
- All projects target `net9.0-windows10.0.19041.0`, `Platform=x64`, `RuntimeIdentifiers=win-x64`. **Windows desktop only** — WinRT activation fails (`REGDB_E_CLASSNOTREG`) in containers/non-Windows.
- `TreatWarningsAsErrors=true` — warnings fail the build. Only exception: `WarningsNotAsErrors=CA1707` (test identifier naming).
- Analyzers: SDK (`AnalysisLevel=latest-recommended`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`) + `Meziantou.Analyzer 3.0.322`. StyleCop evaluated, not adopted.
- Formatting: `dotnet format` (`.editorconfig` is source of truth). No CI workflows exist (`.github` only has `github-app.yml`).

## Project layout
- `src/b-pro-max.csproj` — app (`OutputType=WinExe`, `UseWinUI=true`). Entry: `src/Program.cs` → `App.Initialize(args)` (DI + config) → `InteractiveMenu`. Composition root: `src/App.cs`.
- `tests/b-pro-max.tests.csproj` — xUnit unit tests, references src. **Excludes `e2e/**`** (Compile Remove) to avoid CS0579 duplicate assembly attributes — e2e is its own project.
- `tests/e2e/b-pro-max.e2e.csproj` — separate xUnit e2e project (Npgsql). Run independently.
- `src/Views/` — WinUI views (`.xaml` + `.xaml.cs`).
- `Scripts/001_agents.sql` — PostgreSQL `agents` table schema (from DD.exe `AgentThai`).
- `appsettings.json` — config. `.env` (gitignored) overrides via DotNetEnv; never commit real credentials.

## Build quirks (easily missed)
1. **XAML compilation is disabled.** `MarkupCompilePass1`/`MarkupCompilePass2` are no-op'd; all `**\*.xaml` removed from compilation; pre-generated `.g.cs` files used instead. Reason: `System.Security.Permissions` v6 unavailable on .NET 9 and `XamlCompiler.exe` crashes silently. Do not add new XAML pages expecting normal compilation.
2. **`System.Drawing.Common 8.0.0` is pinned** to unify an EPPlus/System.Windows.Extensions assembly conflict (MSB3277). Do not downgrade.
3. `Package.appxmanifest` is copied to output as Content.
4. Scratch under `src/` (`BenchmarksTest.csproj`, `ProgramApp.csproj`, `RunBenchmarks.csproj`, `simple_test.csproj`, `test_program.csproj`, `compile_test.bat`, `BenchmarksFolder`, `Benchmarks_File`, `test.txt`, `src/global.json`) and root `benchmarks/`, `benchmarks.tmp` are **gitignored local scratch** — do not add to the solution.
5. Transient `CS2012` file-lock errors from Microsoft Defender can occur during builds; retry usually succeeds.

## Commands (run from repo root `F:\b\b-pro-max`)
```
# Quality pass, in order:
dotnet format --verify-no-changes        # formatting check
dotnet build b-pro-max.sln -c Release    # whole solution (src + tests + e2e)
dotnet test tests\b-pro-max.tests.csproj # unit tests
dotnet test tests\e2e\b-pro-max.e2e.csproj --no-build   # e2e tests
# Single test:
dotnet test tests\b-pro-max.tests.csproj --filter "FullyQualifiedName~ClassName"
# Run app:
dotnet run --project src\b-pro-max.csproj
```
Always run `build -> test` before committing.

## Databases / secrets
- `appsettings.json` has placeholder empty DB password and Supabase keys. Real credentials live in `.env` (gitignored, loaded by `DotNetEnv.Env.Load()` in `App.cs`).
- Create the `agents` table via `Scripts/001_agents.sql` against PostgreSQL/Supabase before running the app (app continues in limited mode if the table is missing).

## Instruction files to keep in sync
`AGENTS.md`, `agent.md`, `plan.md`, `todos.md` — update together when conventions change.
