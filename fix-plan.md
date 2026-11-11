# b-pro-max — Fix Plan (Post-Phase 5 Review)

**Status:** Draft — awaiting approval
**Date:** 2026-11-11
**Scope:** Fix the functional gaps found in the post-login review, then complete Phase 6.
**Baseline:** .NET 9 WinUI 3, local PostgreSQL only, 107 tests passing, 0 warnings.

---

## 1. Goals

1. Make login usable end-to-end on a fresh installation.
2. Persist users, license, time-lock settings, and auto-buy rules.
3. Complete the missing sales data path so filters and reports show real data.
4. Harden backup/restore, input validation, and password storage.
5. Wire the existing Excel/CSV/PDF export services into the UI.
6. Wire Myanmar regional settings and the Pyidaungsu font into the UI and PDF output.
7. Add tests for every fix and close Phase 6 (E2E, benchmarks, docs, audit).

**Exit criteria:** On a clean Windows machine, the operator can create an admin account, log in, manage agents and sales, export reports, back up and restore, change regional settings to Myanmar, restart the app, and find all data and settings intact — with 0 build warnings and all tests green.

---

## 2. Findings Being Fixed

| # | Finding | Severity |
|---|---|---|
| 1 | `RegisterCredential()` is never called; a fresh app has no users, so login is impossible | P0 |
| 2 | App startup does not navigate to `LoginView`; the content frame is empty | P0 |
| 3 | Login success navigates content but does not update the NavigationView selection | P1 |
| 4 | `UPDATE agents` omits `agent_name`, so name edits are silently lost | P1 |
| 5 | There is no "New Agent" entry point in the UI | P1 |
| 6 | `GetAllSalesAsync()` returns an empty list; no sales table exists | P1 |
| 7 | Auto-buy rules live only in memory and vanish on restart | P1 |
| 8 | Excel/CSV services are registered but unused; PDFs are never saved; Export button is a placeholder | P1 |
| 9 | Password hashing uses fast SHA-256 instead of a password KDF | P2 |
| 10 | License state is in memory only, and `licensed` is never used to gate features | P2 |
| 11 | Time lock is never enforced at login or navigation | P2 |
| 12 | Backup is plain `pg_dump`; restore uses `pg_restore` (format mismatch) | P2 |
| 13 | `CREATE DATABASE` interpolates an unvalidated name | P2 |
| 14 | Startup only checks for tables; it does not create them | P2 |
| 15 | Time-lock out-of-range input can throw an unhandled exception | P2 |
| 16 | Sound slider does not reflect config and cannot control `Console.Beep` volume | P3 |
| 17 | Regional/Myanmar settings have no UI and are not used by views | P3 |
| 18 | PDF output uses Verdana and cannot render Myanmar Unicode | P3 |
| 19 | `AutomationId` coverage and E2E tests are incomplete | P3 |
| 20 | Benchmarks are not restored as a proper project | P3 |

---

## 3. Fix Items

### P0 — Blockers

#### FIX-01 — Persistent users + first-run admin setup

**Problem:** `AuthService` stores credentials in a `Dictionary`; `RegisterCredential()` is public but never called. No user exists after a normal start, and lockout resets on restart.

**Steps**
1. Add `Scripts/002_users.sql`:
   ```sql
   CREATE TABLE IF NOT EXISTS users (
       id              SERIAL PRIMARY KEY,
       username        TEXT NOT NULL,
       password_hash   BYTEA NOT NULL,
       salt            BYTEA NOT NULL,
       iterations      INTEGER NOT NULL DEFAULT 210000,
       failed_attempts INTEGER NOT NULL DEFAULT 0,
       locked_until    TIMESTAMPTZ NULL,
       created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
       updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
   );
   CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username
       ON users (LOWER(username));
   ```
