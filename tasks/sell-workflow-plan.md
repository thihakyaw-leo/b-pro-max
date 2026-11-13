# Sell Workflow Review — Gap Analysis & Detailed Plan

**Repository:** thihakyaw-leo/b-pro-max  
**Component:** Sell workflow (`SalesFilterView` + `SaleEntryService` + supporting services)  
**Status:** Review complete, plan created  
**Phase:** Pre-Phase 6 (sell workflow specific — outside the general Phase 6 polish scope)

---

## 1. Current State Summary

### What exists
| Layer | File | Description |
|-------|------|-------------|
| View | `src/Views/SalesFilterView.xaml` | Sell panel XAML: agent/draw combo, batch entry, manual add, quick add, current sale list, save/clear, sales history filter |
| View code-behind | `src/Views/SalesFilterView.cs` (~26 KB) | All event handlers, DI-resolved services, `ILogger` already injected (6 `LoggerMessage.Define` fields) |
| Service | `src/Services/SaleEntryService.cs` (~29 KB) | Transactional sale creation: `CreateSaleAsync`, `CreateSaleCoreAsync`, `PlanItemAsync`, `EvaluateAutoBuyAsync`, `ValidateRequest` |
| Service interface | `src/Services/ISaleEntryService.cs` | `CreateSaleAsync(SaleEntryRequest)` |
| Filter service | `src/Services/ISalesFilterService.cs` + `SalesFilterService.cs` | Sales history filtering (date, agent, status, amount range, search, sort, totals) |
| Models | `src/Models/SaleEntryModels.cs` | `SaleEntryRequest`, `SaleEntryResult`, `SaleEntryItemRequest/Result`, `SaleItemAmountResult` |
| Models | `src/Models/SalePanelModels.cs` | `SalePanelLine` (current sale preview rows) |
| Models | `src/Models/BatchSaleModels.cs` | `BatchSaleRequest/Preview/Line/Number` (batch entry) |
| Models | `src/Models/SaleRecord.cs` | `SaleRecord` (sales history rows, status: Pending/Paid/Cancelled) |
| Supporting services | `INumberBatchParser`, `IBatchSaleBuilder`, `ISalePanelLineFactory`, `ISaleItemAmountCalculator`, `INumberResolutionService`, `IPayoutCalculator`, `ICommissionService`, `IAutoBuyService` | All registered in `App.xaml.cs` DI container |
| Tests (unit) | `tests/SalePanelLineFactoryTests.cs`, `SaleItemAmountCalculatorTests.cs`, `BatchSaleBuilderTests.cs`, `SalesFilterServiceTests.cs` | Individual component validation |
| Tests (e2e) | `tests/e2e/SaleEntryE2ETests.cs` | Single sale, batch stakes, quick add group, closed draw rejection, auto-buy trigger |
| Tests (e2e) | `tests/e2e/SalesFilterE2ETests.cs` | Agent name/currency/commission/phone filtering |

### Verified working
- Build: 0 warnings, 0 errors (Release)
- Unit tests: 246/246 pass
- E2E tests: 37/37 pass
- CI format gate: passes
- `AutomationProperties.AutomationId` present on essentially all interactive controls
- `SaleEntryService` constructor uses optional `ILogger` with `NullLogger` fallback (e2e-safe)
- Transactional sale creation (sale + items + numbers + auto-buy in one DB transaction)
- Auto-buy rules evaluated per sale when `totalStake >= triggerAmount`
- Batch number parsing with comma/space/semicolon/pipe/newline separators + continuous digit grouping
- Reverse (R) number handling including double-number dedup and reverse-pair merging
- Amount modes: per-number, total-split, total

---

## 2. Gap Analysis

The gaps below are organized by severity. These are **sell-workflow-specific** gaps — they do not block compilation or existing tests, but they affect completeness vs. the legacy DD.exe sell workflow and shipping readiness.

