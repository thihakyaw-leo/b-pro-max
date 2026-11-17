# AGENTS.md — b-pro-max

WinUI 3 (.NET 9) desktop app (VB6 lottery/agent) – Windows only, requires registered Windows App Runtime.

## Toolchain
- SDK `9.0.100` (root `global.json`), rollForward: latestFeature → resolves to 9.0.321; do **not** use `latestMajor`.
- Target `net9.0-windows10.0.19041.0`, `Platform=x64`, `RuntimeIdentifiers=win-x64`.
- `TreatWarningsAsErrors=true`; only `CA1707` exempt. `CS1998` (async without await) is error.
- Analyzers: SDK (`latest-recommended`, `Recommended`, `EnforceCodeStyleInBuild=true`) + `Meziantou.Analyzer`. `.editorconfig` (eol=lf) is source of truth.
- `System.Drawing.Common 8.0.0` pinned – do not downgrade (EPPlus vs System.Windows.Extensions conflict).

## XAML
- XAML compilation on; `InitializeComponent()` called in view constructors.
- Use `FindChildByName<T>()`; set `AutomationProperties.AutomationId` for UI automation.
- Resolve brushes via `src/Views/ThemeBrush.cs` (`ThemeBrush.Success`, `.Error`, `.Warning`) – do **not** use removed WinUI 2 keys or `Resources["X"] as Brush ?? fallback` (throws COMException).

## WinUI 3 theme resources (crash‑prone)
- `Application.Current.Resources[key]` throws if key missing; use `TryGetValue` fallback via `ThemeBrush`.
- Use `{ThemeResource SuccessTextBrush}` etc.; never the removed WinUI 2 keys.

## Line endings
- Working tree must be LF (`eol=lf` via `.gitattributes` + `.editorconfig`). `core.autocrlf=true` without `.gitattributes` causes CRLF and `dotnet format --verify-no-changes` failure. Fix drift with `git add --renormalize .`.

## Project layout
- `src/b-pro-max.csproj` – WinExe, `UseWinUI=true`. Entry: `src/Program.cs` ([STAThread] Main) → `App.xaml.cs` (DI + config, `Initialize()`) → `MainWindow` (`src/Views/MainWindow.cs`).
- `src/Views/` – WinUI views (`.xaml` + `.cs`, pre‑generated `.g.cs`).
- `tests/b-pro-max.tests.csproj` – xUnit unit tests, references src, excludes `e2e/**` (CS0579 duplicate assembly attributes).
- `tests/e2e/b-pro-max.e2e.csproj` – separate xUnit e2e project (Npgsql); run independently.
- `Scripts/001_agents.sql` … `Scripts/006_access_requests.sql` – PostgreSQL schema scripts.

## Commands (run from repo root)
```bash
# Quality pass (CI order)
dotnet format --verify-no-changes
dotnet build src\b-pro-max.csproj -c Release --no-incremental
dotnet build tests\b-pro-max.tests.csproj -c Release --no-incremental
dotnet test tests\b-pro-max.tests.csproj --no-build
dotnet build tests\e2e\b-pro-max.e2e.csproj -c Release --no-incremental
dotnet test tests\e2e\b-pro-max.e2e.csproj --no-build

# Single test
dotnet test tests\b-pro-max.tests.csproj --filter "FullyQualifiedName~ClassName"
```

Always run `build -> test` before committing; fix warnings instead of suppressing.

## Database / secrets
- `appsettings.json` contains empty‑password placeholder; real credentials are in `.env` (gitignored, loaded by `DotNetEnv.Env.Load()` in `App.xaml.cs`). `.env` is optional – app starts without it. Override via `DATABASE_URL` or `DATABASE_CONNECTION_STRING`. Copy `.env.example` to `.env`; never commit real credentials.
- Create local PostgreSQL `bpro_max` database and run `Scripts/001_agents.sql`. App runs in limited mode if the `agents` table is missing.
- Unit tests are mocked (no DB). E2E tests connect to local `bpro_max_test` (postgres/postgres, port 5432) and self‑create schema; they are skipped when no DB is available. Some e2e tables/columns exist only in `tests/e2e/E2ETestBase.cs`.

## Scratch paths
- `/benchmarks`, `/benchmarks.tmp`, and any stray `src/*` projects (e.g., `src/BenchmarksTest.csproj`, `src/ProgramApp.csproj`) are gitignored – never add them to the solution.

## Gotchas
- Windows desktop only – cannot run on other OS.
- WinUI 3 requires registered Windows App Runtime; app will crash silently on unhandled exceptions (writes to `%LOCALAPPDATA%\b-pro-max\crash.log`).
- Transient `CS2012` file‑lock errors (Defender) may appear mid‑build; retry the build.
- `dotnet format --verify-no-changes` fails on CRLF line endings; ensure LF throughout.