2. Add `src/Models/User.cs` (id, username, hash, salt, iterations, failed attempts, locked until).
3. Extend `IAuthService` with asynchronous operations:
   - `Task<AuthResult> LoginAsync(string username, string password)`
   - `Task RegisterCredentialAsync(string username, string password)`
   - `Task<bool> AnyUserExistsAsync()`
4. Add `AuthResult` record: `Success`, `FailureReason` (`Invalid`, `LockedOut`, `EmptyInput`, `NotProvisioned`), `LockedUntil`.
5. Rework `AuthService` to read/write `users` through `IDatabaseService`; hash with PBKDF2 (see FIX-09); compare with `CryptographicOperations.FixedTimeEquals`.
6. Persist lockout: update `failed_attempts` and `locked_until` in the database. Reset both on success.
7. Add first-run UX:
   - `LoginView` checks `AnyUserExistsAsync()` in `OnNavigatedTo`.
   - If no users, show a "Create admin account" button that opens a new `SetupAdminView` (username, password, confirm).
   - On success, return to `LoginView`.
8. Keep DI registration as a singleton.

**Files:** `Scripts/002_users.sql`, `src/Models/User.cs`, `src/Services/IAuthService.cs`, `src/Services/AuthService.cs`, `src/Views/LoginView.xaml(.cs)`, `src/Views/SetupAdminView.xaml(.cs)`, `src/App.xaml.cs`, `tests/AuthServiceTests.cs`.

**Tests**
- Create user → login succeeds.
- Wrong password → fails and increments the counter.
- 5 failures → locked for 15 minutes.
- Lockout expires → login succeeds again.
- Duplicate username (case-insensitive) → rejected.
- Empty username/password → rejected.
- User persists after service re-instantiation.

**Acceptance:** Fresh database → setup admin screen → create account → login → app restart → same account can log in; wrong password 5 times → locked.

---

#### FIX-02 — Navigate to login on startup

**Problem:** `MainWindow` never navigates the content frame; the window opens with an empty content area.

**Steps**
1. In `MainWindow` constructor or a `ContentFrame.Loaded` handler:
   - `ContentFrame.Navigate(typeof(LoginView));`
   - `NavView.SelectedItem = NavLogin;`
   - `UpdateMenuState();`
2. Guard against duplicate navigation if the frame already has content.
3. Keep the login page as the only startup page; do not restore a previous page.

**Files:** `src/MainWindow.xaml.cs`.

**Tests:** Manual + E2E (Windows host); assert `LoginView` is visible on launch.

**Acceptance:** Launching the app shows the login page immediately.

---

#### FIX-03 — Login state and navigation consistency

**Problem:** After login, the content shows `AgentListView` but the NavigationView still highlights `Login`; failure messages do not distinguish lockout from bad credentials.

**Steps**
1. Add `MainWindow.NavigateToAuthenticatedHome()` that selects `NavAgents` and navigates to `AgentListView`.
2. `LoginView` calls this method instead of navigating its own frame directly.
3. Use the `AuthResult` from FIX-01 to show:
   - `Invalid username or password.`
   - `Account locked. Try again after HH:mm.`
4. Disable the Login button while the request is in flight; re-enable on completion.
5. Support Enter key in username/password fields.
6. On logout, select `NavLogin` explicitly.

**Files:** `src/MainWindow.xaml.cs`, `src/Views/LoginView.xaml(.cs)`.

**Tests:** Result-to-message mapping unit tests; E2E navigation check.

**Acceptance:** Menu highlight always matches the displayed page; lockout message shows the remaining time.

---

### P1 — Data Integrity and Core Features

#### FIX-04 — Agent update, validation, and creation entry point

**Problem:** `UPDATE agents` does not set `agent_name`; no UI path creates a new agent; delete reports success even when 0 rows were affected.

**Steps**
1. Add `agent_name = @agent_name` to the UPDATE statement in `AgentService.UpdateAgentAsync`.
2. Validate in `AgentDetailView`:
   - Agent name is required, trimmed, max 200 characters.
   - Commission/rates are non-negative; rates are 0–100.
   - Numeric parsing uses a culture-aware parser and reports per-field errors.