### Gap 1 — No end-to-end sell workflow e2e test (Critical)
**Impact:** High. Individual components are unit-tested in isolation, but no test exercises the complete sell flow end-to-end: agent select → draw select → add batch/manual items → preview → save sale → verify in sales history → verify auto-buy trigger.

**Why it matters:** A regression in any single handler (e.g., `SaveSaleButton_Click` wiring) would not be caught by component tests.

**Current coverage:** `SaleEntryE2ETests` tests `SaleEntryService.CreateSaleAsync` directly (bypassing the view). `SalesFilterView` has no e2e test at all.

### Gap 2 — No sale cancellation / refund workflow (High)
**Impact:** Medium. `SaleRecord.Status` supports `"Cancelled"` but there is no UI to cancel a sold sale or process a refund.

**Why it matters:** DD.exe supports voiding/cancelling sales. Without it, agents cannot correct mistakes after a sale is saved.

**Required UI:** Cancel button on sales history row listing, confirmation dialog, status update to "Cancelled", optional refund amount entry.

### Gap 3 — No print receipt after save (High)
**Impact:** Medium. After `SaveSaleButton_Click` succeeds, the user gets a status message but no printed receipt (paper or PDF).

**Why it matters:** Lottery shops typically print a physical receipt for the customer after each sale.

**Existing infra:** `PdfSharpCore` is available in the project for PDF generation.

### Gap 4 — No undo for removed sale lines (Medium)
**Impact:** Low-Medium. `RemoveLine_Click` permanently removes a line from `CurrentSaleList` with no undo.

**Why it matters:** Accidental removals require re-entering the entire line.

### Gap 5 — No clipboard batch import (Medium)
**Impact:** Low. `BatchNumbersBox` only accepts manual typing. No "Paste from clipboard" support.

**Why it matters:** Agents often copy number lists from external sources (SMS, spreadsheets).

### Gap 6 — No sound feedback on save success/failure (Medium)
**Impact:** Low. `ISoundService` is registered in DI but never used in `SalesFilterView`.

**Why it matters:** Audio confirmation is useful in noisy shop environments.

### Gap 7 — No keyboard shortcuts (Low)
**Impact:** Low. All operations require mouse: Add batch, Add sale item, Add quick add, Save, Clear, Filter, Reset.

**Why it matters:** Power users (agents) benefit from keyboard-driven workflow (Enter to add, F2 to save, Esc to clear).

### Gap 8 — No sale preview / confirmation modal before save (Low)
**Impact:** Low. `CurrentSaleList` shows items inline, but there is no summary confirmation step (total stake, commission, max payout) before committing.

**Why it matters:** Reduces accidental saves.

### Gap 9 — No multi-currency display in sell panel (Low)
**Impact:** Low. Agent currency/rate is not shown in the sell panel. Amounts are displayed as raw decimals.

**Why it matters:** Agents working in multiple currencies need to see the currency context.

### Gap 10 — No per-item commission breakdown (Low)
**Impact:** Low. Only total commission is shown (`CurrentSaleCommissionText`). No per-line commission.

### Gap 11 — Validation errors use inline StatusMessage only (Low)
**Impact:** Low. Validation failures appear in `StatusMessage` TextBlock at the bottom of the view. No field-level error indication.

**Why it matters:** Users must scan to the bottom to find errors.

### Gap 12 — No focus management after Add operations (Low)
**Impact:** Low. After clicking Add, focus stays on the Add button instead of moving to the next logical input field.

### Gap 13 — StatusMessage uses hardcoded color (Low)
**Impact:** Low. `StatusMessage.Foreground` is set to `Green`/`Red` hardcoded `SolidColorBrush` instead of theme resources.

---

## 3. Detailed Sell Workflow Plan

Plan file: `tasks/sell-workflow-plan.md` (this document)

Each task includes: ID, title, description, acceptance criteria, files to touch, estimate, and dependencies.

### Phase A: E2E Coverage for Sell Workflow

