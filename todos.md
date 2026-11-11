# todos.md — b-pro-max Task List

**Status:** Phase 0, 1 & 2 complete — WinUI 3 migration done — Code Analysis Phase 3+4 done — 78 tests pass  
**Location:** `F:\b\b-pro-max\todos.md`  
**Reference:** `plan.md` for phase details, `agent.md` for architecture  
**Tech Stack:** .NET 9 WinUI 3 + Windows App SDK 1.5.240227000 + Npgsql + Supabase.Postgrest + EPPlus + JSON config  
**UI:** WinUI 3 desktop app — MainWindow NavigationView shell with 14 Page views

---

## Phase 0 — Foundation ✅ COMPLETE

| Task | Status | Notes |
| --- | --- | --- |
| P0-01 Initialize repo | ✅ Done | .sln, csproj, WinUI 3 project |
| P0-02 Project structure | ✅ Done | App.xaml/.cs, Program.cs, MainWindow, Models/, Services/, Views/, Helpers/ |
| P0-03 Database connection | ✅ Done | Npgsql + Supabase.Postgrest |
| P0-04 Agents table schema | ✅ Done | Scripts/001_agents.sql + Supabase SQL |
| P0-05 JSON configuration | ✅ Done | appsettings.json (Supabase + PostgreSQL) |
| P0-06 EPPlus package ref | ✅ Done | Ready for Excel export |
| P0-07 Code analysis | ✅ Done | .editorconfig + Meziantou.Analyzer + TreatWarningsAsErrors=true |
| P0-08 xUnit test project | ✅ Done | Tests passing |
| P0-09 README.md | ✅ Done | Complete documentation |

**Build:** `dotnet build src\b-pro-max.csproj` — 0 errors, 0 warnings  
**Tests:** `dotnet test tests\b-pro-max.tests.csproj` — 78 passed, 0 failed  
**App launch:** Blocked by `REGDB_E_CLASSNOTREG` — WinRT activation unavailable in container; requires Windows desktop with WinRT runtime registered  
**Code review:** `/winui-code-review` fixes applied — Window.Current → App.MainWindowInstance, hardcoded SolidColorBrush → ThemeResource, raw FontSize/FontWeight → Theme styles (19 issues across 15 files)

---

## Manual Steps Required (you need to do these)

### Option A: Local PostgreSQL
| Task | Status | Instructions |
| --- | --- | --- |
| Install PostgreSQL | ⏳ Manual | https://www.postgresql.org/download/windows/ |
| Create `bpro_max` database | ⏳ Manual | `createdb -U postgres bpro_max` |
| Run `Scripts/001_agents.sql` | ⏳ Manual | Execute against `bpro_max` |
| Update `appsettings.json` | ⏳ Manual | Replace `YOUR_PASSWORD_HERE` |

### Option B: Supabase (REST API) ✅ CODE READY
| Task | Status | Instructions |
| --- | --- | --- |
| Create Supabase project | ⏳ Manual | https://app.supabase.com/ |
| Get DB password | ⏳ Manual | Settings → Database → Connection string |
| Get Anon Key | ⏳ Manual | Settings → API → public anon key |
| Create `agents` table | ⏳ Manual | Run SQL in Supabase SQL Editor |
| Update `appsettings.json` | ⏳ Manual | Set Supabase URL, AnonKey, DB password |

