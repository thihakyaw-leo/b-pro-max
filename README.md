# b-pro-max

**Modern upgrade of DD.exe — Visual Basic 6 lottery/agent management system**

## Overview

b-pro-max is a complete reimplementation of DD.exe (a VB6 lottery/agent management application) using a modern .NET 9 + C# stack. The original binary was reverse-engineered using REA/Ghidra to extract all business logic, database schema, and UI structure. No original binary code, VB6 runtime dependencies, ActiveX controls, or Registry storage is carried over.

## Source Analysis (via REA/Ghidra)

| Property | Value |
| --- | --- |
| Original binary | `DD.exe` |
| Format | PE x86 |
| Primary dependency | MSVBVM60.DLL (VB6 runtime) |
| ActiveX controls | MSFlexGrid, Crystal Reports OCX, DTPicker, CommonDialog |
| Windows APIs | kernel32.dll (SetVolumeLabelA), winmm.dll (sndPlaySoundA) |

### Core Database Table — AgentThai → `agents`

| Field | Type |
| --- | --- |
| AgentName | Text (NOT NULL) |
| Address | Text |
| Phone | Text |
| Commission | NUMERIC(12,2) |
| Rate | NUMERIC(12,2) |
| Commission1 | NUMERIC(12,2) |
| Rate1 | NUMERIC(12,2) |
| CurrencyType | Text |
| CurrencyRate | NUMERIC(12,4) |

Schema: `Scripts/001_agents.sql` (PostgreSQL).

## Tech Stack

| Layer | Technology |
| --- | --- |
| Framework | .NET 9 |
| Language | C# |
| UI | WinUI 3 XAML (Windows desktop) |
| Database | PostgreSQL (Npgsql) — local |
| Reporting | FastReport .NET |
| Excel | EPPlus |
| Configuration | JSON (appsettings.json) |
| Sound | .NET Console.Beep (no winmm.dll) |
| Testing | xUnit (VSTest) |
| Static analysis | SDK analyzers + Meziantou.Analyzer, `TreatWarningsAsErrors=true` |

## Project Structure

```
b-pro-max/
├── b-pro-max.sln
├── appsettings.json          # Configuration (local PostgreSQL connection string)
├── .editorconfig
├── .gitignore
├── global.json               # Pins .NET SDK 9.0.321
├── README.md                 # This file
├── AGENTS.md                 # Repo-native quality baseline + build commands
├── Scripts/
│   └── 001_agents.sql        # PostgreSQL agents table schema
├── src\
│   ├── b-pro-max.csproj
│   ├── App.cs                # (superseded) former DI/config, now App.xaml.cs
│   ├── App.xaml.cs           # DI container + configuration + app startup
│   ├── Program.cs            # WinUI 3 entry point ([STAThread] Main)
│   ├── InteractiveMenu.cs    # Console UI for testing
│   ├── Models/
│   │   └── Agent.cs
│   ├── Configuration/
│   │   └── AppSettings.cs
│   ├── Services/
│   │   ├── IDatabaseService.cs
│   │   ├── DatabaseService.cs
│   │   ├── IAgentService.cs
│   │   ├── AgentService.cs
│   │   ├── ICommissionService.cs
│   │   ├── CommissionService.cs
│   │   ├── IReportService.cs
│   │   ├── ReportService.cs
│   │   ├── IExcelExportService.cs
│   │   ├── ExcelExportService.cs
│   │   ├── ICsvExportService.cs
│   │   ├── CsvExportService.cs
│   │   ├── IDatabaseMaintenanceService.cs
│   │   ├── DatabaseMaintenanceService.cs
│   │   ├── IDatabaseMigrationService.cs
│   │   ├── DatabaseMigrationService.cs
│   │   ├── ISoundService.cs
│   │   ├── SoundService.cs
│   │   ├── IAuthService.cs
│   │   ├── AuthService.cs
│   │   ├── ILicensingService.cs
│   │   ├── LicensingService.cs
│   │   ├── ITimeLockService.cs
│   │   ├── ITimeLockService.cs
│   │   ├── ISalesFilterService.cs
│   │   ├── SalesFilterService.cs
│   │   ├── IAutoBuyService.cs
│   │   └── AutoBuyService.cs
│   └── Views/                # WinUI 3 placeholders (13 files)
└── tests\
    ├── b-pro-max.tests.csproj
    ├── AgentServiceTests.cs      (8 tests)
    ├── CommissionServiceTests.cs (6 tests)
    ├── CommissionServiceCurrencyTests.cs (10 tests)
    ├── SoundServiceTests.cs      (5 tests)
    ├── DatabaseMaintenanceServiceTests.cs (4 tests)
    ├── AuthServiceTests.cs       (7 tests)
    ├── LicensingServiceTests.cs  (5 tests)
    ├── TimeLockServiceTests.cs   (7 tests)
    ├── SalesFilterServiceTests.cs (9 tests)
    ├── AutoBuyServiceTests.cs    (13 tests)
    ├── ExcelExportServiceTests.cs (2 tests)
    └── ReportServiceTests.cs     (2 tests)
```