#### A1. Add full sell workflow e2e test
**Description:** Create `tests/e2e/SellWorkflowE2ETests.cs` with a test that exercises the complete flow through `SaleEntryService`: insert agent → insert draw → insert bet type → create sale (single item + batch item) → verify result (sale ID, total stake, items, numbers, auto-buy flag) → verify sale is queryable via `ISalesFilterService`.

**Acceptance criteria:**
- Test builds and passes against local PostgreSQL
- Covers: single exact sale, batch with stakes, quick add group, auto-buy trigger, closed-draw rejection
- Verifies `SaleEntryResult.SaleId > 0`, `TotalStake`, `Items`, `AutoBuyTriggered`
- Verifies sale appears in filtered history

**Files:**
- `tests/e2e/SellWorkflowE2ETests.cs` (new)

**Estimate:** 1 day  
**Depends on:** None (uses existing `E2ETestBase`, `SaleEntryService`, `ISalesFilterService`)

---

### Phase B: Sale Cancellation / Refund

#### B1. Add `CancelSaleAsync` to `ISaleEntryService` + implementation
**Description:** Add method to mark a sale as cancelled. Sets `Status = "Cancelled"`, `UpdatedAt = now`. Optionally records a refund amount.

**Acceptance criteria:**
- `ISaleEntryService.CancelSaleAsync(long saleId, decimal? refundAmount)` returns `Task<bool>` (true if sale existed and was cancelled)
- `SaleEntryService` implementation updates `sales` table status
- Throws `InvalidOperationException` if sale not found or already cancelled
- Unit test: cancel existing sale → status is "Cancelled"
- Unit test: cancel non-existent sale → returns false / throws

**Files:**
- `src/Services/ISaleEntryService.cs`
- `src/Services/SaleEntryService.cs`
- `tests/SaleEntryServiceTests.cs` (new or extend existing)

**Estimate:** 1 day  
**Depends on:** None

#### B2. Add Cancel button to Sales history UI
**Description:** Add a "Cancel" button per row in `SalesList` ListView (or a selected-row cancel). Confirmation dialog before cancelling.

**Acceptance criteria:**
- Cancel button visible on each sales history row (or on selected row)
- Clicking shows confirmation dialog (ContentDialog)
- On confirm, calls `CancelSaleAsync`, refreshes sales list, shows status message
- Cancelled sales excluded from default filter (or shown with cancelled status)

**Files:**
- `src/Views/SalesFilterView.xaml` (add Cancel button column)
- `src/Views/SalesFilterView.cs` (cancel handler, dialog)

**Estimate:** 1 day  
**Depends on:** B1

---

### Phase C: Print Receipt

#### C1. Add `IReceiptService` interface + PDF implementation
**Description:** Create receipt generation service using `PdfSharpCore`. Receipt includes: sale ID, date, agent name, draw, itemized numbers/stakes/amounts, total stake, commission, max payout, auto-buy info.

**Acceptance criteria:**
- `IReceiptService.GenerateReceiptAsync(saleId, outputPath)` → `Task<string>` (PDF path)
- PDF contains all sale details
- Unit test verifies PDF is generated and non-empty (or mock-based test)

**Files:**
- `src/Services/IReceiptService.cs` (new)
- `src/Services/ReceiptService.cs` (new)
- `src/App.xaml.cs` (register `IReceiptService`)

**Estimate:** 1 day  
**Depends on:** None (PdfSharpCore already referenced)

#### C2. Wire "Print Receipt" button after save
**Description:** After successful `SaveSaleButton_Click`, offer to print/show receipt. Button appears enabled only after a sale is saved.

**Acceptance criteria:**
- "Print Receipt" button appears in save result area
- Clicking generates PDF and opens it (or shows save file dialog)
- Status message confirms receipt saved

**Files:**
- `src/Views/SalesFilterView.xaml`
- `src/Views/SalesFilterView.cs`

