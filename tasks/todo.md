# Task List: Phase 6 — Polish & Verification

Reference: `tasks/plan.md` — no pre-existing incomplete plan was overwritten (tasks/ did not exist).

---

## Phase 6.1 — E2E behavior parity vs DD.exe

## Task 1: Define DD.exe behavior comparison scenarios

**Description:** Enumerate the DD.exe behaviors to compare against b-pro-max on identical test data: agent CRUD, sale entry (batch/single/Quick Add), result/draw entry, payout settlement, and report export (PDF/Excel/CSV). Record expected outputs for each scenario.

**Acceptance criteria:**
- [ ] Written scenario list covers agent CRUD, sale entry, draw/result, payout, and report export
- [ ] Each scenario has expected output defined from DD.exe on known test data
- [ ] Scenario list stored in `tasks/e2e-scenarios.md` (or referenced from plan.md)

**Verification:**
- [ ] Reviewed with user; scenarios match DD.exe behavior
- [ ] Build/tests unaffected (documentation-only task)

**Dependencies:** None

**Files likely touched:** `tasks/e2e-scenarios.md` (new)

**Estimated scope:** S (1 file)

---

## Task 2: Add e2e tests mirroring DD.exe agent CRUD

**Description:** Add xUnit e2e tests under `tests/e2e/` that exercise agent create/read/update/delete through b-pro-max services against local PostgreSQL, asserting the same outcomes as DD.exe for the defined scenarios.

**Acceptance criteria:**
- [ ] Agent CRUD e2e tests added (create, read, update, delete)
- [ ] Tests assert b-pro-max output matches DD.exe expected output
- [ ] Tests run against local PostgreSQL `bpro_max` test database

**Verification:**
- [ ] `dotnet test tests\e2e\b-pro-max.e2e.csproj` builds and new tests pass
- [ ] Full e2e suite still green (no regressions)

**Dependencies:** Task 1

**Files likely touched:** `tests/e2e/*AgentCrud*Tests.cs` (new)

**Estimated scope:** S (1-2 files)

---

## Task 3: Add e2e tests mirroring DD.exe sale entry

**Description:** Add e2e tests for sale entry (batch, single, Quick Add) asserting totals, commission, and payout match DD.exe on the same input numbers.

**Acceptance criteria:**
- [ ] Sale entry e2e tests added (batch + single + Quick Add)
- [ ] Tests assert totals/commission/payout match DD.exe expected output
- [ ] Tests use transactional sale entry path

**Verification:**
- [ ] New sale entry e2e tests pass
- [ ] Full e2e suite green

**Dependencies:** Task 1, Task 2

**Files likely touched:** `tests/e2e/*SaleEntry*Tests.cs` (new)

**Estimated scope:** S (1-2 files)

---

## Task 4: Add e2e tests mirroring DD.exe report export

**Description:** Add e2e tests for report export (PDF/Excel/CSV) asserting generated files contain expected data and match DD.exe report output where applicable.

**Acceptance criteria:**
- [ ] Report export e2e tests added (PDF, Excel, CSV)
- [ ] Tests assert file contents match expected data
- [ ] Tests verify export runs without exception

**Verification:**
- [ ] New report export e2e tests pass
- [ ] Full e2e suite green

**Dependencies:** Task 1

**Files likely touched:** `tests/e2e/*ReportExport*Tests.cs` (new)

**Estimated scope:** S (1-2 files)

---

## Task 5: Run e2e suite and document behavior gaps vs DD.exe

**Description:** Run the full e2e suite against local DD.exe data (where DD.exe is runnable) and document any behavior gaps between DD.exe and b-pro-max.

**Acceptance criteria:**
- [ ] Full e2e suite runs and passes on Windows desktop with WinRT
- [ ] Behavior gaps vs DD.exe documented in `tasks/e2e-gaps.md`
- [ ] Gaps categorized as blocking / non-blocking / out-of-scope

**Verification:**
- [ ] E2E suite green on Windows host
- [ ] Gap document reviewed

**Dependencies:** Tasks 1-4

**Files likely touched:** `tasks/e2e-gaps.md` (new)

**Estimated scope:** S (1 file)

---

