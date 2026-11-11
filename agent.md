# agent.md — Agent Instructions for b-pro-max

**Project:** b-pro-max (modern upgrade of DD.exe, a VB6 lottery/agent management system)  
**Location:** `F:\b\b-pro-max\`  
**Source reference:** `F:\b\DD.exe` (PE x86, analyzed via Ghidra 12.1.4 through REA)  
**Date:** 2026-11-10

---

## 1. Project Purpose

Build a modern, enhanced version of **DD.exe** — a Visual Basic 6 lottery/agent management application — with identical internal functionality but no original binary code, VB6 runtime dependencies, ActiveX controls, or Registry storage carried over.

**Source app identity:** DD.exe is a ~2.37 MB PE x86 executable with 1,214 procedures and 3,370 strings. It manages lottery agents, sales, commissions, and reports using an `AgentThai` database table.

---

## 2. What an Agent Must Know (would be missed without this file)

### 2.1 Source app architecture (from REA analysis)

| Aspect | DD.exe (original) | b-pro-max (target) |
|--------|-------------------|---------------------|
| Language/runtime | Visual Basic 6 + MSVBVM60.DLL | C# + WinUI 3 (.NET) |
| UI | VB6 forms + ActiveX (MSFlexGrid, Crystal OCX, DTPicker, CommonDialog) | WinUI XAML + Modern Controls |
| Database | ADO + AgentThai table (Jet/Access or SQL Server) | PostgreSQL |
| Reporting | Crystal Reports | PdfSharpCore (PDF) + QuestPDF |
| Sound | winmm.dll sndPlaySoundA | .NET SoundPlayer / MediaPlayer |
| Excel | OLE automation (modExcel) | EPPlus |
| Config | Registry (`*\AD:\SOFT\Xerus_ADO\bsql\bsql.vbp`) + INI | JSON + appsettings |
| Date Picker | DTPicker (ActiveX) | Native WinUI DatePicker |
| Data Grid | MSFlexGrid (ActiveX) | WinUI `ListView` |
| Paths | Hardcoded `D:\WINDOWS\system32\...`, `D:\Program Files\...` | Relative paths + app config |

### 2.2 Core database schema (AgentThai → must be preserved)

```sql
CREATE TABLE agents (
  id SERIAL PRIMARY KEY,
  agent_name TEXT NOT NULL,
  address TEXT,
  phone TEXT,
  commission NUMERIC(12,2),
  rate NUMERIC(12,2),
  commission1 NUMERIC(12,2),
  rate1 NUMERIC(12,2),
  currency_type TEXT,
  currency_rate NUMERIC(12,4),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### 2.3 Key SQL operations to replicate (from string inventory)

- `SELECT * FROM agents`
- `SELECT * FROM agents WHERE agent_name = @name`
- `SELECT * FROM agents ORDER BY agent_name`
- `DELETE FROM agents WHERE agent_name = @name`

### 2.4 Module map (VB6 → modern C#)

| VB6 Module | Responsibility | Modern C# Replacement |
|------------|---------------|----------------------|
| `modMain` | App entry point, global init | `App.xaml.cs`, `Program.cs` |
| `modGlobal` | Global variables, constants | `Models/`, `Configuration/` |
| `modImExPort` | Import/Export data | `Services/ExcelExportService.cs`, `Services/CsvExportService.cs` |
| `modReg` | Registration/licensing | `Services/LicensingService.cs` |
| `modINI` | INI file config | `appsettings.json` + `.env` |
| `modExcel` | Excel OLE automation | `Services/ExcelExportService.cs` (EPPlus) |
| `modCompactDB` | DB compaction/repair | `Services/DatabaseMaintenanceService.cs` |
| `modRegional` | Regional/locale settings | `Services/RegionalSettingsService.cs` |

### 2.5 Form map (VB6 → WinUI XAML views)

| VB6 Form | Responsibility | Modern View (XAML) |
|----------|---------------|-------------------|
| `frmLogin` | User authentication | `Views/LoginView.xaml` |
| `frmRegister` | Software registration | `Views/RegisterView.xaml` |
| `frmMaintenance` | Data maintenance | `Views/MaintenanceView.xaml` |
| `frmSaleFilt` | Sales filtering | `Views/SalesFilterView.xaml` |
| `frmAutoBuy` | Auto-purchase | `Views/AutoBuyView.xaml` |
| `frmBackUpData` | Backup | `Views/BackupView.xaml` |
| `frmCreateNewDatabase` | DB creation | `Views/CreateDatabaseView.xaml` |
| `frmCompactMsg` | DB compaction UI | `Views/CompactDatabaseView.xaml` |
| `frmSearch` | Search agents | `Views/AgentSearchView.xaml` |
| `frmCustomer` | Customer management | `Views/AgentDetailView.xaml` |
| `frmReport` / `frmF5Report` | Reporting | `Views/ReportView.xaml` (PdfSharpCore) |
| `frmSoundSetting` | Sound config | `Views/SoundSettingsView.xaml` |
| `frmTimeLock` | Time-based access control | `Views/TimeLockView.xaml` |

---

## 3. Hard Constraints

- **Do not copy any original binary code, strings, or resources** from DD.exe.
- **Do not depend on MSVBVM60.DLL** or any VB6 runtime.
- **Do not use ActiveX controls** (MSFlexGrid, Crystal Reports OCX, DTPicker, CommonDialog).
- **Do not write to Windows Registry** for app config — use JSON only.
- **Do not hardcode drive paths** (`D:\...`).
- **Preserve all business logic** — agent CRUD, commission calculation, sales filtering, backup/compact, reporting, sound alerts, time-lock access.
- **Use WinUI 3 (Windows App SDK)** — not WPF, not WinForms, not Electron.
- **Use PostgreSQL** — not SQLite, not SQL Server, not Access.
- **Use PdfSharpCore (PDF generation)** — not Crystal Reports, not FastReport.
- **Use EPPlus** — not OLE automation, not closed XML.
- **Use native WinUI DatePicker** — not third-party date pickers.
- **Use the native WinUI `ListView`** — not third-party grids. (The WinUI Data Grid package is not referenced.)

---

## 4. REA Analysis Evidence (for reference)

- Binary: `F:\b\DD.exe` (SHA256: `d41b30ed0c6e7d5e6bfd14097778f1f2a2fa7843a08e5ebe1d15fd02f133036e`)
- Format: PE x86, image base `0x400000`
- Segments: Headers, .text (4.3 MB), .data (120 KB), .rsrc (28 KB), tdb (4 KB)
- Provider: Ghidra 12.1.4 (via REA)
- Procedures: 1,214 | Strings: 3,370
- External deps: MSVBVM60.DLL, kernel32.dll (SetVolumeLabelA), winmm.dll (sndPlaySoundA)

---

## 5. Tech Stack Summary

| Layer | Technology |
|-------|-----------|
| Framework | WinUI 3 / Windows App SDK 1.7 |
| Language | C# (.NET 9) |
| UI | XAML + Modern Controls (WinUI) |
| Database | PostgreSQL (Npgsql provider) |
| Reporting | PdfSharpCore (PDF) + QuestPDF |
| Excel | EPPlus |
| Configuration | JSON (appsettings.json) + `.env` (DotNetEnv) |
| Date Picker | Native WinUI DatePicker |
| Data Grid | WinUI `ListView` |
| Sound | System.Media.SoundPlayer / MediaElement |
| Testing | xUnit |

---

## 6. How to Work in This Repo

- The repo is already under git on branch `main` (`F:\b\b-pro-max\.git`); do not re-initialize it.
- All source goes under `F:\b\b-pro-max\src\`.
- Tests go under `F:\b\b-pro-max\tests\` (unit) and `F:\b\b-pro-max\tests\e2e\` (e2e).
- Solution file: `F:\b\b-pro-max\b-pro-max.sln`
- App project: `F:\b\b-pro-max\src\b-pro-max.csproj` (WinUI 3 / Windows App SDK)
- Config at repo root: `appsettings.json` (plus optional gitignored `.env`)
- Quality gate mirrors CI: `dotnet format --verify-no-changes` → `dotnet build b-pro-max.sln -c Release` → unit tests → e2e tests.
- Do not modify `AGENTS.md`, `agent.md`, `plan.md`, or `todos.md` without updating the others to stay in sync.

---

## 7. Required NuGet Packages

| Package | Purpose |
|---------|---------|
| Microsoft.WindowsAppSDK | WinUI 3 framework |
| Npgsql | PostgreSQL ADO.NET provider |
| PdfSharpCore | PDF report generation |
| QuestPDF | PDF composition (reference reserved for report work) |
| EPPlus | Excel file generation |
| CsvHelper | CSV import/export |
| Microsoft.Extensions.Configuration | JSON config binding |
| Microsoft.Extensions.Configuration.Json | JSON config provider |
| DotNetEnv | `.env` secret overrides |
| xUnit | Unit testing |
