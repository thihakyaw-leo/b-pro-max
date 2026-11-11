# Changelog

## 2026-11-11 — v2.0.0 (M5 + Phase 6)

This release completes the M5 2D sales system and Phase 6 login, migrating the app from a console-style layout to WinUI 3 with direct PostgreSQL access.

### Features: Sales & views
- **2D sales system (M5 complete):** Batch sale building, bet types, number resolution, payout calculation, commissions, draws, sales filtering, and regional settings, with new Draws, Number Wisdom, Report, Sales Filter, and Setup Admin views.
- **WinUI 3 migration:** All 14 views converted to XAML Pages with a navigation shell (MainWindow, App), replacing the previous console-style layout.
- **Login view (Phase 6):** Login page implemented for agent sign-in.

### Features: Tooling & testing
- **Automated testing:** WinUI 3 UI test automation script plus an E2E test suite covering agent CRUD, backup/restore, commission calculation, database compaction, and sales filtering.

### Bug fixes: UI & layout
- Fixed overlapping `Grid.Row` layouts in the Agent List, Agent Search, and Auto Buy views.
- Fixed window sizing and layout issues in the Report and Sales Filter views.
- Applied code review fixes across all views (control automation IDs, naming, consistency).

### Bug fixes: Build & repository
- Fixed a duplicate compile warning that blocked CI builds.
- Fixed CRLF line endings across the repository so the CI formatting gate passes.

### Chores
- Pinned the .NET 9 SDK (9.0.100) and retargeted projects; added `.gitattributes` to enforce LF line endings.
- Added Meziantou.Analyzer with rule severities; promoted CA1852/CA1862 to errors (CA1707 excepted for test naming).
- CI quality workflow: format check, build, test, and coverage gates; Windows App SDK upgrade; E2E project added to the solution.
- Cleaned up benchmark/scratch files; updated `AGENTS.md` and `todos.md`.

### Breaking changes
- Test project retargeted to `net9.0-windows10.0.19041.0` as part of the WinUI 3 migration.
- `SupabaseAgentService` and the Supabase REST path were removed in favor of direct PostgreSQL access via Npgsql.
- Analyzer rules CA1852 and CA1862 are now build errors.

### Upgrading
- If you were using the Supabase path, switch to direct PostgreSQL: create the `agents` table via `Scripts/001_agents.sql` and update your connection string in `appsettings.json`/`.env`.
- CA1852/CA1862 are now build errors; run `dotnet format` to auto-fix most occurrences.