### Checkpoint: E2E parity
- [ ] E2E tests build and pass
- [ ] Behavior gaps vs DD.exe documented

---

## Phase 6.2 — Performance benchmarking

## Task 6: Create benchmark scratch project under benchmarks/

**Description:** Create a gitignored benchmark scratch project under `benchmarks/` to measure app startup time, memory usage, and query performance. Do not add to the solution.

**Acceptance criteria:**
- [ ] Benchmark project created under `benchmarks/` (gitignored)
- [ ] Benchmark project is NOT referenced by `b-pro-max.sln`
- [ ] Benchmark project builds independently

**Verification:**
- [ ] `dotnet build` of benchmark project succeeds
- [ ] `benchmarks/` confirmed gitignored (not tracked)

**Dependencies:** None

**Files likely touched:** `benchmarks/Benchmark.csproj` (new, gitignored)

**Estimated scope:** S (1 file)

---

## Task 7: Measure startup time, memory, query performance

**Description:** Run benchmarks measuring app startup time, memory footprint, and key database query performance on the local PostgreSQL instance.

**Acceptance criteria:**
- [ ] Startup time measured (multiple runs, average)
- [ ] Memory usage measured
- [ ] Key query performance measured (agent list, sale query, report query)
- [ ] Raw results recorded

**Verification:**
- [ ] Benchmark run produces reproducible measurements
- [ ] Results recorded in `benchmarks/results.md` (or similar)

**Dependencies:** Task 6

**Files likely touched:** `benchmarks/*.cs` (new, gitignored)

**Estimated scope:** S (1-2 files)

---

## Task 8: Document benchmark results vs DD.exe baseline

**Description:** Compare measured b-pro-max benchmarks against the DD.exe baseline (4.3 MB .text section app) and document findings.

**Acceptance criteria:**
- [ ] Benchmark results compared to DD.exe baseline
- [ ] Findings documented in `benchmarks/comparison.md` (or plan.md)
- [ ] Any performance regressions flagged

**Verification:**
- [ ] Comparison document reviewed
- [ ] No unexplained performance regression

**Dependencies:** Task 7

**Files likely touched:** `benchmarks/comparison.md` (new, gitignored)

**Estimated scope:** S (1 file)

---

### Checkpoint: Benchmarking
- [ ] Benchmark project builds and runs
- [ ] Results documented vs DD.exe baseline

---

## Phase 6.3 — UI/UX polish

## Task 9: Audit AutomationId on all WinUI views (accessibility)

**Description:** Review all 16 WinUI Page views and ensure interactive controls have `AutomationId` set for accessibility/UI automation.

**Acceptance criteria:**
- [ ] All interactive controls across views have AutomationId
- [ ] AutomationId values follow a consistent naming convention
- [ ] No duplicate AutomationIds within a view

**Verification:**
- [ ] Build succeeds (TreatWarningsAsErrors — no new warnings)
- [ ] Manual review of XAML/code-behind for AutomationId coverage

**Dependencies:** None

**Files likely touched:** `src/Views/*.xaml` + `src/Views/*.xaml.cs` (up to 16 pairs)

**Estimated scope:** M (3-5 files touched per session; spread across views)

---

## Task 10: Apply theme resources / consistent styling across views

**Description:** Ensure views use theme resources (ThemeResource) consistently rather than hardcoded SolidColorBrush/FontSize/FontWeight, per prior winui-code-review fixes.

**Acceptance criteria:**
- [ ] No hardcoded SolidColorBrush colors remain in views
- [ ] Font sizes/weights use theme styles
- [ ] Styling consistent across all 16 views

**Verification:**
- [ ] Build succeeds with 0 warnings/errors
- [ ] `dotnet format --verify-no-changes` clean

**Dependencies:** Task 9 (audit first)

**Files likely touched:** `src/Views/*.xaml`

**Estimated scope:** M (multiple view files)

---

## Task 11: Verify responsive layout / window sizing

**Description:** Verify views render correctly at different window sizes and the NavigationView shell handles resizing without layout breakage.

**Acceptance criteria:**
- [ ] Views checked at multiple window sizes
- [ ] No overlapping or clipped controls at common sizes
- [ ] NavigationView shell stable on resize

