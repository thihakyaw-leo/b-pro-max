# AGENTS.md — b-pro-max

## What this repo is
Modern WinUI 3 reimplementation of DD.exe (VB6 lottery/agent management). .NET 9 + C#, Windows App SDK / WinUI 3, PostgreSQL via Npgsql (local only — the Supabase REST path was removed), EPPlus/CsvHelper/PdfSharpCore for export/reporting, JSON config (`appsettings.json` + `.env` secrets via DotNetEnv), xUnit tests.

## Toolchain
- `global.json`: pins .NET SDK `9.0.100`, `rollForward: latestFeature` → resolves to **9.0.321** on this machine. Do **not** use `latestMajor`: it selects the highest installed SDK and picks up preview SDKs (e.g. 11.0-rc), which emit `NETSDK1057`. Keep the pin in the 9.0.x line to match CI's `dotnet-version: 9.0.x`.
- All projects target `net9.0-windows10.0.19041.0`, `Platform=x64`, `RuntimeIdentifiers=win-x64`. **Windows desktop only** — WinRT activation fails (`REGDB_E_CLASSNOTREG`) in containers/non-Windows.
- `TreatWarningsAsErrors=true` — warnings fail the build. Only exception: `WarningsNotAsErrors=CA1707` (test identifier naming). This includes `CS1998`: an `async Task` method with no `await` is a **build error**, so sync-only test methods must be declared `void`, not `async Task`.
- Analyzers: SDK (`AnalysisLevel=latest-recommended`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`) + `Meziantou.Analyzer 3.0.322`. StyleCop evaluated, not adopted.
- Formatting: `dotnet format` (`.editorconfig` is source of truth, `end_of_line = lf`).
- **CI:** `.github/workflows/dotnet.yml` runs on `windows-latest` (push/PR to `main`) and enforces the full gate: restore → format check → build src → build tests → unit tests → coverage → build e2e → e2e tests. A format or build failure fails the whole workflow, so local `dotnet format --verify-no-changes` is mandatory before pushing.

## Project layout
- `src/b-pro-max.csproj` — app (`OutputType=WinExe`, `UseWinUI=true`). Entry: `src/Program.cs` → `App.Initialize(args)` (DI + config) → `InteractiveMenu`. Composition root: `src/App.cs`.
- `tests/b-pro-max.tests.csproj` — xUnit unit tests, references src. **Excludes `e2e/**`** (Compile Remove) to avoid CS0579 duplicate assembly attributes — e2e is its own project.
- `tests/e2e/b-pro-max.e2e.csproj` — separate xUnit e2e project (Npgsql). Run independently.
- `src/Views/` — WinUI views (`.xaml` + `.xaml.cs`).
- `Scripts/001_agents.sql` … `Scripts/006_access_requests.sql` — PostgreSQL schema scripts (`agents`, `users`, `sales`, `autobuy`, `settings`, `access_requests`).
- `appsettings.json` — config. `.env` (gitignored, untracked) overrides via DotNetEnv; never commit real credentials. `.env` is optional — `DotNetEnv.Env.Load()` returns an empty sequence when the file is absent, so the app still starts. Use `.env.example` as the template.
- `.gitattributes` — forces `* text=auto eol=lf` (plus binary patterns). The working tree must be **LF**: `.editorconfig` sets `end_of_line = lf`, and `core.autocrlf=true` without `.gitattributes` checks files out as CRLF, which fails the CI format gate with `ENDOFLINE`. If line endings drift, run `git add --renormalize .` to reconcile the index.

## Build quirks (easily missed)
1. **XAML compilation is disabled.** `MarkupCompilePass1`/`MarkupCompilePass2` are no-op'd; all `**\*.xaml` removed from compilation; pre-generated `.g.cs` files used instead. Reason: `System.Security.Permissions` v6 unavailable on .NET 9 and `XamlCompiler.exe` crashes silently. Do not add new XAML pages expecting normal compilation.
2. **`System.Drawing.Common 8.0.0` is pinned** to unify an EPPlus/System.Windows.Extensions assembly conflict (MSB3277). Do not downgrade.
3. `Package.appxmanifest` is copied to output as Content.
4. Local scratch paths are gitignored and must never be added to the solution: `/benchmarks`, `/benchmarks.tmp`, `src/BenchmarksTest*`, `src/ProgramApp.csproj`, `src/RunBenchmarks.csproj`, `src/simple_test.csproj`, `src/test_program.csproj`, `src/compile_test.bat`, `src/build_output.txt`, `src/BenchmarksFolder`, `src/Benchmarks_File`. Only the **root** `global.json` is tracked — do not add a `src/global.json`, which would override the SDK pin for the app project.
5. Transient `CS2012` file-lock errors from Microsoft Defender can occur during builds; retry usually succeeds.

## Commands (run from repo root `F:\b\b-pro-max`)
```
# Quality pass, in order (mirrors CI exactly):
dotnet format --verify-no-changes        # formatting check (CI gates on this)
dotnet build b-pro-max.sln -c Release    # whole solution (src + tests + e2e)
dotnet test tests\b-pro-max.tests.csproj # unit tests
dotnet test tests\e2e\b-pro-max.e2e.csproj --no-build   # e2e tests
# Single test:
dotnet test tests\b-pro-max.tests.csproj --filter "FullyQualifiedName~ClassName"
# Run app:
dotnet run --project src\b-pro-max.csproj
```
Always run `build -> test` before committing. `TreatWarningsAsErrors=true` means analyzer/style warnings are build errors — fix the code rather than suppressing.

## Databases / secrets
- `appsettings.json` has a placeholder empty DB password. Real credentials live in `.env` (gitignored, loaded by `DotNetEnv.Env.Load()` in `App.cs`).
- Create the `agents` table via `Scripts/001_agents.sql` against the local PostgreSQL instance before running the app (app continues in limited mode if the table is missing).

## Instruction files to keep in sync
`AGENTS.md`, `agent.md`, `plan.md`, `todos.md` — update together when conventions change.
