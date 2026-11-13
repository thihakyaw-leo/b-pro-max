# AGENTS.md — b-pro-max

WinUI 3 (.NET 9) desktop reimplementation of a VB6 lottery/agent app. Local PostgreSQL only (Npgsql); Windows desktop only — WinRT activation (`REGDB_E_CLASSNOTREG`) fails outside Windows.

## Toolchain (verified in global.json / Directory.Build.props / csproj)
- `global.json` pins .NET SDK `9.0.100` with `rollForward: latestFeature` (resolves to 9.0.321 here). Do **not** switch to `latestMajor` — preview SDKs (e.g. 11.0-rc) fail with `NETSDK1057`. Only the root `global.json` is tracked; never add `src/global.json` (gitignored).
- All projects target `net9.0-windows10.0.19041.0`, `Platform=x64`, `RuntimeIdentifiers=win-x64`.
- `TreatWarningsAsErrors=true` — warnings fail the build. Only exception: `WarningsNotAsErrors=CA1707`. `CS1998` (`async` without `await`) is an error: don't mark sync-only methods `async Task`.
- Analyzers: SDK (`latest-recommended`, `Recommended`, `EnforceCodeStyleInBuild=true`) + `Meziantou.Analyzer`. `.editorconfig` is the source of truth (`end_of_line = lf`).
- `System.Drawing.Common 8.0.0` is pinned — do not downgrade (EPPlus vs System.Windows.Extensions conflict, MSB3277).
- Transient `CS2012` file-lock errors (Microsoft Defender) can occur mid-build; retry.

## XAML
XAML compilation is disabled: `MarkupCompilePass1`/`2` are no-ops, `**\*.xaml` is removed from compilation, and pre-generated `.g.cs` files are used (`System.Security.Permissions` v6 is missing on .NET 9; `XamlCompiler.exe` crashes silently). **Do not add new XAML pages expecting normal compilation.** Controls are resolved at runtime via `FindChildByName<T>()` — there are no `InitializeComponent()` calls yet.

## Line endings
Working tree must be **LF** (`.gitattributes` + `.editorconfig` enforce `eol=lf`). `core.autocrlf=true` without `.gitattributes` checks out CRLF and fails `dotnet format --verify-no-changes` with `ENDOFLINE`. If endings drift: `git add --renormalize .`.

## Project layout
- `src/b-pro-max.csproj` — app (`OutputType=WinExe`, `UseWinUI=true`). Entry: `src/Program.cs` (`[STAThread] Main`) → `App.xaml.cs` (DI + config, `Initialize()`) → `MainWindow` (`src/Views/MainWindow.cs`).
- `src/Views/` — WinUI views (`.xaml` + `.cs`, pre-generated `.g.cs`).
- `tests/b-pro-max.tests.csproj` — xUnit unit tests, references src. Excludes `e2e/**` (otherwise CS0579 duplicate assembly attributes).
- `tests/e2e/b-pro-max.e2e.csproj` — separate xUnit e2e project (Npgsql); run independently.
- `Scripts/001_agents.sql` … `Scripts/006_access_requests.sql` — PostgreSQL schema scripts.

## Commands (run from repo root)
```
# Quality pass, in CI order
dotnet format --verify-no-changes
dotnet build src\b-pro-max.csproj -c Release --no-incremental
dotnet build tests\b-pro-max.tests.csproj -c Release --no-incremental
dotnet test tests\b-pro-max.tests.csproj --no-build
dotnet build tests\e2e\b-pro-max.e2e.csproj -c Release --no-incremental
dotnet test tests\e2e\b-pro-max.e2e.csproj --no-build

# Single test
dotnet test tests\b-pro-max.tests.csproj --filter "FullyQualifiedName~ClassName"

# Run app (unpackaged WinUI: RID build is required so the native bootstrapper is deployed)
dotnet run --project src\b-pro-max.csproj -r win-x64
```
Always run `build -> test` before committing. Fix warnings rather than suppressing.

## Database / secrets
- `appsettings.json` has an empty-password placeholder connection string. Real credentials live in `.env` (gitignored, loaded by `DotNetEnv.Env.Load()` in `App.xaml.cs`); `.env` is optional — the app starts without it. Override via `DATABASE_URL` or `DATABASE_CONNECTION_STRING`. Copy `.env.example` to `.env`; never commit real credentials.
- App DB: create the local PostgreSQL `bpro_max` database and run `Scripts/001_agents.sql` against it. The app runs in limited mode if the `agents` table is missing.
- Unit tests are mocked and need no database. E2E tests connect to local `bpro_max_test` (`postgres`/`postgres`, port 5432) and self-create schema; they are skipped when no database is available. Some e2e tables/columns exist only in `tests/e2e/E2ETestBase.cs`, not in `Scripts/`.

## Scratch paths
`/benchmarks`, `/benchmarks.tmp`, and stray `src/*` projects (e.g. `src/BenchmarksTest.csproj`, `src/ProgramApp.csproj`) are gitignored — never add scratch projects to the solution. See `.gitignore`.