**Estimate:** 1 day  
**Depends on:** C1

---

### Phase D: Undo / Clipboard / Sound

#### D1. Add undo for removed sale lines
**Description:** Track the last removed line in a local variable. Show an "Undo" toast/button briefly after removal.

**Acceptance criteria:**
- After `RemoveLine_Click`, an Undo button appears briefly (or status message offers undo)
- Clicking Undo restores the removed line to `CurrentSaleList`

**Files:**
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** None

#### D2. Add clipboard paste to batch numbers
**Description:** Add "Paste" button next to `BatchNumbersBox`. Reads clipboard text and populates the box.

**Acceptance criteria:**
- Paste button reads `Windows.ApplicationModel.DataTransfer.Clipboard`
- Populates `BatchNumbersBox.Text` with clipboard content
- Triggers `BatchNumbersBox_TextChanged` for auto-formatting

**Files:**
- `src/Views/SalesFilterView.xaml`
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** None

#### D3. Add sound feedback on save
**Description:** Inject `ISoundService` into `SalesFilterView`. Play success sound on save, error sound on validation failure.

**Acceptance criteria:**
- `ISoundService` resolved in constructor
- Success sound played after `CreateSaleAsync` succeeds
- Error sound played on validation/save failure

**Files:**
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** None (`ISoundService` already registered)

---

### Phase E: Keyboard Shortcuts & UX Polish

#### E1. Add keyboard shortcuts
**Description:** Add `KeyboardAccelerator` or `KeyDown` handlers: Enter to add current item, F2 to save sale, Esc to clear sale.

**Acceptance criteria:**
- Enter in `SelectionBox`/`BatchNumbersBox` triggers the corresponding Add
- F2 triggers `SaveSaleButton_Click`
- Esc triggers `ClearSaleButton_Click`

**Files:**
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** None

#### E2. Add sale preview summary before save
**Description:** Show a summary (total stake, commission, max payout, item count) in a more prominent way before save. Could be a `ContentDialog` or expanded panel.

**Acceptance criteria:**
- Summary panel shows total stake, item count, commission, max payout
- Visually distinct from the running totals (e.g., bold, highlighted)

**Files:**
- `src/Views/SalesFilterView.xaml`
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** None

#### E3. Add multi-currency display
**Description:** Show agent's currency type and rate in the sell panel (e.g., "Currency: THB (rate: 1.00)").

**Acceptance criteria:**
- Agent combo selection updates a currency display TextBlock
- Amounts show currency suffix where appropriate

**Files:**
- `src/Views/SalesFilterView.xaml`
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** `IAgentService` already injected

---

### Phase F: Validation & Accessibility Polish

#### F1. Field-level validation errors
**Description:** Instead of only bottom `StatusMessage`, show validation errors near the offending field (e.g., red border + tooltip on `BatchAmountBox`).

**Acceptance criteria:**
- Invalid amount highlights the amount TextBox with red border
- Error tooltip shows the validation message
- Clears when user corrects input

**Files:**
- `src/Views/SalesFilterView.xaml`
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.5 day  
**Depends on:** None

#### F2. Focus management after Add
**Description:** After successfully adding a line (batch/manual/quick add), move keyboard focus to the next logical input (e.g., `SelectionBox` after manual add).

**Acceptance criteria:**
- `SelectionBox.Focus(FocusState.Programmatic)` after manual add
- `BatchNumbersBox.Focus(FocusState.Programmatic)` after batch add

**Files:**
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.25 day  
**Depends on:** None

#### F3. Theme resource for StatusMessage colors
**Description:** Replace hardcoded `Colors.Green`/`Colors.Red` with `{ThemeResource SystemControlSuccessTextForegroundBrush}` / `SystemControlErrorTextForegroundBrush`.

**Acceptance criteria:**
- No hardcoded color `SolidColorBrush` in `SalesFilterView.cs` for status messages
- Uses theme resources

**Files:**
- `src/Views/SalesFilterView.cs`

