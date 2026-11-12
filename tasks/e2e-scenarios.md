# DD.exe Behavior Comparison Scenarios

Scenarios used for Phase 6.1 E2E behavior parity testing. Each scenario records the DD.exe behavior on known test data so b-pro-max output can be compared side-by-side.

## Test Data Baseline (PostgreSQL `bpro_max_test`)

Agents (seeded per scenario, cleaned between tests):

| # | agent_name | address | phone | commission | rate | commission1 | rate1 | currency_type | currency_rate |
|---|---|---|---|---|---|---|---|---|---|
| A1 | Alpha Agent | 1 Test St | 02-111-2222 | 1000 | 5 | 500 | 2.5 | THB | 1 |
| A2 | Beta Agent | 2 Test St | 02-333-4444 | 2000 | 7.5 | 1000 | 3.5 | USD | 35.5 |
| A3 | Gamma Agent | 3 Test St | 08-555-6666 | 3000 | 10 | 1500 | 5 | THB | 1 |

## Scenario SC-01: Agent CRUD (Create/Read/Update/Delete)

Mirrors DD.exe `AgentThai` table operations.

| Step | DD.exe SQL | b-pro-max equivalent | Expected result |
|---|---|---|---|
| Create | `INSERT INTO AgentThai VALUES (...)` | `InsertTestAgentAsync("John Doe", ...)` | id > 0, row present |
| Read all | `SELECT * FROM AgentThai ORDER BY AgentName` | `SELECT * FROM agents ORDER BY agent_name` | rows sorted by name |
| Read by name | `SELECT * FROM AgentThai WHERE AgentName='John Doe'` | `SELECT * FROM agents WHERE agent_name = @name` | single matching row |
| Update | `UPDATE AgentThai SET commission=1500 WHERE ...` | `UPDATE agents SET commission=@c WHERE id=@i` | 1 row affected, new value |
| Delete by name | `DELETE FROM AgentThai WHERE AgentName='...'` | `DELETE FROM agents WHERE agent_name = @name` | 1 row affected, count=0 |

**Coverage:** `AgentCrudE2ETests.cs` (existing) — CreateReadUpdateDeleteAgentMatchesDDExeBehavior, GetAllAgentsReturnsSortedByName, SearchAgentsByNameReturnsMatchingResults, DeleteAgentByIdReturnsAffectedRowCount.

**Status:** ✅ Covered by existing tests.

---

## Scenario SC-02: Sales Filtering (Agent name, date range, currency, commission range, phone)

Mirrors DD.exe `frmSaleFilt` sales filter queries.

| Step | DD.exe behavior | b-pro-max equivalent | Expected result |
|---|---|---|---|
| Filter by name | `WHERE AgentName LIKE '%Alpha%'` | `WHERE agent_name ILIKE @pattern` | matching agents |
| Filter by currency | `WHERE CurrencyType = 'THB'` | `WHERE currency_type = @currency` | THB agents only |
| Filter by commission range | `WHERE Commission BETWEEN min AND max` | `WHERE commission >= @min AND commission <= @max` | agents in range |
| Filter by phone | `WHERE Phone LIKE '02-%'` | `WHERE phone LIKE @pattern` | agents matching prefix |

**Coverage:** `SalesFilterE2ETests.cs` (existing) — FilterAgentsByName, ByCurrencyType, ByCommissionRange, ByPhoneNumber.

**Status:** ✅ Covered by existing tests.

---

## Scenario SC-03: Commission Calculation

Mirrors DD.exe commission logic: `commission = commission * rate / 100`.

| Step | DD.exe behavior | b-pro-max equivalent | Expected result |
|---|---|---|---|
| Commission with rate | commission=1000, rate=5 → 50 | `commission * rate / 100m` | 50 |
| Zero rate | commission=5000, rate=0 → 0 | `commission * rate / 100m` | 0 |
| Dual rate | commission1=500, rate1=2.5 stored | columns commission1/rate1 | values stored correctly |
| Multiple agents total | sum of individual commissions | sum over rows | 500 for A/B/C baseline |

**Coverage:** `CommissionCalculationE2ETests.cs` (existing).

**Status:** ✅ Covered by existing tests.

---

## Scenario SC-04: Draw / Result Entry (GAP — not yet tested)

Mirrors DD.exe `frmDraw` / result entry: open draw, enter winning numbers, close draw, mark drawn.

| Step | DD.exe behavior | b-pro-max equivalent | Expected result |
|---|---|---|---|
| Open draw | Insert draw record with status Open | `DrawService.OpenDraw(...)` | draw status = Open |
| Enter result | Update draw with winning numbers | `DrawService.EnterResult(...)` | winning numbers stored |
| Close draw | Set status Closed | `DrawService.CloseDraw(...)` | draw status = Closed |
| Mark drawn | Set status Drawn | `DrawService.MarkDrawn(...)` | draw status = Drawn |
| Cancel draw | Reset status | `DrawService.CancelDraw(...)` | draw cancelled |

