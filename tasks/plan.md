# Implementation Plan: Phase 6 — Polish & Verification

## Overview
b-pro-max (WinUI 3 reimplementation of DD.exe) has Phases 0–5 complete and verified (246 unit + 40 e2e tests passing, quality gate clean). Phase 6 polish/verification remains: e2e behavior parity vs DD.exe, performance benchmarking, UI/UX polish, documentation/deployment, and a final dependency audit to confirm zero VB6/ActiveX/Registry/MSVBVM60 dependencies.

## Architecture Decisions
- Keep Phase 6 work scoped to verification/polish — no new feature development.
- Benchmark scratch lives under `benchmarks/` (gitignored — never commit).
- E2E tests continue to run against local PostgreSQL; behavior is compared side-by-side with DD.exe on identical test data.
- XAML compilation remains disabled (MarkupCompilePass1/2 no-op'd); UI polish is limited to code-behind styling/AutomationId, not new XAML pages.

## Task List

### Phase 6.1 — E2E behavior parity vs DD.exe
- [ ] Task 1: Define DD.exe behavior comparison scenarios
- [ ] Task 2: Add e2e tests mirroring DD.exe agent CRUD
- [ ] Task 3: Add e2e tests mirroring DD.exe sale entry
- [ ] Task 4: Add e2e tests mirroring DD.exe report export
- [ ] Task 5: Run e2e suite and document behavior gaps vs DD.exe

### Checkpoint: E2E parity
- [ ] E2E tests build and pass
- [ ] Behavior gaps vs DD.exe documented

### Phase 6.2 — Performance benchmarking
- [ ] Task 6: Create benchmark scratch project under benchmarks/
- [ ] Task 7: Measure startup time, memory, query performance
- [ ] Task 8: Document benchmark results vs DD.exe baseline

### Checkpoint: Benchmarking
- [ ] Benchmark project builds and runs
- [ ] Results documented vs DD.exe baseline

### Phase 6.3 — UI/UX polish
- [ ] Task 9: Audit AutomationId on all WinUI views (accessibility)
- [ ] Task 10: Apply theme resources / consistent styling across views
- [ ] Task 11: Verify responsive layout / window sizing

### Checkpoint: UI/UX polish
- [ ] All views have AutomationId where interactive
- [ ] Styling uses theme resources consistently
- [ ] No layout regressions in build

### Phase 6.4 — Documentation & deployment
- [ ] Task 12: Write deployment/packaging guide (MSIX)
- [ ] Task 13: Update README with build/run/deploy steps

### Checkpoint: Documentation
- [ ] README build/run steps accurate
- [ ] Deployment guide present

### Phase 6.5 — Final dependency audit
- [ ] Task 14: Scan codebase for VB6/ActiveX/Registry/MSVBVM60/winmm.dll dependencies
- [ ] Task 15: Verify no hardcoded D:\ paths remain
- [ ] Task 16: Confirm zero original-binary dependencies

### Checkpoint: Complete
- [ ] Audit passes: no VB6/ActiveX/Registry/MSVBVM60/winmm.dll references
- [ ] Phase 6 all tasks complete
- [ ] Ready for release review

## Risks and Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| WinRT activation unavailable in container (REGDB_E_CLASSNOTREG) | High | E2E/UI tests must run on Windows desktop with WinRT runtime; non-UI tests run anywhere |
| Behavior gaps vs DD.exe found late | Med | Run e2e comparison early (Task 5) before polishing |
| Benchmark results non-representative | Low | Measure on same hardware, multiple runs, document environment |

## Open Questions
- None — Phase 6 scope is well-defined from plan.md.
