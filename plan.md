# plan.md — b-pro-max Project Plan

**Project:** b-pro-max (modern upgrade of DD.exe)  
**Location:** `F:\b\b-pro-max\`  
**Source:** `F:\b\DD.exe` (VB6 lottery/agent management system)  
**Date:** 2026-11-10  
**Tech Stack:** WinUI 3 + C# (.NET 9) + PostgreSQL + PdfSharpCore/QuestPDF + EPPlus + JSON config

---

## 1. Vision

Rebuild DD.exe as a modern WinUI 3 desktop application with the **same internal functionality** — agent management, sales tracking, commission calculation, reporting, backup/compact, sound alerts, time-lock access — but with a modern tech stack and zero dependencies on the original VB6 binary, runtime, ActiveX controls, or Registry storage.

---

## 2. Phase 0 — Foundation (Week 1)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P0-01 | Initialize repo (git, .sln, csproj, WinUI 3 template) | `F:\b\b-pro-max\b-pro-max.sln` |
| P0-02 | Set up WinUI 3 + C# project structure | `App.xaml.cs`, `MainWindow.xaml`, `Views/`, `Models/`, `Services/` |
| P0-03 | Configure PostgreSQL connection (Npgsql) | `Services/DatabaseService.cs` |
| P0-04 | Create `agents` table schema (matching AgentThai fields) | `Scripts/001_agents.sql` |
| P0-05 | Set up JSON configuration (appsettings.json) | `appsettings.json` |
| P0-06 | Configure EPPlus, PdfSharpCore, QuestPDF package references | `.csproj` PackageReference entries |
| P0-07 | Configure code analysis (.editorconfig, warnings as errors) | `.editorconfig` |

**Exit criteria:** App launches, PostgreSQL connects, config loads from JSON, WinUI window displays.

---

## 3. Phase 1 — Core Agent Management (Weeks 2–3)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P1-01 | Implement agent CRUD service (C# + Npgsql) | `Services/AgentService.cs` |
| P1-02 | Build agent list view with search/filter (WinUI `ListView`) | `Views/AgentListView.xaml` |
| P1-03 | Build agent detail/edit form (XAML) | `Views/AgentDetailView.xaml` |
| P1-04 | Implement `SELECT * FROM agents` query | `Services/AgentService.cs` |
| P1-05 | Implement `WHERE agent_name = @name` filtered query | `Services/AgentService.cs` |
| P1-06 | Implement `ORDER BY agent_name` sorted query | `Services/AgentService.cs` |
| P1-07 | Add agent deletion with confirmation dialog (XAML ContentDialog) | `Views/AgentListView.xaml` |
| P1-08 | Unit tests for agent service (xUnit) | `tests/AgentServiceTests.cs` |

**Exit criteria:** Full agent CRUD works through WinUI UI, data persists in PostgreSQL.

---

## 4. Phase 2 — Sales & Commission (Weeks 4–5)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P2-01 | Implement commission/rate calculation logic | `Services/CommissionService.cs` |
| P2-02 | Build sales filtering UI (WinUI `ListView` + DatePicker) | `Views/SalesFilterView.xaml` |
| P2-03 | Build auto-purchase feature | `Views/AutoBuyView.xaml` |
| P2-04 | Add currency type/rate fields (CurType, CurRate) | `Scripts/002_currency.sql` + UI |
| P2-05 | Implement commission payment/receive tracking | `Services/PaymentService.cs` |
| P2-06 | Unit tests for commission logic | `tests/CommissionServiceTests.cs` |

**Exit criteria:** Commission calculations match original DD.exe logic, sales filter works with native DatePicker.

---

## 5. Phase 3 — Reporting & Export (Weeks 6–7)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P3-01 | Replace Crystal Reports with PdfSharpCore PDF generation | `Services/ReportService.cs` (PdfSharpCore) |
| P3-02 | Build agent report view | `Views/ReportView.xaml` |
| P3-03 | Implement Excel export (EPPlus) | `Services/ExcelExportService.cs` |
| P3-04 | Implement CSV export | `Services/CsvExportService.cs` |
| P3-05 | Unit tests for report generation | `tests/ReportServiceTests.cs` |

**Exit criteria:** Reports generate via PdfSharpCore, Excel/CSV export works.

---

## 6. Phase 4 — Database Maintenance (Week 8)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P4-01 | Implement backup/restore (PostgreSQL pg_dump/pg_restore) | `Services/BackupService.cs` |
| P4-02 | Implement DB compaction/repair (PostgreSQL VACUUM/REINDEX) | `Services/DatabaseMaintenanceService.cs` |
| P4-03 | Implement create new database | `Services/DatabaseCreationService.cs` |
| P4-04 | Unit tests for DB maintenance | `tests/DatabaseMaintenanceTests.cs` |

**Exit criteria:** Backup, compact, and new-DB creation all work against PostgreSQL.

---

## 7. Phase 5 — System Features (Weeks 9–10)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P5-01 | Implement login/auth (WinUI XAML) | `Views/LoginView.xaml`, `Services/AuthService.cs` |
| P5-02 | Implement registration/licensing | `Services/LicensingService.cs` |
| P5-03 | Implement sound settings (.NET SoundPlayer, no winmm.dll) | `Services/SoundService.cs`, `Views/SoundSettingsView.xaml` |
| P5-04 | Implement time-lock access control | `Services/TimeLockService.cs`, `Views/TimeLockView.xaml` |
| P5-05 | Implement regional/locale settings (JSON config) | `Services/RegionalSettingsService.cs`, `appsettings.json` (`Regional` / `RegionalPresets`) |
| P5-06 | Implement maintenance dashboard | `Views/MaintenanceView.xaml` |

**Exit criteria:** Auth, licensing, sound, time-lock, regional all functional.

---

## 8. Phase 6 — Polish & Verification (Week 11)

| Task | Description | Deliverable |
|------|-------------|-------------|
| P6-01 | End-to-end testing against original DD.exe behavior | `tests/e2e/` |
| P6-02 | Performance benchmarking (compare to 4.3 MB .text section app) | Local scratch under `benchmarks/` (gitignored — do not commit) |
| P6-03 | UI/UX polish (Modern Controls, responsive layout) | `Views/` |
| P6-04 | Documentation and deployment scripts | `docs/`, `scripts/` |
| P6-05 | Final audit: no VB6/ActiveX/Registry dependencies remain | Audit checklist |

**Exit criteria:** Feature parity with DD.exe, zero original-binary dependencies, app passes e2e tests.

---

## 9. Command Order (when it matters)

```
1. dotnet restore          (restore NuGet packages)
2. dotnet build            (build solution)
3. dotnet test             (run unit tests)
4. dotnet run              (run WinUI 3 app)
```

**Always run build → test before committing.**

---

## 10. Testing Strategy

- **Unit tests:** xUnit — agent service, commission logic, report generation, DB maintenance
- **Integration tests:** PostgreSQL queries against test database
- **E2E tests:** WinAppDriver or similar against WinUI 3 app — full agent CRUD flow, report export, backup/restore
- **Test database:** Separate PostgreSQL test database (not production)

---

## 11. Verification Checklist (feature parity with DD.exe)

- [x] Agent CRUD (Create, Read, Update, Delete)
- [x] Agent search by name
- [x] Agent list sorted by name
- [x] Commission/rate calculation
- [x] Sales filtering with native DatePicker
- [x] Auto-purchase
- [x] Currency type/rate support
- [x] PdfSharpCore report generation
- [x] EPPlus Excel export
- [x] CSV export
- [x] PostgreSQL backup
- [x] PostgreSQL compaction/VACUUM
- [x] Create new database
- [x] Login/authentication
- [x] Registration/licensing
- [x] Sound alerts (no winmm.dll)
- [x] Time-lock access control
- [x] Regional/locale settings
- [x] No Registry writes (JSON config only)
- [x] No hardcoded D:\ paths
- [x] No ActiveX controls
- [x] No MSVBVM60 dependency
- [x] WinUI 3 / Windows App SDK only (no WPF, no WinForms)

---

## 12. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Commission logic differs from original | Compare output side-by-side with DD.exe on same test data |
| Crystal Reports formatting lost | PdfSharpCore report templates can replicate the layout |
| ActiveX-only features (date picker, common dialog) | WinUI native DatePicker and FilePicker replace them |
| Registry-based licensing | Move to JSON license file + online activation |
| Large .text section (4.3 MB) implies complex logic | Prioritize core agent/sales features first, add edge cases incrementally |
| WinUI 3 deployment complexity | Use MSIX packaging, test on target Windows 10/11 |

---

## 13. References

- `agent.md` — Agent instructions and source-app architecture
- `todos.md` — Task tracking
- REA analysis evidence: Ghidra 12.1.4 on `F:\b\DD.exe` (1,214 procedures, 3,370 strings)
- Original binary: `F:\b\DD.exe`