**Estimate:** 0.25 day  
**Depends on:** None

---

### Phase G: Observability (already partially done)

#### G1. Add logger messages for sell workflow events
**Description:** `SalesFilterView` already has `ILogger` with 6 `LoggerMessage.Define` fields. Verify all key events are logged: sale saved, sale cleared, line added, line removed, filter applied, error occurred.

**Acceptance criteria:**
- Each major user action logs at Information level
- Each error logs at Error level with exception
- EventIds are unique and sequential

**Files:**
- `src/Views/SalesFilterView.cs` (verify/complete)

**Estimate:** 0.5 day  
**Depends on:** Already instrumented — verify only

---

## 4. Task Dependency Graph

```
A1 (e2e sell workflow test) ─────────┐
                                      │
B1 (CancelSaleAsync service) ──► B2 (Cancel UI)
C1 (IReceiptService) ─────────► C2 (Print Receipt UI)
D1 (Undo) ────────────────────────────┐
D2 (Clipboard paste) ─────────────────┤
D3 (Sound feedback) ──────────────────┤
E1 (Keyboard shortcuts) ──────────────┼──► All independent, no cross-deps
E2 (Preview summary) ─────────────────┤
E3 (Multi-currency display) ──────────┤
F1 (Field-level validation) ──────────┤
F2 (Focus management) ────────────────┤
F3 (Theme resource colors) ───────────┘
G1 (Observability verify) ────────────┘
```

Phases B, C, D, E, F, G are all independent of each other (except B1→B2, C1→C2). A1 is independent but provides regression coverage for all.

---

## 5. Suggested Execution Order (Incremental)

| Order | Task | Reason |
|-------|------|--------|
| 1 | A1 | E2E test gives safety net for all subsequent changes |
| 2 | B1 + B2 | Cancellation is the highest-value missing feature vs DD.exe |
| 3 | C1 + C2 | Receipt printing is standard lottery-shop workflow |
| 4 | D1 + D2 + D3 | Undo/clipboard/sound are quick UX wins |
| 5 | E1 + E2 + E3 | Keyboard/preview/currency improve agent productivity |
| 6 | F1 + F2 + F3 | Validation/accessibility polish |
| 7 | G1 | Verify observability completeness |

---

## 6. Definition of Done (per task)

- [ ] Code compiles with 0 warnings, 0 errors (`TreatWarningsAsErrors=true`)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] New/changed unit tests pass
- [ ] E2E tests pass against local PostgreSQL
- [ ] No new hardcoded strings that should be localized (globalization is Phase 6)
- [ ] `AutomationProperties.AutomationId` on any new interactive controls
- [ ] No `async void` except event handlers; no `GetAwaiter().GetResult()` in new code
- [ ] `ILogger` used via `LoggerMessage.Define` pattern (no direct `Log*` calls → CA1848)

---

## 7. Out of Scope (Phase 6 general polish)

These are tracked in `tasks/plan.md` and are **not** part of this sell workflow plan:
- MVVM migration (ViewModel, `ObservableProperty`, `RelayCommand`)
- Globalization (`Resources.resw`, `x:Uid`, `ResourceLoader`)
- XAML compilation enablement (`MarkupCompilePass1/2`) — done
- Performance benchmarking
- Final dependency audit (VB6/ActiveX/MSVBVM60)
- Documentation/deployment guide

---

## 8. Open Questions

1. **Cancellation UI:** Should cancelled sales be hidden from the default history filter, or shown with a "Cancelled" status badge? Recommendation: shown with status badge, optional filter to exclude.
2. **Receipt output:** Should the receipt PDF auto-open, or prompt for save location? Recommendation: auto-open via `Process.Start` after saving to `%TEMP%` or a configured receipts folder.
3. **Sound service:** Does `ISoundService` already have methods for success/error tones, or do they need to be added? Need to verify `ISoundService` interface.

---

*End of plan.* 
