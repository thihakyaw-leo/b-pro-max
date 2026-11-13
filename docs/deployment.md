# Deployment & Packaging Guide — MSIX

This guide covers building, packaging (MSIX), and deploying b-pro-max on Windows desktop.

## Prerequisites

- **Windows 10/11** (WinUI 3 requires Windows desktop; WinRT activation is unavailable in containers/Linux)
- **.NET 9 SDK** — https://dotnet.microsoft.com/download/dotnet/9.0 (pinned to 9.0.100 in `global.json`)
- **PostgreSQL** — local instance for the `bpro_max` database (https://www.postgresql.org/download/windows/). Add PostgreSQL `bin` to PATH for backup/restore via `pg_dump`/`pg_restore`.
- **Pyidaungsu font** — required for correct Myanmar Unicode rendering (install `Pyidaungsu.ttf` on the target machine)
- **Windows App SDK** — included via NuGet packages in the project; no separate SDK install required for packaging

## Build

Run from the repository root:

```cmd
dotnet build b-pro-max.sln -c Release
```

Expected output: 0 warnings, 0 errors. The app assembly is produced at:
`src\bin\x64\Release\net9.0-windows10.0.19041.0\b-pro-max.exe`

## Folder publish (scripts/publish.ps1)

As an alternative to MSIX packaging, the app can be published as a plain
folder for x64 Windows desktop using `scripts\publish.ps1`. The script runs
`dotnet publish -c Release -r win-x64` against `src\b-pro-max.csproj`
(net9.0-windows10.0.19041.0, Platform=x64, UseWinUI=true,
TreatWarningsAsErrors=true) and outputs to `./publish` by default.

### Usage examples

Framework-dependent publish (the target machine needs the .NET 9 runtime):

```powershell
.\scripts\publish.ps1
```

Self-contained publish (includes the .NET runtime; no runtime install needed on the target):

```powershell
.\scripts\publish.ps1 -SelfContained
```

Self-contained publish to a custom folder, then ZIP the output:

```powershell
.\scripts\publish.ps1 -SelfContained -Output C:\deploy\b-pro-max -Zip
```

### Parameters

| Parameter | Purpose |
| --- | --- |
| `-SelfContained` | Include the .NET runtime in the output (self-contained). Omit for framework-dependent deployment. |
| `-Output` | Destination folder. Default: `./publish`. |
| `-Runtime` | Runtime identifier. Default: `win-x64`. |
| `-Configuration` | Build configuration. Default: `Release`. |
| `-NoBuild` | Skip rebuilding; reuse previously built assets (`--no-build`). |
| `-Zip` | ZIP the published output after publishing. |

The published folder contains `b-pro-max.exe` and its dependencies and can be
copied directly to a target Windows machine. For a framework-dependent build,
the target machine needs the .NET 9 desktop runtime and the Windows App SDK
runtime (`winget install Microsoft.WindowsAppSDK`). The target also needs a
local PostgreSQL instance with the `bpro_max` database and schema
(see Database setup on target machine) and the Pyidaungsu font for Myanmar
Unicode rendering.

## MSIX Packaging

b-pro-max is packaged as an MSIX app package for Windows desktop distribution.

### Step 1 — Add the Windows App SDK packaging project (if not present)

If the solution does not already contain a Windows Application Packaging Project (WAPP), add one:

1. In Visual Studio 2022, add a new **Windows Application Packaging Project** to the solution.
2. Set b-pro-max as the application reference in the packaging project.
3. Set target platform to `x64`, configuration to `Release`.

### Step 2 — Configure package identity

In `Package.appxmanifest` of the packaging project:

| Field | Value |
| --- | --- |
| `Identity Name` | `b-pro-max` |
| `Identity Publisher` | `CN=your-publisher` (use your code-signing certificate) |
| `DisplayName` | `b-pro-max` |
| `PublisherDisplayName` | `Your Organization` |
| `Description` | `Modern upgrade of DD.exe — VB6 lottery/agent management system` |

### Step 3 — Declare capabilities

The app requires:

- `internetClient` — PostgreSQL connections over TCP (localhost)
- `runFullTrust` — WinUI 3 desktop app needs full trust for file system and registry-free COM activation

Example `Package.appxmanifest` capability section:

```xml
<Capabilities>
  <Capability Name="internetClient"/>
  <rescap:Capability Name="runFullTrust"/>
</Capabilities>
```

Add the `rescap` namespace:
```xml
xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
```

### Step 4 — Build the MSIX

```cmd
msbuild b-pro-max.sln -p:Configuration=Release -p:Platform=x64 -t:Publish
```

Or build the packaging project directly in Visual Studio (Build → Publish → Create App Packages).

Output: `b-pro-max_1.0.0.0_x64.msix` (or `.msixbundle` for multi-arch).

### Step 5 — Code sign (production)

Sign the MSIX with a trusted code-signing certificate:

```cmd
signtool sign /fd SHA256 /a /f path\to\certificate.pfx /p <password> b-pro-max_1.0.0.0_x64.msix
```

Unsigned MSIX packages can only be sideloaded in developer mode.

## Deployment

### Sideload (development / internal)

1. Enable **Developer Mode** on the target Windows machine (Settings → Privacy & security → For developers → Developer mode).
2. Install the MSIX: double-click the `.msix` file, or use PowerShell:
   ```powershell
   Add-AppxPackage b-pro-max_1.0.0.0_x64.msix
   ```
3. Install the Pyidaungsu font on the target machine (copy `Pyidaungsu.ttf` to `C:\Windows\Fonts\`).
4. Ensure PostgreSQL is running locally with the `bpro_max` database and schema from `Scripts/001_agents.sql` … `Scripts/006_access_requests.sql`.

### Enterprise distribution

Distribute the signed MSIX via:
- Microsoft Intune / Microsoft Endpoint Manager
- Group Policy app deployment
- Internal file share with `Add-AppxPackage` script

### Uninstall

```powershell
Remove-AppxPackage b-pro-max_1.0.0.0_x64
```

Or via Settings → Apps → Installed apps → b-pro-max → Uninstall.

## Database setup on target machine

The app connects to a local PostgreSQL instance. On first run:

1. Install PostgreSQL (or ensure it is already installed).
2. Create the `bpro_max` database:
   ```cmd
   createdb -U postgres bpro_max
   ```
3. Run schema scripts:
   ```cmd
   psql -U postgres -d bpro_max -f Scripts\001_agents.sql
   psql -U postgres -d bpro_max -f Scripts\002_users.sql
   psql -U postgres -d bpro_max -f Scripts\003_sales.sql
   psql -U postgres -d bpro_max -f Scripts\004_autobuy.sql
   psql -U postgres -d bpro_max -f Scripts\005_settings.sql
   psql -U postgres -d bpro_max -f Scripts\006_access_requests.sql
   ```
4. Configure the connection string in `appsettings.json` or via environment variable `DATABASE_URL` / `DATABASE_CONNECTION_STRING`.

## WinRT runtime requirement

WinUI 3 depends on the Windows App SDK runtime. On machines without the MSIX framework package pre-installed, the MSIX installer will download the required framework package automatically from the Microsoft Store. Ensure the target machine has internet access during first installation, or pre-install the Windows App SDK runtime via the Winget package:

```cmd
winget install Microsoft.WindowsAppSDK
```

## Known limitations

- **Windows desktop only** — WinRT activation (`REGDB_E_CLASSNOTREG`) fails outside Windows.
- **PostgreSQL only** — no SQLite/SQL Server support.
- **XAML compilation enabled** — all views use `InitializeComponent()`; controls resolved via `FindChildByName<T>()` with `AutomationProperties.AutomationId`.
- **Sound volume** — `SoundService` persists volume via `ISettingsStore` and uses `Console.Beep` for alerts; `Console.Beep` does not honor volume on all Windows versions, so volume is stored but may not be audible on every device. (Known limitation)
- **Backup/Restore** — `BackupView`/`Restore` shell out to `pg_dump`/`pg_restore`; ensure PostgreSQL `bin` is on the system `PATH` for backup/restore to succeed.