3. Check affected row counts: update/delete of a missing ID shows "Agent not found."
4. Add a **New Agent** button to `AgentListView` that navigates to `AgentDetailView` without a parameter.
5. Remove the unused `_editAgentId` field in `AgentListView`.
6. Add a duplicate-name check (optional warning, not a hard block unless the product requires uniqueness).

**Files:** `src/Services/AgentService.cs`, `src/Views/AgentListView.xaml(.cs)`, `src/Views/AgentDetailView.xaml(.cs)`, `tests/AgentServiceTests.cs`.

**Tests:** Rename persists; empty name rejected; negative commission rejected; update/delete of a missing ID returns 0 and shows the right message; create button navigates.

**Acceptance:** Edit an agent name → save → list shows the new name; New Agent creates a record.

---

#### FIX-05 — Sales table and data layer

**Problem:** `SaleRecord` exists only in `SalesFilterService.cs`; `GetAllSalesAsync()` returns an empty list; filters and reports have no real data.

**Steps**
1. Add `Scripts/003_sales.sql`:
   ```sql
   CREATE TABLE IF NOT EXISTS sales (
       id             BIGSERIAL PRIMARY KEY,
       agent_id       INTEGER NOT NULL REFERENCES agents(id) ON DELETE RESTRICT,
       sale_date      TIMESTAMPTZ NOT NULL,
       amount         NUMERIC(14,2) NOT NULL DEFAULT 0,
       commission     NUMERIC(14,2) NOT NULL DEFAULT 0,
       commission1    NUMERIC(14,2) NOT NULL DEFAULT 0,
       currency_type  TEXT,
       currency_rate  NUMERIC(12,4) NOT NULL DEFAULT 1,
       status         TEXT NOT NULL DEFAULT 'Pending'
                      CHECK (status IN ('Pending','Paid','Cancelled')),
       created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
       updated_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
   );
   CREATE INDEX IF NOT EXISTS idx_sales_sale_date ON sales(sale_date);
   CREATE INDEX IF NOT EXISTS idx_sales_agent_id ON sales(agent_id);
   CREATE INDEX IF NOT EXISTS idx_sales_status ON sales(status);
   ```