**Verification:**
- [ ] Build succeeds
- [ ] Manual/layout review (UI smoke test on Windows desktop where possible)

**Dependencies:** Task 10

**Files likely touched:** `src/Views/*.xaml`, `src/MainWindow*` (if shell changes needed)

**Estimated scope:** S (1-2 files)

---

### Checkpoint: UI/UX polish
- [ ] All views have AutomationId where interactive
- [ ] Styling uses theme resources consistently
- [ ] No layout regressions in build

---

## Phase 6.4 — Documentation & deployment

## Task 12: Write deployment/packaging guide (MSIX)

**Description:** Document how to build, package (MSIX), and deploy b-pro-max on Windows desktop, including WinRT runtime prerequisites.

**Acceptance criteria:**
- [ ] Deployment guide written (`docs/deployment.md` or similar)
- [ ] MSIX packaging steps included
- [ ] WinRT prerequisite noted

**Verification:**
- [ ] Document reviewed for accuracy
- [ ] Build/tests unaffected (documentation-only)

**Dependencies:** None

**Files likely touched:** `docs/deployment.md` (new)

**Estimated scope:** S (1 file)

---

## Task 13: Update README with build/run/deploy steps

**Description:** Ensure `README.md` has accurate build, test, run, and deploy commands matching the CI gate and AGENTS.md commands.

**Acceptance criteria:**
- [ ] README build command matches `dotnet build b-pro-max.sln -c Release`
- [ ] README test commands match unit + e2e test commands
- [ ] README run command matches `dotnet run --project src\b-pro-max.csproj`
- [ ] README notes Windows desktop requirement

**Verification:**
- [ ] README commands verified against AGENTS.md/CI
- [ ] Build/tests unaffected

**Dependencies:** None

**Files likely touched:** `README.md`

**Estimated scope:** S (1 file)

---

### Checkpoint: Documentation
- [ ] README build/run steps accurate
- [ ] Deployment guide present

---

## Phase 6.5 — Final dependency audit

## Task 14: Scan codebase for VB6/ActiveX/Registry/MSVBVM60/winmm.dll dependencies

**Description:** Search the entire codebase for any remaining references to VB6 runtime (MSVBVM60), ActiveX controls, Registry writes, or winmm.dll that would violate the zero-original-dependency goal.

**Acceptance criteria:**
- [ ] grep/scan for MSVBVM60, winmm.dll, Reg*, ActiveX, Ocx returns no production hits
- [ ] Any hits are documented as intentional/explained or removed
- [ ] No Registry writes in production code (JSON config only)

**Verification:**
- [ ] `git grep` / ripgrep scan clean for forbidden dependencies
- [ ] Build succeeds (0 warnings)

**Dependencies:** None

**Files likely touched:** scan only; fixes if any found

**Estimated scope:** S (scan + possible fixes)

---

## Task 15: Verify no hardcoded D:\ paths remain

**Description:** Confirm no hardcoded `D:\` paths (from original DD.exe environment) remain in the codebase.

**Acceptance criteria:**
- [ ] ripgrep for `D:\` returns no production hits
- [ ] All paths use config/relative/environment-based locations

**Verification:**
- [ ] ripgrep scan clean
- [ ] Build succeeds

**Dependencies:** Task 14

**Files likely touched:** scan only; fixes if any found

**Estimated scope:** S (scan)

---

## Task 16: Confirm zero original-binary dependencies

**Description:** Final confirmation that b-pro-max has zero dependencies on the original DD.exe binary, runtime, ActiveX controls, or Registry storage.

**Acceptance criteria:**
- [ ] Task 14 and Task 15 both pass
- [ ] App builds and unit tests pass (246 green)
- [ ] E2E tests pass (19 green)
- [ ] Final audit checklist signed off

**Verification:**
- [ ] Full quality gate: `dotnet format --verify-no-changes`, build, unit tests, e2e tests all green
- [ ] Audit documented in plan.md/todos.md

**Dependencies:** Tasks 14, 15

**Files likely touched:** none (verification only)

**Estimated scope:** S (verification)

---

### Checkpoint: Complete
- [ ] Audit passes: no VB6/ActiveX/Registry/MSVBVM60/winmm.dll references
- [ ] Phase 6 all tasks complete
- [ ] Ready for release review