**Coverage:** None — new `DrawsE2ETests.cs` needed.

**Files:** `tests/e2e/DrawsE2ETests.cs` (new).

---

## Scenario SC-05: 2D Sale Entry (Batch / Single / Quick Add) (GAP — not yet tested)

Mirrors DD.exe sale panel: batch number entry, R toggle, Quick Add, per-base/total split, max payout.

| Step | DD.exe behavior | b-pro-max equivalent | Expected result |
|---|---|---|---|
| Batch sale | Parse batch, expand R, insert sale_items | `SaleEntryService.CreateSaleAsync()` with `NumberStakes` | sale items created, totals correct |
| Single sale | Insert single sale item | `SaleEntryService.CreateSaleAsync()` with `Selection` | one sale item |
| Quick Add | Pre-defined number set | `SaleEntryService.CreateSaleAsync()` with `NumberGroupCode` | sale item with preset numbers |
| Payout calculation | commission/rate-based payout | `CommissionService.CalculateCommission()` + 80x multiplier | payout matches DD.exe |
| Auto-buy trigger | Auto-purchase when total >= trigger | `AutoBuyService` + `SaleEntryService.EvaluateAutoBuyAsync()` | auto-buy rule triggered |

**Coverage:** ✅ Covered by `tests/e2e/SellWorkflowE2ETests.cs`:
- `CompleteSellWorkflowSavesSaleAndAppearsInHistory` — batch + single + Quick Add + auto-buy + sales-history verification
- `SellWorkflowRejectsClosedDraw` — closed draw rejection
- `SellWorkflowBatchWithPreResolvedStakes` — batch pre-resolved stakes path

**Files:** `tests/e2e/SellWorkflowE2ETests.cs` (new).

---

## Scenario SC-06: Report Export (PDF / Excel / CSV) (GAP — not yet tested)

Mirrors DD.exe `frmReport` / `frmF5Report`: agent report, sales report, export to file.

| Step | DD.exe behavior | b-pro-max equivalent | Expected result |
|---|---|---|---|
| PDF export | Generate PDF report | `ReportService.GeneratePdf(...)` | PDF file created, contains expected data |
| Excel export | Generate XLSX | `ExcelExportService.Export(...)` | XLSX file created, contains expected data |
| CSV export | Generate CSV | `CsvExportService.Export(...)` | CSV file created, contains expected data |

**Coverage:** None — new `ReportExportE2ETests.cs` needed.

**Files:** `tests/e2e/ReportExportE2ETests.cs` (new).

---

## Scenario SC-07: Auto-Buy / Subscription (GAP — not yet tested)

Mirrors DD.exe `frmAutoBuy`: recurring purchase rules, auto-purchase on draw.

| Step | DD.exe behavior | b-pro-max equivalent | Expected result |
|---|---|---|---|
| Create rule | Insert auto-buy rule | `AutoBuyService.CreateRule(...)` | rule stored |
| Execute rule | Auto-purchase on draw date | `AutoBuyService.ExecuteRules(...)` | sale created per rule |

**Coverage:** None — new `AutoBuyE2ETests.cs` needed.

**Files:** `tests/e2e/AutoBuyE2ETests.cs` (new).

---

## Scenario SC-08: Backup / Restore (already covered)

Mirrors DD.exe `frmBackUpData`: pg_dump / pg_restore.

**Coverage:** `BackupRestoreE2ETests.cs` (existing).

**Status:** ✅ Covered by existing tests.

---

## Scenario SC-09: Database Compaction (already covered)

Mirrors DD.exe compact/repair: VACUUM / REINDEX.

**Coverage:** `DatabaseCompactionE2ETests.cs` (existing).

**Status:** ✅ Covered by existing tests.

---

## Implementation Priority (E2E gap tasks)

| Task | Scenario | File | Status |
|---|---|---|---|
| Task 2 | SC-01 Agent CRUD | `AgentCrudE2ETests.cs` | ✅ Existing |
| Task 3 | SC-05 Sale entry | `SaleEntryE2ETests.cs` | ⬜ New |
| Task 4 | SC-06 Report export | `ReportExportE2ETests.cs` | ⬜ New |
| Task 5 | SC-04 Draws + SC-07 Auto-buy | `DrawsE2ETests.cs`, `AutoBuyE2ETests.cs` | ⬜ New |

**Execution constraint:** E2E/UI tests require a Windows desktop with the WinRT runtime registered (`REGDB_E_CLASSNOTREG` in containers). Non-UI e2e tests (database-driven) run anywhere PostgreSQL is available. The scenario definitions above are database-driven and can be executed on any host with PostgreSQL.