2. Move `SaleRecord` to `src/Models/SaleRecord.cs`; add `AgentId`.
3. Add `ISalesService` with async `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
4. Implement `SalesService` using `IDatabaseService`; join `agents` to populate `AgentName`:
   ```sql
   SELECT s.*, a.agent_name
   FROM sales s
   JOIN agents a ON a.id = s.agent_id
   ORDER BY s.sale_date DESC;
   ```
5. Replace `SalesFilterService.GetAllSalesAsync()` with a call to `ISalesService.GetAllAsync()` (or remove the method and update callers).
6. Add a minimal `SalesEntryView` (or an Add/Edit panel in `SalesFilterView`) with agent picker, date, amount, commission, status.
7. Wire the new page into `MainWindow` navigation.
8. Seed 3–5 sample sales only in development seeding, not in production.

**Files:** `Scripts/003_sales.sql`, `src/Models/SaleRecord.cs`, `src/Services/ISalesService.cs`, `src/Services/SalesService.cs`, `src/Services/SalesFilterService.cs`, `src/Views/SalesFilterView.xaml(.cs)`, `src/Views/SalesEntryView.xaml(.cs)`, `src/MainWindow.xaml` / `.xaml.cs`, `src/App.xaml.cs`, `tests/SalesFilterServiceTests.cs`, `tests/SalesServiceTests.cs`.

**Tests:** Mapping with a real database (integration), filter combinations, totals, create/update/delete, sales report includes data.

**Acceptance:** Create 3 sales → filter by date/agent/status/amount → totals match; report and CSV/Excel include the rows.

---

#### FIX-06 — Persist auto-buy rules

**Problem:** `AutoBuyService` uses an in-memory `List<AutoBuyRule>`; rules disappear on restart, and `CheckTrigger()` is never called.

**Steps**
1. Add `Scripts/004_autobuy.sql`:
   ```sql
   CREATE TABLE IF NOT EXISTS auto_buy_rules (
       id                SERIAL PRIMARY KEY,
       agent_id          INTEGER NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
       trigger_amount    NUMERIC(14,2) NOT NULL,
       auto_buy_amount   NUMERIC(14,2) NOT NULL,
       is_enabled        BOOLEAN NOT NULL DEFAULT TRUE,
       created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
       last_executed     TIMESTAMPTZ NULL,
       execution_count   INTEGER NOT NULL DEFAULT 0
   );
   CREATE UNIQUE INDEX IF NOT EXISTS ux_auto_buy_agent ON auto_buy_rules(agent_id);
   ```
2. Convert `IAutoBuyService` to async database-backed methods (`Task<List<AutoBuyRule>> GetAllRulesAsync()` etc.).
3. Keep `CheckTrigger` as a pure evaluation method; add a documented integration point for the future sales-entry flow.
4. Update `AutoBuyView` to `await` the new methods and handle duplicates.
5. Add a "Run check" button that evaluates all enabled rules against the latest sale totals (optional, only if sales entry exists).

**Files:** `Scripts/004_autobuy.sql`, `src/Services/IAutoBuyService.cs`, `src/Services/AutoBuyService.cs`, `src/Views/AutoBuyView.xaml(.cs)`, `tests/AutoBuyServiceTests.cs`.

**Tests:** CRUD persistence, unique rule per agent, toggle persists, trigger logic boundary values.

**Acceptance:** Create a rule → restart app → rule still exists.

---

#### FIX-07 — Wire reporting and export

**Problem:** Excel/CSV services are registered but unused; PDFs are generated into memory and discarded; the Export button says "not yet implemented."

**Steps**
1. Add to `ReportView`:
   - Report type: Agents / Sales.
   - Date range for sales.
   - Format: PDF / Excel / CSV.
   - **Export** button opens `FileSavePicker` and writes the generated bytes.
2. Wire `IExcelExportService.ExportAgentsToExcelAsync` / `ExportSalesToExcelAsync` and the CSV equivalents.
3. Save PDFs with a suggested filename (`agents-yyyyMMdd.pdf`).
4. Add a PdfSharpCore font resolver that registers `Pyidaungsu` (and a fallback) so Myanmar text renders in PDFs.
5. Use `IRegionalSettingsService` for date/currency formatting in exports.
6. Show success with the saved path; show errors with the exception message.

**Files:** `src/Views/ReportView.xaml(.cs)`, `src/Services/ReportService.cs`, `src/Services/ExcelExportService.cs`, `src/Services/CsvExportService.cs`, `src/Helpers/PdfFontResolver.cs`, `tests/CsvExportServiceTests.cs`, `tests/ExcelExportServiceTests.cs`, `tests/ReportServiceTests.cs`.

**Tests:** CSV header/row counts, Excel bytes non-empty, PDF starts with `%PDF`, file save writes bytes, Myanmar text in PDF (manual on Windows).

**Acceptance:** Each format saves a valid file; sales report includes database rows.

---

### P2 — Security and Correctness

#### FIX-08 — Upgrade password hashing (implement with FIX-01)

**Problem:** SHA-256 is a fast hash and is not suitable for password storage.

**Steps**
1. Use `Rfc2898DeriveBytes.Pbkdf2` with HMAC-SHA256, 210,000 iterations, 16-byte salt, 32-byte hash.
2. Store `iterations` per user so the cost can be raised later.
3. Compare with `CryptographicOperations.FixedTimeEquals`.
4. Rehash on successful login when the stored iteration count is below the current policy.
5. Never log passwords or hashes.

**Files:** `src/Services/AuthService.cs`, `src/Models/User.cs`, tests.

**Tests:** Verify correct password, reject wrong password, unique salt per user, rehash path.

**Acceptance:** No SHA-256 password hashing remains.

---

#### FIX-09 — Persist and enforce licensing

**Problem:** License state is memory-only; `licensed` is computed in `UpdateMenuState` but never used.

**Steps**
1. Add `app_settings` table:
   ```sql
   CREATE TABLE IF NOT EXISTS app_settings (
       key        TEXT PRIMARY KEY,
       value      TEXT NOT NULL,
       updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
   );
   ```
2. Add `ISettingsStore` (`GetAsync`, `SetAsync`, `DeleteAsync`) backed by `IDatabaseService`.
3. `LicensingService` loads the key at startup and persists on successful registration.
4. Decide the gating policy (see Decisions). Recommended: unregistered users can manage agents; Reports, Export, Backup, and Maintenance require a license.
5. `UpdateMenuState` enables licensed-only items only when `_licensing.IsRegistered`.
6. Add a visible "Unregistered" banner with a Register link.

**Files:** `Scripts/005_settings.sql`, `src/Services/ISettingsStore.cs`, `src/Services/SettingsStore.cs`, `src/Services/LicensingService.cs`, `src/MainWindow.xaml(.cs)`, `src/Views/RegisterView.cs`, `tests/LicensingServiceTests.cs`.

**Tests:** Register → restart → still registered; invalid key not persisted; menu gating logic.

**Acceptance:** License survives restart; unregistered users cannot open gated pages.

---

#### FIX-10 — Enforce time lock and persist settings

**Problem:** Time lock is only checked when the user presses "Check"; settings are in memory.

**Steps**
1. Persist `TimeLock:Enabled`, `StartHour`, `EndHour` via `ISettingsStore`.
2. Enforce in `AuthService.LoginAsync` or a login guard:
   - If enabled and outside `[StartHour, EndHour)` → return `LockedOut` with reason `OutsideAllowedHours`.
   - Show the allowed window in the message.
3. Record audit entries in `access_requests` (timestamp, username, reason, granted).
4. Validate 0–23 in the UI before calling setters; catch service exceptions.
5. Add a note that overnight windows (e.g., 22–06) are not supported unless explicitly implemented.

**Files:** `src/Services/TimeLockService.cs`, `src/Services/IAuthService.cs`, `src/Views/TimeLockView.xaml(.cs)`, `src/Views/LoginView.xaml(.cs)`, `Scripts/006_access_requests.sql`, tests.

**Tests:** Boundary 09:00/17:00, disabled state, denied login, persisted settings, invalid hour input.

**Acceptance:** Outside allowed hours, login is denied with a clear message; settings survive restart.

---

#### FIX-11 — Correct backup/restore format and process handling

**Problem:** `pg_dump` defaults to plain SQL, while `pg_restore` expects custom/tar/directory format; `PGPASSWORD` is set process-wide.

**Steps**
1. Use `pg_dump -Fc` (custom format) for backups.
2. Pass `PGPASSWORD` through `ProcessStartInfo.Environment` only.
3. Add `--no-owner --no-privileges` to both commands.
4. Validate the backup file exists and is non-empty before restore.
5. Capture stderr and exit code; return a result object with the error message.
6. Use `pg_restore --clean --if-exists --no-owner --no-privileges`.
7. Add a timeout (e.g., 5 minutes) and cancel handling.
8. Integration test with a temporary database: insert sentinel row → backup → delete row → restore → verify.

**Files:** `src/Services/DatabaseMaintenanceService.cs`, `tests/DatabaseMaintenanceServiceTests.cs`.

**Tests:** Real round-trip on local PostgreSQL; invalid file returns a clean failure; no process-wide password leakage.

**Acceptance:** Backup → modify → restore → original data returns.

---

#### FIX-12 — Validate database creation names

**Problem:** `CREATE DATABASE "{databaseName}"` uses unvalidated input.

**Steps**
1. Validate with `^[a-z_][a-z0-9_]{0,62}$` (case-insensitive, normalize to lower case).
2. Use `NpgsqlCommandBuilder.QuoteIdentifier` for the identifier.
3. Reject reserved names (`postgres`, `template0`, `template1`).
4. Optionally create the `agents` schema in the new database.
5. Return a result object with a specific error message.

**Files:** `src/Services/DatabaseMaintenanceService.cs`, `src/Views/CreateDatabaseView.xaml(.cs)`, tests.

**Tests:** Valid name creates; invalid/injection-like names rejected; duplicate name returns a clear error.

**Acceptance:** No raw interpolation of user input into SQL.

---

#### FIX-13 — Startup migration and retry behavior

**Problem:** Startup only checks for the `agents` table and logs a warning if missing; `_migrationChecked` is set before the work succeeds.

**Steps**
1. In `InitializeDatabaseAsync`, call `EnsureTablesCreatedAsync()` before `GetStatusAsync()`.
2. Run all idempotent scripts (`001`–`006`) from a migration runner or replicate them in `DatabaseMigrationService`.
3. Seed only when the table is empty.
4. Set `_migrationChecked = true` only after a successful run; allow retry after failure.
5. Show a non-blocking banner if the database is unavailable; keep the login page usable.

**Files:** `src/App.xaml.cs`, `src/Services/DatabaseMigrationService.cs`, `src/MainWindow.xaml(.cs)`.

**Tests:** Empty database → tables created; failure → retry works; seed skipped when data exists.

**Acceptance:** Fresh database starts with all tables and no manual script step.

---

#### FIX-14 — Time-lock and sound UI correctness

**Problem:** Out-of-range hours can throw; the sound slider does not reflect config and does not control beep volume.

**Steps**
1. Time lock: validate in the view, wrap setters in try/catch, show field-level errors.
2. Sound: load the configured volume into the slider.
3. Decide volume behavior:
   - **Option A (recommended):** remove the volume slider, keep the enable toggle, and document that system beep volume is controlled by Windows.
   - **Option B:** play WAV files through `System.Media.SoundPlayer` and add a Windows volume API (e.g., NAudio) if per-app volume is required.
4. Persist sound settings through `ISettingsStore`.

**Files:** `src/Views/TimeLockView.xaml(.cs)`, `src/Views/SoundSettingsView.xaml(.cs)`, `src/Services/SoundService.cs`, tests.

**Tests:** Invalid hour shows an error and does not throw; slider reflects saved volume; settings persist.

**Acceptance:** No unhandled exceptions from settings pages; UI does not promise volume control it cannot deliver.

---

### P3 — UX, Myanmar Support, and Phase 6

#### FIX-15 — Regional settings page and application wiring

**Problem:** `IRegionalSettingsService` has no UI and is not used by any view.

**Steps**
1. Add `RegionalSettingsView` with:
   - Culture combo: `en-US`, `my-MM`, `th-TH`.
   - Date format, date-time format, currency symbol/code, decimal digits.
   - First day of week, 24-hour toggle.
   - Font family (default `Segoe UI`, Myanmar `Pyidaungsu`).
   - Buttons: **Apply Myanmar preset**, **Apply Pyidaungsu font**, **Save**.
2. Persist values through `ISettingsStore`.
3. Apply the font at the window root (`FontFamily` on the content root) or via a theme resource.
4. Use `IRegionalSettingsService.FormatDate/FormatDateTime/FormatCurrency` in agent list/detail, sales, and reports.
5. Add a nav item under a new "Settings" group.

**Files:** `src/Views/RegionalSettingsView.xaml(.cs)`, `src/MainWindow.xaml(.cs)`, `src/App.xaml.cs`, `src/Services/RegionalSettingsService.cs`, `src/Configuration/RegionalSettings.cs`, tests.

**Tests:** Preset values, format outputs, persistence, font selection.

**Acceptance:** Selecting Myanmar changes date/currency formatting and font immediately; restart keeps the setting.

---

#### FIX-16 — Myanmar font detection and fallback

**Problem:** Pyidaungsu may not be installed; PDFs cannot render Myanmar text.

**Steps**
1. On startup, check `C:\Windows\Fonts\Pyidaungsu.ttf` (and the per-user font directory).
2. If missing, show a warning with installation instructions and fall back to `Myanmar Text` / `Segoe UI`.
3. Register the Pyidaungsu font with the PDF font resolver (FIX-07).
4. Update README with exact installation steps and verification.

**Files:** `src/Services/FontAvailabilityService.cs`, `src/Helpers/PdfFontResolver.cs`, `src/Views/RegionalSettingsView.xaml(.cs)`, `README.md`.

**Tests:** Detection logic with mocked paths; PDF generation with the font (manual on Windows).

**Acceptance:** With Pyidaungsu installed, Myanmar UI and PDF text render correctly; without it, the app warns instead of showing boxes.

---

#### FIX-17 — AutomationId audit and E2E tests

**Problem:** UI automation coverage is incomplete and E2E tests cannot run in the Linux container.

**Steps**
1. Audit every interactive control for a stable `AutomationProperties.AutomationId`.
2. Add E2E tests (Windows host) for:
   - First-run admin setup.
   - Login success/failure/lockout.
   - Agent create/edit/delete.
   - Sales create/filter/report export.
   - Backup/restore.
   - Regional settings switch.
3. Document the Windows test prerequisites.

**Files:** `src/Views/*.xaml`, `tests/e2e/*`.

**Tests:** E2E suite green on Windows.

**Acceptance:** Full critical-path E2E run passes on a Windows desktop.

---

#### FIX-18 — Benchmarks

**Problem:** The benchmark suite was removed/local-only and is not tracked.

**Steps**
1. Create `src/Benchmarks/Benchmarks.csproj` with BenchmarkDotNet.
2. Add benchmarks for:
   - Agent list query with 10,000 rows.
   - Sales filter and totals with 100,000 rows.
   - PDF/Excel/CSV export.
3. Add a short results section to the docs.

**Files:** `src/Benchmarks/*`, solution file.

**Acceptance:** `dotnet run -c Release --project src/Benchmarks` produces a report.

---

#### FIX-19 — Documentation, deployment scripts, final audit

**Steps**
1. Update `README.md`: first-run setup, users, license, time lock, backup/restore, Myanmar font.
2. Add `scripts/setup-db.ps1`, `scripts/backup.ps1`, `scripts/restore.ps1`.
3. Add a deployment checklist (publish, prerequisites, PostgreSQL service).
4. Run the final audit: no VB6, ActiveX, Registry, or original-binary dependencies (and no FastReport/Crystal Reports packages).
5. Update `todos.md` and close Phase 6.

**Acceptance:** A clean Windows machine can be set up by following the docs only; audit checklist is complete.

---

## 4. Execution Order and Milestones

| Milestone | Items | Outcome |
|---|---|---|
| **M1 — Usable app** | FIX-01, FIX-02, FIX-03 | Fresh install → create admin → login → agent list |
| **M2 — Core data** | FIX-04, FIX-05, FIX-06, FIX-07 | Agent edits persist; sales data exists; exports work |
| **M3 — Security/correctness** | FIX-08 (with 01), FIX-09, FIX-10, FIX-11, FIX-12, FIX-13, FIX-14 | Persistent license/time lock; safe backup; auto-migration |
| **M4 — Phase 6 close** | FIX-15, FIX-16, FIX-17, FIX-18, FIX-19 | Myanmar UI, E2E, benchmarks, docs, audit |

**Dependencies**
- FIX-01 must land before FIX-09/FIX-10 (settings and login integration).
- FIX-05 must land before FIX-06 (trigger evaluation) and FIX-07 (sales exports).
- FIX-15 before FIX-16 (UI selects the font).
- FIX-17 after UI changes stabilize.

**Suggested sequencing:** M1 → M2 → M3 → M4, one milestone per branch/PR. Run the full quality gate after each item.

### Rough Effort Estimate (single developer)

| Milestone | Estimate |
|---|---|
| M1 — Usable app | 2–3 days |
| M2 — Core data | 5–7 days |
| M3 — Security/correctness | 5–7 days |
| M4 — Phase 6 close | 6–9 days |
| **Total** | **18–26 developer-days** |

Estimates exclude Windows-host E2E execution time and any product-decision delays.

---

## 5. Test Strategy

| Layer | Tooling | Scope |
|---|---|---|
| Unit | xUnit + Moq | Services, formatting, validation, result mapping |
| Integration | xUnit + local PostgreSQL `bpro_max_test` | SQL, migrations, backup/restore round-trip |
| UI/E2E | WinAppDriver on Windows | Critical user journeys |
| Manual | Windows desktop | Font rendering, file pickers, dialogs |

**Quality gate after every change**
```cmd
dotnet format --verify-no-changes
dotnet build b-pro-max.sln -c Release
dotnet test tests\b-pro-max.tests.csproj --no-build
```

**Definition of done for each item**
- Code + tests + docs updated.
- 0 errors, 0 warnings.
- New tests pass; full suite still passes.
- Acceptance criteria demonstrated.
- No new analyzer suppressions unless documented.

---

## 6. Decisions Required

| # | Decision | Recommended default |
|---|---|---|
| D1 | User provisioning model | First-run admin setup screen (no hard-coded default password) |
| D2 | License gating policy | Agents/Sales free; Reports/Export/Backup/Maintenance require license |
| D3 | Time-lock behavior outside hours | Deny login with a clear message |
| D4 | Auto-buy execution trigger | Evaluate when a sale is created; no background timer yet |
| D5 | Sound volume | Remove the slider (system beep volume) or add NAudio/WAV; recommend removing for now |
| D6 | Overlapping time-lock windows | Not supported in v1; document 0–23 same-day windows only |
| D7 | Sales entry UI | Minimal Add/Edit panel in SalesFilterView (no separate page) |

---

## 7. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Windows-only UI/E2E validation is blocked in this container | M4 delay | Mark as Windows-host tasks; unit-test all non-UI logic |
| Schema changes break existing local data | Data loss | Idempotent `IF NOT EXISTS` scripts; back up before migration; transactions |
| `pg_dump`/`pg_restore` not on PATH | Backup fails | Detect at startup; show installation guidance |
| License policy undefined | Rework | Confirm D2 before FIX-09 |
| First-run flow changes login UX | E2E churn | Implement FIX-01/02/03 together before E2E |
| PBKDF2 cost too high on old hardware | Slow login | Make iterations configurable; measure and tune |

---

## 8. Out of Scope (for this plan)

- Reintroducing Supabase or any cloud backend.
- Full localization resource files (`resw`) for every string; only regional formatting and font support are in scope.
- Background auto-buy scheduler.
- Overnight time-lock windows.
- Multi-user roles/permissions beyond admin vs. operator.
- Migrating the legacy binary data automatically (manual import remains a separate task).

---

## 9. First Iteration (recommended next action)

1. **FIX-01** — `users` table, PBKDF2, DB-backed `AuthService`, first-run admin screen.
2. **FIX-02** — startup navigation to `LoginView`.
3. **FIX-03** — login result messages and menu selection.
4. **FIX-13** — create missing tables at startup.
5. **FIX-04** — agent name update + New Agent button.

Deliver M1 first, verify on Windows, then proceed to M2.
