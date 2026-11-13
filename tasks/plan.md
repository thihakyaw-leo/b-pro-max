# Implementation Plan: Phase 6 — Polish & Verification

## Status: ✅ COMPLETE

All Phase 6 tasks are done and verified. Final gate: `dotnet format --verify-no-changes` clean, src/tests/e2e builds 0 warnings / 0 errors, 255 unit tests + 40 e2e tests passing.

## Overview
b-pro-max (WinUI 3 reimplementation of DD.exe) has Phases 0–7 complete and verified (255 unit + 40 e2e tests passing, quality gate clean). Phase 6 covered e2e behavior parity vs DD.exe, performance benchmarking, UI/UX polish, documentation/deployment, and a final dependency audit confirming zero VB6/ActiveX/Registry/MSVBVM60 dependencies.

## Architecture Decisions
- Keep Phase 6 work scoped to verification/polish — no new feature development.
- Benchmark scratch lives under `benchmarks/` (gitignored — never commit); the published baseline lives in `benchmarks/BenchmarkResults.md`.
- E2E tests continue to run against local PostgreSQL; behavior is compared side-by-side with DD.exe on identical test data (scenarios in `tasks/e2e-scenarios.md`).
- XAML compilation enabled; all views use `InitializeComponent()`; controls resolved via `FindChildByName<T>()` with `AutomationProperties.AutomationId`.

## Task List

### Phase 6.1 — E2E behavior parity vs DD.exe
- [x] Task 1: Define DD.exe behavior comparison scenarios — `tasks/e2e-scenarios.md`
- [x] Task 2: Add e2e tests mirroring DD.exe agent CRUD — `tests/e2e/AgentCrudE2ETests.cs`
- [x] Task 3: Add e2e tests mirroring DD.exe sale entry — `tests/e2e/SaleEntryE2ETests.cs`
- [x] Task 4: Add e2e tests mirroring DD.exe report export — `tests/e2e/ReportExportE2ETests.cs`
- [x] Task 5: Run e2e suite and document behavior gaps vs DD.exe — 40 e2e tests passing

### Checkpoint: E2E parity
- [x] E2E tests build and pass — 40/40 against local PostgreSQL
- [x] Behavior gaps vs DD.exe documented — `tasks/e2e-scenarios.md`

### Phase 6.2 — Performance benchmarking
- [x] Task 6: Create benchmark scratch project under benchmarks/ — gitignored scratch project
- [x] Task 7: Measure startup time, memory, query performance — BatchSaleBuilder / Commission / NumberResolution / Payout
- [x] Task 8: Document benchmark results vs DD.exe baseline — `benchmarks/BenchmarkResults.md`

### Checkpoint: Benchmarking
- [x] Benchmark project builds and runs
- [x] Results documented vs DD.exe baseline

### Phase 6.3 — UI/UX polish
- [x] Task 9: Audit AutomationId on all WinUI views (accessibility) — all interactive controls carry an AutomationId
- [x] Task 10: Apply theme resources / consistent styling across views — hardcoded `Foreground="Green"` → `ThemeResource` in 14 files
- [x] Task 11: Verify responsive layout / window sizing — `MainWindow.SizeWindow()` sizes to 1180×760 dip

### Checkpoint: UI/UX polish
- [x] All views have AutomationId where interactive
- [x] Styling uses theme resources consistently
- [x] No layout regressions in build

### Phase 6.4 — Documentation & deployment
- [x] Task 12: Write deployment/packaging guide (MSIX) — `docs/deployment.md`
- [x] Task 13: Update README with build/run/deploy steps — `README.md`

### Checkpoint: Documentation
- [x] README build/run steps accurate
- [x] Deployment guide present

### Phase 6.5 — Final dependency audit
- [x] Task 14: Scan codebase for VB6/ActiveX/Registry/MSVBVM60/winmm.dll dependencies — none found
- [x] Task 15: Verify no hardcoded D:\ paths remain — none found
- [x] Task 16: Confirm zero original-binary dependencies — no DD.exe code copied

### Checkpoint: Complete
- [x] Audit passes: no VB6/ActiveX/Registry/MSVBVM60/winmm.dll references
- [x] Phase 6 all tasks complete
- [x] Ready for release review

## Risks and Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| WinRT activation unavailable in container (REGDB_E_CLASSNOTREG) | High | E2E/UI tests must run on Windows desktop with WinRT runtime; non-UI tests run anywhere |
| Behavior gaps vs DD.exe found late | Med | Run e2e comparison early (Task 5) before polishing |
| Benchmark results non-representative | Low | Measure on same hardware, multiple runs, document environment |
| `winapp` CLI loses the window handle after Frame navigation | Med | UI automation (`ui-tests.ps1`) covers the pre-login shell only; post-login flows remain unverified by the harness — the app itself renders and logs in correctly |

## Open Questions
- None — Phase 6 scope is fully delivered.