**Database path:** local PostgreSQL only. Supabase REST integration was removed; the app now connects to a local `bpro_max` database via `IDatabaseService` → `DatabaseService` (Npgsql). Supabase settings were removed from `appsettings.json` and `.env`.

## Quick Start

### Prerequisites

1. **.NET 9 SDK** — https://dotnet.microsoft.com/download/dotnet/9.0
2. **PostgreSQL** — https://www.postgresql.org/download/windows/
3. **Visual Studio 2022** — For WinUI 3 XAML development (the app is Windows-only; WinRT activation is unavailable in containers)

### Myanmar Unicode Support (Pyidaungsu Font)

To render Myanmar (Burmese) text correctly in the WinUI 3 UI, install the **Pyidaungsu font pack** on the Windows machine:

1. Download Pyidaungsu font from the Myanmar Unicode font repository (e.g., https://github.com/myanmar-fonts/pyidaungsu-font)
2. Install the `.ttf` font file on Windows (right-click → Install, or copy to `C:\Windows\Fonts\`)
3. The app will automatically use `Pyidaungsu` as the font family when the Myanmar preset is applied via `ApplyMyanmarPreset()` or `ApplyMyanmarUnicodeFont()`
4. Verify font installation: `C:\Windows\Fonts\Pyidaungsu.ttf` should exist

Without the Pyidaungsu font installed, Myanmar Unicode text may display as boxes or incorrect glyphs.

### Setup

1. **Install PostgreSQL** and create the `bpro_max` database:
   ```cmd
   createdb -U postgres bpro_max
   ```

2. **Run the schema script** against `bpro_max`:
   ```cmd
   psql -U postgres -d bpro_max -f Scripts\001_agents.sql
   ```

3. **Configure `appsettings.json`** — set the local PostgreSQL connection string:
   ```json
   {
     "Database": {
       "ConnectionString": "Host=localhost;Port=5432;Database=bpro_max;Username=postgres;Password=your_password;SSL Mode=Disable;Trust Server Certificate=true"
     }
   }
   ```
   Alternatively override via environment variable `DATABASE_URL` or `DATABASE_CONNECTION_STRING` (takes precedence). See `.env` for commented examples.

4. **Build**:
   ```cmd
   dotnet build b-pro-max.sln -c Release
   ```

5. **Run unit tests**:
   ```cmd
   dotnet test tests\b-pro-max.tests.csproj --no-build
   ```

6. **Run the application**:
   ```cmd
   dotnet run --project src\b-pro-max.csproj
   ```

### Quality pass (repo root, Windows only)

See `AGENTS.md` for the full ordered command list:

```cmd
dotnet format --verify-no-changes
dotnet build b-pro-max.sln -c Release
dotnet build src\b-pro-max.csproj
dotnet build tests\b-pro-max.tests.csproj
dotnet test tests\b-pro-max.tests.csproj --no-build
dotnet test tests\b-pro-max.tests.csproj --collect:"XPlat Code Coverage" --results-directory:.\coverage
dotnet test tests\e2e\b-pro-max.e2e.csproj --no-build
```

## Console Menu

The console application provides a temporary interface for testing:

| Option | Description |
| --- | --- |
| 1 | List all agents |
| 2 | Search agents by name |
| 3 | Add new agent |
| 4 | Calculate commission |
| 5 | Test sound alert |
| 6 | Show configuration |
| 0 | Exit |

## WinUI 3 Migration

The console app is a temporary test harness. The final UI will be WinUI 3 XAML. Placeholder classes exist in `src\Views\`:

| View | Replaces VB6 Form |
| --- | --- |
| AgentDetailView | frmCustomer |
| SalesFilterView | frmSaleFilt |
| AutoBuyView | frmAutoBuy |
| ReportView | frmReport / frmF5Report |
| LoginView | frmLogin |
| RegisterView | frmRegister |
| BackupView | frmBackUpData |
| CreateDatabaseView | frmCreateNewDatabase |
| CompactDatabaseView | frmCompactMsg |
| AgentSearchView | frmSearch |
| MaintenanceView | frmMaintenance |
| SoundSettingsView | frmSoundSetting |
| TimeLockView | frmTimeLock |

Replace each `.cs` placeholder with `.xaml` + `.xaml.cs` in Visual Studio.

## Hard Constraints

- No VB6 runtime (MSVBVM60.DLL)
- No ActiveX controls (MSFlexGrid, Crystal Reports OCX, DTPicker, CommonDialog)
- No Windows Registry for configuration
- No hardcoded `D:\...` / `F:\...` paths
- No original binary code copied from DD.exe
- WinUI 3 only (no WPF, no WinForms)
- PostgreSQL only (no SQLite, no SQL Server)
- FastReport .NET only (no Crystal Reports)
- EPPlus only (no OLE automation)
- Native WinUI DatePicker only

## Commands

```cmd
# Build
dotnet build src\b-pro-max.csproj

# Test
dotnet test tests\b-pro-max.tests.csproj

# Run
dotnet run --project src\b-pro-max.csproj
```

## License

Internal use only.
