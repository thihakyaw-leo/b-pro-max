# b-pro-max

**Modern upgrade of DD.exe — Visual Basic 6 lottery/agent management system**

## Overview

b-pro-max is a complete reimplementation of DD.exe (a VB6 lottery/agent management application) using a modern .NET 8 + C# stack. The original binary was reverse-engineered using REA/Ghidra to extract all business logic, database schema, and UI structure. No original binary code, VB6 runtime dependencies, ActiveX controls, or Registry storage is carried over.

## Source Analysis (via REA/Ghidra)

| Property | Value |
|----------|-------|
| Original binary | `F:\b\DD.exe` |
| Format | PE x86 |
| Size | 2,371,584 bytes |
| Procedures | 1,214 |
| Strings | 3,370 |
| Image base | 0x400000 |
| Primary dependency | MSVBVM60.DLL (VB6 runtime) |
| ActiveX controls | MSFlexGrid, Crystal Reports OCX, DTPicker, CommonDialog |
| Windows APIs | kernel32.dll (SetVolumeLabelA), winmm.dll (sndPlaySoundA) |

### Core Database Table — AgentThai

| Field | Type |
|-------|------|
| AgentName | Text |
| Address | Text |
| Phone | Text |
| Commision | Numeric |
| Rate | Numeric |
| Commision1 | Numeric |
| Rate1 | Numeric |
| CurType | Text |
| CurRate | Numeric |

### Key SQL Operations (from string inventory)

- `Select * From AgentThai`
- `Select * From AgentThai Where AgentName='...'`
- `Select * From AgentThai Order By AgentName`
- `Delete From AgentThai Where AgentName='...'`

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 8 |
| Language | C# |
| UI | Console app now → WinUI 3 XAML (Visual Studio) |
| Database | PostgreSQL (Npgsql) |
| Reporting | FastReport .NET |
| Excel | EPPlus |
| Configuration | JSON (appsettings.json) |
| Sound | .NET Console.Beep (no winmm.dll) |
| Testing | xUnit |

## Project Structure

```
F:\b\b-pro-max\
├── b-pro-max.sln
├── appsettings.json          # Configuration (update with real PostgreSQL password)
├── .editorconfig
├── .gitignore
├── global.json               # Pins .NET SDK 8.0.100
├── README.md                 # This file
├── Scripts/
│   └── 001_agents.sql        # PostgreSQL agents table schema
├── src\
│   ├── b-pro-max.csproj
│   ├── App.cs                # DI container + configuration
│   ├── Program.cs            # Console entry point
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
│   │   ├── IDatabaseMaintenanceService.cs
│   │   ├── DatabaseMaintenanceService.cs
│   │   ├── ISoundService.cs
│   │   ├── SoundService.cs
│   │   ├── IAuthService.cs
│   │   ├── AuthService.cs
│   │   ├── ILicensingService.cs
│   │   ├── LicensingService.cs
│   │   ├── ITimeLockService.cs
│   │   └── TimeLockService.cs
│   └── Views/                # WinUI 3 placeholders (13 files)
└── tests\
    ├── b-pro-max.tests.csproj
    ├── AgentServiceTests.cs      (8 tests)
    ├── CommissionServiceTests.cs (6 tests)
    ├── SoundServiceTests.cs      (5 tests)
    ├── DatabaseMaintenanceServiceTests.cs (4 tests)
    ├── AuthServiceTests.cs       (5 tests)
    ├── LicensingServiceTests.cs  (4 tests)
    ├── TimeLockServiceTests.cs   (5 tests)
    ├── ExcelExportServiceTests.cs (2 tests)
    └── ReportServiceTests.cs     (2 tests)
```

**Total: 42 unit tests, all passing**

## Quick Start

### Prerequisites

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
2. **PostgreSQL** — https://www.postgresql.org/download/windows/
3. **Visual Studio 2022** — For WinUI 3 XAML development (optional for console mode)

### Setup

1. **Install PostgreSQL** and create the `bpro_max` database:
   ```cmd
   createdb bpro_max
   ```

2. **Run the schema script**:
   ```cmd
   psql -d bpro_max -f F:\b\b-pro-max\Scripts\001_agents.sql
   ```

3. **Update `appsettings.json`** — Replace `YOUR_PASSWORD_HERE` with your PostgreSQL password:
   ```json
   {
     "Database": {
       "ConnectionString": "Host=localhost;Port=5432;Database=bpro_max;Username=bpro_max_user;Password=your_password;"
     }
   }
   ```

4. **Build**:
   ```cmd
   cd F:\b\b-pro-max
   dotnet build src\b-pro-max.csproj
   ```

5. **Run tests**:
   ```cmd
   dotnet test tests\b-pro-max.tests.csproj
   ```

6. **Run the application**:
   ```cmd
   dotnet run --project src\b-pro-max.csproj
   ```

## Console Menu

The console application provides a temporary interface for testing:

| Option | Description |
|--------|-------------|
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
|------|-------------------|
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
- No hardcoded `D:\...` paths
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

# Run from repo root
dotnet run --project src\b-pro-max.csproj
```

## License

Internal use only.
# b-pro-max