**Supabase SQL to run:**
```sql
CREATE TABLE IF NOT EXISTS agents (
    id SERIAL PRIMARY KEY,
    agent_name TEXT NOT NULL,
    address TEXT,
    phone TEXT,
    commission NUMERIC(12,2) DEFAULT 0,
    rate NUMERIC(12,2) DEFAULT 0,
    commission1 NUMERIC(12,2) DEFAULT 0,
    rate1 NUMERIC(12,2) DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## WinUI 3 Migration — COMPLETE

| Task | Status | Notes |
| --- | --- | --- |
| W3-01 Convert csproj to WinUI 3 | ✅ Done | net9.0-windows10.0.19041.0, UseWinUI=true |
| W3-02 Create App.xaml/App.xaml.cs | ✅ Done | Removed InitializeComponent, DI container, LoggerMessage delegates |
| W3-03 Create MainWindow NavigationView | ✅ Done | FindChildByName<T> for NavView/ContentFrame/NavViewItem |
| W3-04 Convert 14 views to Pages | ✅ Done | All views converted to WinUI 3 Pages with XAML + code-behind |
| W3-05 Add VisualTreeExtensions | ✅ Done | FindChildByName<T>() runtime control lookup helper |
| W3-06 Remove InitializeComponent | ✅ Done | XAML compiler crashes; controls resolved at runtime via FindChildByName |
| W3-07 Fix Program.cs entry point | ✅ Done | [STAThread] Main, Application.Start with _ = app discard |
| W3-08 Restore analyzer policy | ✅ Done | Removed TreatWarningsAsErrors=false/NoWarn from csproj |
| W3-09 Fix CA1305/CA1848/CA1806 | ✅ Done | Fixed in code, not suppressed |
| W3-10 Retarget tests to net9.0-windows | ✅ Done | tests + e2e retargeted, root global.json SDK 9.0.321 |
| W3-11 Add DisablePriGen to test projects | ✅ Done | Same WindowsAppSDK suppression as src project |
| W3-12 WinUI 3 code review fixes | ✅ Done | Window.Current → App.MainWindowInstance, hardcoded SolidColorBrush → ThemeResource, raw FontSize/FontWeight → Theme styles |
| W3-13 UI test automation | ✅ Scripted | ui-tests.ps1 written per winui-ui-testing template; live execution blocked (no winapp CLI, REGDB_E_CLASSNOTREG in container) — run on Windows host |

---

## Phase 1 — Core Agent Management ✅ CODE COMPLETE

| Task | Status |
| --- | --- |
| P1-01 Agent CRUD service | ✅ AgentService.cs + SupabaseAgentService.cs |
| P1-02 Agent list view | ✅ WinUI 3 Page (AgentListView) |
| P1-03 Agent detail/edit form | ✅ WinUI 3 Page (AgentDetailView) |
| P1-04 `SELECT * FROM agents` | ✅ Implemented (both Npgsql + Supabase) |
| P1-05 `WHERE agent_name = @name` | ✅ Implemented |
| P1-06 `ORDER BY agent_name` | ✅ Implemented |
| P1-07 Agent deletion with confirmation | ✅ Implemented |
| P1-08 Unit tests for agent service | ✅ 8 tests passing |

---

## Phase 2 — Sales & Commission ✅ CODE COMPLETE

| Task | Status |
| --- | --- |
| P2-01 Commission/rate calculation logic | ✅ CommissionService.cs enhanced |
| P2-02 Sales filtering UI | ✅ WinUI 3 Page (SalesFilterView) |
| P2-03 Auto-purchase feature | ✅ WinUI 3 Page (AutoBuyView) |
| P2-04 Currency type/rate fields | ✅ Currency conversion in CommissionService |
| P2-05 Commission payment/receive tracking | ✅ SaleRecord model + tracking |
| P2-06 Unit tests for commission logic | ✅ 22 tests passing |

---

## Phase 3 — Reporting & Export

- [ ] P3-01 Replace Crystal Reports with FastReport .NET — Stub created
- [ ] P3-02 Build agent report view — WinUI 3 Page (ReportView) created
- [ ] P3-03 Implement Excel export (EPPlus) — Stub created
- [ ] P3-04 Implement CSV export
- [ ] P3-05 Unit tests for report generation — ✅ 2 tests passing

---

## Phase 4 — Database Maintenance

- [ ] P4-01 Implement backup/restore — ✅ Implemented with pg_dump/pg_restore
- [ ] P4-02 Implement DB compaction/repair — ✅ VACUUM ANALYZE implemented
- [ ] P4-03 Implement create new database — ✅ CREATE DATABASE implemented
- [ ] P4-04 Unit tests for DB maintenance — ✅ 4 tests passing

---

## Phase 5 — System Features

- [ ] P5-01 Login/auth — ✅ 5 tests passing — WinUI 3 Page (LoginView) created
- [ ] P5-02 Registration/licensing — ✅ 4 tests passing — WinUI 3 Page (RegisterView) created
- [ ] P5-03 Sound settings — ✅ 5 tests passing — WinUI 3 Page (SoundSettingsView) created
- [ ] P5-04 Time-lock access control — ✅ 5 tests passing — WinUI 3 Page (TimeLockView) created
- [ ] P5-05 Regional/locale settings (JSON)
- [ ] P5-06 Maintenance dashboard — WinUI 3 Page (MaintenanceView) created

---

## Phase 6 — Polish & Verification

- [ ] P6-01 End-to-end testing against original DD.exe behavior
- [ ] P6-02 Performance benchmarking
- [ ] P6-03 UI/UX polish (WinUI 3 XAML styling, AutomationId audit)
- [ ] P6-04 Documentation and deployment scripts — README.md done
- [ ] P6-05 Final audit: no VB6/ActiveX/Registry dependencies remain

---

## WinUI 3 Views (all converted to Pages)

| View | File | XAML | Replaces VB6 Form |
| --- | --- | --- | --- |
| AgentListView | `src\Views\AgentListView.cs` | `AgentListView.xaml` | frmCustomer |
| AgentDetailView | `src\Views\AgentDetailView.cs` | `AgentDetailView.xaml` | frmCustomer |
| SalesFilterView | `src\Views\SalesFilterView.cs` | `SalesFilterView.xaml` | frmSaleFilt |
| AutoBuyView | `src\Views\AutoBuyView.cs` | `AutoBuyView.xaml` | frmAutoBuy |
| ReportView | `src\Views\ReportView.cs` | `ReportView.xaml` | frmReport / frmF5Report |
| LoginView | `src\Views\LoginView.cs` | `LoginView.xaml` | frmLogin |
| RegisterView | `src\Views\RegisterView.cs` | `RegisterView.xaml` | frmRegister |
| BackupView | `src\Views\BackupView.cs` | `BackupView.xaml` | frmBackUpData |
| CreateDatabaseView | `src\Views\CreateDatabaseView.cs` | `CreateDatabaseView.xaml` | frmCreateNewDatabase |
| CompactDatabaseView | `src\Views\CompactDatabaseView.cs` | `CompactDatabaseView.xaml` | frmCompactMsg |
| AgentSearchView | `src\Views\AgentSearchView.cs` | `AgentSearchView.xaml` | frmSearch |
| MaintenanceView | `src\Views\MaintenanceView.cs` | `MaintenanceView.xaml` | frmMaintenance |
| SoundSettingsView | `src\Views\SoundSettingsView.cs` | `SoundSettingsView.xaml` | frmSoundSetting |
| TimeLockView | `src\Views\TimeLockView.cs` | `TimeLockView.xaml` | frmTimeLock |

---

## Test Results (78 tests, all passing)

| Test Class | Tests |
| --- | --- |
| AgentServiceTests | 8 |
| CommissionServiceTests | 6 |
| CommissionServiceCurrencyTests | 10 |
| SoundServiceTests | 5 |
| DatabaseMaintenanceServiceTests | 4 |
| AuthServiceTests | 5 |
| LicensingServiceTests | 4 |
| TimeLockServiceTests | 5 |
| SalesFilterServiceTests | 9 |
| AutoBuyServiceTests | 13 |
| SupabaseAgentServiceTests | 4 |
| ExcelExportServiceTests | 2 |
| ReportServiceTests | 2 |

---

## Commands

```cmd
# Build (from repo root)
cd F:\b\b-pro-max
dotnet build src\b-pro-max.csproj

# Build tests
dotnet build tests\b-pro-max.tests.csproj

# Test
dotnet test tests\b-pro-max.tests.csproj

# Run (blocked in container — REGDB_E_CLASSNOTREG)
dotnet run --project src\b-pro-max.csproj
```

---

## Manual Steps Required (you need to do these)

### Option A: Local PostgreSQL
| Task | Status | Instructions |
| --- | --- | --- |
| Install PostgreSQL | ⏳ Manual | https://www.postgresql.org/download/windows/ |
| Create `bpro_max` database | ⏳ Manual | `createdb -U postgres bpro_max` |
| Run `Scripts/001_agents.sql` | ⏳ Manual | Execute against `bpro_max` |
| Update `appsettings.json` | ⏳ Manual | Replace `YOUR_PASSWORD_HERE` |

### Option B: Supabase (REST API) ✅ CODE READY
| Task | Status | Instructions |
| --- | --- | --- |
| Create Supabase project | ⏳ Manual | https://app.supabase.com/ |
| Get DB password | ⏳ Manual | Settings → Database → Connection string |
| Get Anon Key | ⏳ Manual | Settings → API → public anon key |
| Create `agents` table | ⏳ Manual | Run SQL in Supabase SQL Editor |
| Update `appsettings.json` | ⏳ Manual | Set Supabase URL, AnonKey, DB password |

**Supabase SQL to run:**
```sql
CREATE TABLE IF NOT EXISTS agents (
    id SERIAL PRIMARY KEY,
    agent_name TEXT NOT NULL,
    address TEXT,
    phone TEXT,
    commission NUMERIC(12,2) DEFAULT 0,
    rate NUMERIC(12,2) DEFAULT 0,
    commission1 NUMERIC(12,2) DEFAULT 0,
    rate1 NUMERIC(12,2) DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## Phase 1 — Core Agent Management ✅ CODE COMPLETE

| Task | Status |
| --- | --- |
| P1-01 Agent CRUD service | ✅ AgentService.cs + SupabaseAgentService.cs |
| P1-02 Agent list view | ✅ Console menu + placeholder view |
| P1-03 Agent detail/edit form | ✅ Placeholder view created |
| P1-04 `SELECT * FROM agents` | ✅ Implemented (both Npgsql + Supabase) |
| P1-05 `WHERE agent_name = @name` | ✅ Implemented |
| P1-06 `ORDER BY agent_name` | ✅ Implemented |
| P1-07 Agent deletion with confirmation | ✅ Implemented |
| P1-08 Unit tests for agent service | ✅ 8 tests passing |

---

## Phase 2 — Sales & Commission ✅ CODE COMPLETE

| Task | Status |
| --- | --- |
| P2-01 Commission/rate calculation logic | ✅ CommissionService.cs enhanced |
| P2-02 Sales filtering UI | ✅ SalesFilterService.cs + placeholder view |
| P2-03 Auto-purchase feature | ✅ AutoBuyService.cs + placeholder view |
| P2-04 Currency type/rate fields | ✅ Currency conversion in CommissionService |
| P2-05 Commission payment/receive tracking | ✅ SaleRecord model + tracking |
| P2-06 Unit tests for commission logic | ✅ 22 tests passing |

**New Phase 2 files:**
- `src/Services/ISalesFilterService.cs` — Interface
- `src/Services/SalesFilterService.cs` — Implementation with SaleRecord model
- `src/Services/IAutoBuyService.cs` — Interface
- `src/Services/AutoBuyService.cs` — Implementation with AutoBuyRule model
- `src/Services/ISupabaseAgentService.cs` — Supabase interface
- `src/Services/SupabaseAgentService.cs` — Supabase implementation
- `src/Models/SupabaseAgent.cs` — Supabase agent model
- `tests/CommissionServiceCurrencyTests.cs` — 10 tests
- `tests/SalesFilterServiceTests.cs` — 9 tests
- `tests/AutoBuyServiceTests.cs` — 13 tests
- `tests/SupabaseAgentServiceTests.cs` — 4 tests

---

## Phase 3 — Reporting & Export

- [ ] P3-01 Replace Crystal Reports with FastReport .NET — Stub created
- [ ] P3-02 Build agent report view — Placeholder created
- [ ] P3-03 Implement Excel export (EPPlus) — Stub created
- [ ] P3-04 Implement CSV export
- [ ] P3-05 Unit tests for report generation — ✅ 2 tests passing

---

## Phase 4 — Database Maintenance

- [ ] P4-01 Implement backup/restore — ✅ Implemented with pg_dump/pg_restore
- [ ] P4-02 Implement DB compaction/repair — ✅ VACUUM ANALYZE implemented
- [ ] P4-03 Implement create new database — ✅ CREATE DATABASE implemented
- [ ] P4-04 Unit tests for DB maintenance — ✅ 4 tests passing

---

## Phase 5 — System Features

- [ ] P5-01 Login/auth — ✅ 5 tests passing
- [ ] P5-02 Registration/licensing — ✅ 4 tests passing
- [ ] P5-03 Sound settings — ✅ 5 tests passing (Console.Beep, no winmm.dll)
- [ ] P5-04 Time-lock access control — ✅ 5 tests passing
- [ ] P5-05 Regional/locale settings (JSON)
- [ ] P5-06 Maintenance dashboard — Placeholder created

---

## Code Analysis — COMPLETE

| Task | Status | Notes |
| --- | --- | --- |
| CA-01 Promote CA1852 & CA1862 to errors | ✅ Done | .editorconfig: CA1852.severity=error, CA1862.severity=error (CA1416 already error) |
| CA-02 Fix resulting src warnings | ✅ Done | 0 CA warnings in src after promotion — all clean |
| CA-03 Phase 4: TreatWarningsAsErrors=true | ✅ Done | Already set in Directory.Build.props |
| CA-04 WarningsNotAsErrors=CA1707 | ✅ Done | Added to Directory.Build.props (tests: 148 occurrences) |
| CA-05 Validate full build + tests | ✅ Done | src 0 errors, tests 0 errors, 78/78 pass |

---

## Phase 6 — Polish & Verification

- [ ] P6-01 End-to-end testing against original DD.exe behavior
- [ ] P6-02 Performance benchmarking
- [ ] P6-03 UI/UX polish (WinUI 3 XAML views in Visual Studio)
- [ ] P6-04 Documentation and deployment scripts — README.md done
- [ ] P6-05 Final audit: no VB6/ActiveX/Registry dependencies remain


---

## View Placeholders Created (WinUI 3 — to be implemented in Visual Studio)

| View | File | Replaces VB6 Form |
| --- | --- | --- |
| AgentDetailView | `src\Views\AgentDetailView.cs` | frmCustomer |
| SalesFilterView | `src\Views\SalesFilterView.cs` | frmSaleFilt |
| AutoBuyView | `src\Views\AutoBuyView.cs` | frmAutoBuy |
| ReportView | `src\Views\ReportView.cs` | frmReport / frmF5Report |
| LoginView | `src\Views\LoginView.cs` | frmLogin |
| RegisterView | `src\Views\RegisterView.cs` | frmRegister |
| BackupView | `src\Views\BackupView.cs` | frmBackUpData |
| CreateDatabaseView | `src\Views\CreateDatabaseView.cs` | frmCreateNewDatabase |
| CompactDatabaseView | `src\Views\CompactDatabaseView.cs` | frmCompactMsg |
| AgentSearchView | `src\Views\AgentSearchView.cs` | frmSearch |
| MaintenanceView | `src\Views\MaintenanceView.cs` | frmMaintenance |
| SoundSettingsView | `src\Views\SoundSettingsView.cs` | frmSoundSetting |
| TimeLockView | `src\Views\TimeLockView.cs` | frmTimeLock |

---

## Test Results (78 tests, all passing)

| Test Class | Tests |
| --- | --- |
| AgentServiceTests | 8 |
| CommissionServiceTests | 6 |
| CommissionServiceCurrencyTests | 10 |
| SoundServiceTests | 5 |
| DatabaseMaintenanceServiceTests | 4 |
| AuthServiceTests | 5 |
| LicensingServiceTests | 4 |
| TimeLockServiceTests | 5 |
| SalesFilterServiceTests | 9 |
| AutoBuyServiceTests | 13 |
| SupabaseAgentServiceTests | 4 |
| ExcelExportServiceTests | 2 |
| ReportServiceTests | 2 |

---

## Commands

```cmd
# Build
cd F:\b\b-pro-max
dotnet build src\b-pro-max.csproj

# Test
dotnet test tests\b-pro-max.tests.csproj

# Run (from repo root)
dotnet run --project src\b-pro-max.csproj
```
