# 2D Sales System — Design Specification

**Status:** Draft for approval  
**Scope:** 2D only (`00`–`99`); no 3D or other number types  
**Related:** `todos.md`, `tasks/plan.md`

---

## 1. Design Principles

1. **2D only.** Every bet resolves to one or more two-digit numbers in the range `00`–`99`.
2. **First and second digits are distinct.** `45` and `54` are different numbers. `R` is the only sale type that deliberately covers both.
3. **Dealer rules are data, not code.** Payout multipliers, number-generation rules, and fixed groups live in database tables and can be changed without a rebuild.
4. **Resolve numbers at sale time.** A sale stores the exact numbers and per-number stakes that were sold. Later rule changes must not alter historical sales.
5. **One sale = one transaction.** Sale header, sale items, resolved numbers, and auto-buy evaluation are committed atomically.
6. **Every payout is traceable.** A payout points to the draw, sale, sale item, and resolved number that produced it.

---

## 2. Bet Types and Sale Panel Labels

### 2.1 Approved Sale Panel labels

The Sale Panel must display **only** these Burmese labels. Internal codes exist for the database and service layer and must never appear in the Sale Panel.

| Sale Panel label | Internal code | Selection | Resolved numbers | Amount mode | Payout mode | Default multiplier |
|---|---|---|---|---|---|---|
| **R** | `R` | Two-digit number | Number + reverse (2 numbers) | Total split | Per winning number | 80x |
| **ပါ** | `APA` | One digit | 19 numbers containing the digit | Total split | Per winning number | 80x |
| **ဘရိတ်** | `BRAKE` | One digit | 19 numbers (dealer rule) | Total split | Per winning number | 80x |
| **ထိပ်** | `TOP` | One digit | 10 numbers (`d0`–`d9`) | Total split | Per winning number | 80x |
| **နောက်** | `BOTTOM` | One digit | 10 numbers (`0d`–`9d`) | Total split | Per winning number | 80x |
| **စုံစုံ** | `EVEN_EVEN` | No input | 15 unordered even pairs | Total split | Per winning number | 80x |
| **မမ** | `ODD_ODD` | No input | 15 unordered odd pairs | Total split | Per winning number | 80x |
| **စုံမ** | `MIXED` | No input | 25 ordered mixed-parity numbers | Total split | Per winning number | 80x |
| **ပါဝါ** | `POWER_GROUP` | Quick Add group | 5 base numbers; 10 with R | Total split | Per winning number | 80x |
| **နက္ခတ်** | `NAKKHAT` | Quick Add group | 5 base numbers; 10 with R | Total split | Per winning number | 80x |

**Naming rules**

- The Sale Panel must **not** show `Apa`, `Break`, `Top`, `Bottom`, `Direct`, or any other English/transliterated bet-type name.
- `ပါ` replaces `Apa`; `ဘရိတ်` replaces `Break`; `ထိပ်` replaces `Top`; `နောက်` replaces `Bottom`.
- `R` remains the Latin letter `R` because that is the dealer-approved label.
- `ပါဝါ` and `နက္ခတ်` are the two fixed groups sold through Quick Add.
- The base number row is a direct two-digit bet (internal `DIRECT`). It has no special-type label in the Sale Panel; the ten labels above are the only named sale types.
- `Bine` is a dealer announcement/reference only. It is **not** a bet type and is not stored as a sale item.
- Additional dealer-defined bet types may be added through `bet_types`, but the Sale Panel must still show a Burmese label approved by the dealer.

### 2.2 Amount modes

| Mode | Meaning | Example |
|---|---|---|
| `PerNumber` | The entered amount is the stake for the single selected number. | Base number `45 1,000` → `45: 1,000` |
| `TotalSplit` | The entered amount is the total stake, divided evenly across all resolved numbers. | `45 R 1,000` → `45: 500`, `54: 500` |
| `Total` | The entered amount is a single stake on the whole outcome set; no per-number split. | Optional dealer type only; not used by the ten default sale types |

For `TotalSplit`, rounding is to two decimals. If the division leaves a remainder, the remainder is distributed one cent at a time to the first resolved numbers so that the sum of per-number stakes equals the entered total exactly.

### 2.3 Parity sale types

| Label | Count | Canonical examples | Matching rule |
|---|---|---|---|
| `စုံစုံ` | 15 | `00,02,04,06,08,22,24,26,28,44,46,48,66,68,88` | Both digits even; order does not matter. `42` is matched as `24`. |
| `မမ` | 15 | `11,13,15,17,19,33,35,37,39,55,57,59,77,79,99` | Both digits odd; order does not matter. `53` is matched as `35`. |
| `စုံမ` | 25 | Mixed parity | One even and one odd digit in a fixed position. Default orientation is configurable per dealer. |

The parity sets are dealer-configurable. If a dealer uses the ordered 25-number variant for `စုံစုံ` or `မမ`, the rule engine supports it through `rule_config`; the seed keeps the 15-number unordered variants above.

### 2.4 Dealer-defined rules

Each bet type stores:

```json
{
  "rule_type": "DigitInEitherPosition",
  "rule_config": {
    "digits": "selected",
    "positions": ["tens", "ones"],
    "include_reverse": false
  },
  "payout_mode": "PerWinningNumber",
  "amount_mode": "TotalSplit",
  "payout_multiplier": 80
}
```

Supported `rule_type` values for v1:

- `Exact`
- `ExactAndReverse`
- `DigitInEitherPosition`
- `DigitInPosition`
- `FixedGroup`
- `UnorderedPair`
- `Doubles`
- `Parity`
- `Range`

The rule engine is covered by unit tests; unknown rule types are rejected at save time rather than silently guessing.

---

## 3. နက္ခတ် and ပါဝါ Groups

These are **fixed groups**, not digit-wise mappings. They are sold through Quick Add.

### 3.1 နက္ခတ် (`NAKKHAT`)

| Base | R pair (reverse) |
|---|---|
| `07` | `70` |
| `18` | `81` |
| `24` | `42` |
| `35` | `53` |
| `69` | `96` |

- Base group: 5 numbers.
- With `R`: 10 numbers.
- Per-number payout: 80x.

### 3.2 ပါဝါ (`POWER_GROUP`)

| Base | R pair (reverse) |
|---|---|
| `05` | `50` |
| `16` | `61` |
| `27` | `72` |
| `38` | `83` |
| `49` | `94` |

- Base group: 5 numbers.
- With `R`: 10 numbers.
- Per-number payout: 80x.

### 3.3 Quick Add with optional R

Quick Add is a first-class sales-entry action for both groups:

```
Group:   [ နက္ခတ် ▼ ]   Amount: [ 1,000 ]   [x] R
Preview: 5 numbers · 200 each            (without R)
         10 numbers · 100 each           (with R)
[ Add to Sale ]
```

Rules:

1. **Base group is always included.** Quick Add never adds only the reverse numbers.
2. **`R` is optional.** When checked, the five reverse numbers are added to the same sale item.
3. **One sale item per Quick Add.** The item stores `bet_key = NAKKHAT` or `POWER_GROUP`; the resolved numbers live in `sale_item_numbers`.
4. **Amount is the total stake.** It is split evenly across the resolved numbers:
   - `နက္ခတ် 1,000` → 5 × 200.
   - `နက္ခတ် R 1,000` → 10 × 100.
   - `ပါဝါ 1,900` → 5 × 380.
   - `ပါဝါ R 1,000` → 10 × 100.
5. **Potential payout is shown before adding.** For a group item, the most a single draw can pay is one winning number: `per-number stake × 80`.
6. **R does not create a separate sale item.** It expands the number list of the same item so reporting stays clean.
7. **Dealer override.** A dealer can disable R for a group, change the group members, or change the multiplier in the Number Wisdom admin page.

### 3.4 Worked examples

| Entry | Resolved numbers | Per-number stake | If the draw hits | Payout |
|---|---|---|---|---|
| `45 1,000` (base number) | `45` | `1,000` | `45` | `80,000` |
| `45 R 1,000` | `45`, `54` | `500` each | `45` or `54` | `40,000` |
| `4 ထိပ် 1,000` | `40`–`49` | `100` each | `45` | `8,000` |
| `5 နောက် 1,000` | `05,15,…,95` | `100` each | `45` | `8,000` |
| `စုံစုံ 1,000` | 15 unordered even pairs | `66.67` each | `24` or `42` | `5,333.33` |
| `မမ 1,000` | 15 unordered odd pairs | `66.67` each | `35` or `53` | `5,333.33` |
| `စုံမ 1,000` | 25 ordered mixed pairs | `40` each | selected orientation | `3,200` |
| `5 ဘရိတ် 1,900` | 19 numbers (dealer rule) | `100` each | `45` | `8,000` |
| `5 ပါ 1,900` | 19 numbers containing `5` | `100` each | `45` | `8,000` |
| `နက္ခတ် 1,000` | `07,18,24,35,69` | `200` each | `07` | `16,000` |
| `နက္ခတ် R 1,000` | 10 numbers | `100` each | `07` or `70` | `8,000` |
| `ပါဝါ 1,000` | `05,16,27,38,49` | `200` each | `16` | `16,000` |
| `ပါဝါ R 1,000` | 10 numbers | `100` each | `16` or `61` | `8,000` |

---

## 4. Draw, Result, and Payout Workflow

### 4.1 Draw lifecycle

```
Open → Closed → Drawn → Settled
                 ↘ Cancelled
```

| Status | Meaning |
|---|---|
| `Open` | Sales are accepted. |
| `Closed` | Cutoff reached; no more sales. |
| `Drawn` | Result number entered; payout calculation may run. |
| `Settled` | Payouts generated and reviewable. |
| `Cancelled` | Draw voided; linked sales must be cancelled or moved. |

### 4.2 Settlement

For a draw result `R`:

1. For every `PerWinningNumber` item, find the resolved number equal to `R`. If found:
   `payout = per_number_stake × payout_multiplier`.
2. For every `Total` item, if `R` is in the resolved set:
   `payout = item_amount × payout_multiplier`.
3. Create one `payouts` row per winning item/number.
4. Mark the draw `Settled`.
5. A separate “Pay” action marks payouts `Paid` and records `paid_at` / `paid_by`.

Only one two-digit result exists per draw, so a single sale item can have at most one winning number.

### 4.3 Cancellation and correction

- A sale can be cancelled only while its draw is `Open` or `Closed` (not after `Settled`), unless an admin override is recorded.
- Cancelling a sale writes an audit entry and excludes it from totals.
- A settled draw cannot be edited; a correction creates an adjustment record rather than mutating history.

---

## 5. Data Model

### 5.1 New tables

```sql
-- Draw header
CREATE TABLE draws (
    id           BIGSERIAL PRIMARY KEY,
    draw_date    DATE NOT NULL,
    draw_time    TIME NOT NULL,
    draw_number  CHAR(2) NULL CHECK (draw_number ~ '^[0-9]{2}$'),
    status       TEXT NOT NULL DEFAULT 'Open'
                 CHECK (status IN ('Open','Closed','Drawn','Settled','Cancelled')),
    created_by   INTEGER NULL REFERENCES users(id),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (draw_date, draw_time)
);

-- Configurable bet types
CREATE TABLE bet_types (
    id                 SERIAL PRIMARY KEY,
    code               TEXT NOT NULL UNIQUE,
    name_en            TEXT NOT NULL,
    name_my            TEXT NOT NULL,
    rule_type          TEXT NOT NULL,
    rule_config        JSONB NOT NULL DEFAULT '{}'::jsonb,
    amount_mode        TEXT NOT NULL
                       CHECK (amount_mode IN ('PerNumber','TotalSplit','Total')),
    payout_mode        TEXT NOT NULL
                       CHECK (payout_mode IN ('PerWinningNumber','TotalIfHit')),
    payout_multiplier  NUMERIC(10,4) NOT NULL,
    is_active          BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order         INTEGER NOT NULL DEFAULT 0,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Fixed number groups (နက္ခတ်, ပါဝါ, future dealer groups)
CREATE TABLE number_groups (
    id          SERIAL PRIMARY KEY,
    code        TEXT NOT NULL UNIQUE,
    name_en     TEXT NOT NULL,
    name_my     TEXT NOT NULL,
    allow_r     BOOLEAN NOT NULL DEFAULT TRUE,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order  INTEGER NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE number_group_members (
    group_id    INTEGER NOT NULL REFERENCES number_groups(id) ON DELETE CASCADE,
    number      CHAR(2) NOT NULL CHECK (number ~ '^[0-9]{2}$'),
    sort_order  INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (group_id, number)
);
```

### 5.2 Sales tables

Extend the existing `sales` header with `draw_id`, `created_by`, `settlement_status`, and `settled_at`. Keep the existing `amount`, `commission`, `commission1`, `status`, and currency fields for compatibility.

```sql
ALTER TABLE sales
    ADD COLUMN IF NOT EXISTS draw_id BIGINT NULL REFERENCES draws(id),
    ADD COLUMN IF NOT EXISTS created_by INTEGER NULL REFERENCES users(id),
    ADD COLUMN IF NOT EXISTS settlement_status TEXT NOT NULL DEFAULT 'Open'
        CHECK (settlement_status IN ('Open','Settled','Cancelled')),
    ADD COLUMN IF NOT EXISTS settled_at TIMESTAMPTZ NULL;
```

Sale items and resolved numbers:

```sql
CREATE TABLE sale_items (
    id                BIGSERIAL PRIMARY KEY,
    sale_id           BIGINT NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    draw_id           BIGINT NOT NULL REFERENCES draws(id),
    bet_type_id       INTEGER NOT NULL REFERENCES bet_types(id),
    number_group_id   INTEGER NULL REFERENCES number_groups(id),
    bet_key           TEXT NOT NULL,              -- e.g. '45', '5', 'NAKKHAT'
    amount            NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    payout_multiplier NUMERIC(10,4) NOT NULL,     -- snapshot
    payout_mode       TEXT NOT NULL,              -- snapshot
    amount_mode       TEXT NOT NULL,              -- snapshot
    status            TEXT NOT NULL DEFAULT 'Pending'
                      CHECK (status IN ('Pending','Won','Lost','Cancelled')),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE sale_item_numbers (
    id              BIGSERIAL PRIMARY KEY,
    sale_item_id    BIGINT NOT NULL REFERENCES sale_items(id) ON DELETE CASCADE,
    number          CHAR(2) NOT NULL CHECK (number ~ '^[0-9]{2}$'),
    stake           NUMERIC(14,2) NOT NULL CHECK (stake >= 0),
    is_reverse      BOOLEAN NOT NULL DEFAULT FALSE,
    status          TEXT NOT NULL DEFAULT 'Pending'
                    CHECK (status IN ('Pending','Won','Lost','Cancelled')),
    payout_amount   NUMERIC(14,2) NOT NULL DEFAULT 0,
    UNIQUE (sale_item_id, number)
);

CREATE TABLE payouts (
    id                   BIGSERIAL PRIMARY KEY,
    draw_id              BIGINT NOT NULL REFERENCES draws(id),
    sale_id              BIGINT NOT NULL REFERENCES sales(id),
    sale_item_id         BIGINT NOT NULL REFERENCES sale_items(id),
    sale_item_number_id  BIGINT NULL REFERENCES sale_item_numbers(id),
    amount               NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    status               TEXT NOT NULL DEFAULT 'Pending'
                         CHECK (status IN ('Pending','Paid','Cancelled')),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    paid_at              TIMESTAMPTZ NULL,
    paid_by              INTEGER NULL REFERENCES users(id)
);
```

### 5.3 Indexes

```sql
CREATE INDEX idx_draws_status_date ON draws(status, draw_date);
CREATE INDEX idx_sale_items_sale ON sale_items(sale_id);
CREATE INDEX idx_sale_items_draw ON sale_items(draw_id);
CREATE INDEX idx_sale_item_numbers_number ON sale_item_numbers(number);
CREATE INDEX idx_payouts_draw_status ON payouts(draw_id, status);
```

### 5.4 Seed data

`bet_types` seeds the rows from §2.1 with `payout_multiplier = 80` or `1.6` and the rules in §2.4.

`number_groups` seeds:

| Code | Name | Members | R |
|---|---|---|---|
| `NAKKHAT` | နက္ခတ် | `07,18,24,35,69` | enabled |
| `POWER_GROUP` | ပါဝါဂဏန်း | `05,16,27,38,49` | enabled |

R pairs are derived as the reverse of each member; they are not duplicated in `number_group_members`.

---

## 6. Services

### 6.1 New services

| Service | Responsibility |
|---|---|
| `IDrawService` | Create/open/close/draw/settle/cancel draws; list open draws. |
| `IBetTypeService` | CRUD for bet types and number groups; validation of rule configs. |
| `INumberResolutionService` | Turn a bet type + selection into a deterministic list of two-digit numbers. |
| `INumberBatchParser` | Format a continuous number string with auto separators; parse/validate comma, space, or newline separated two-digit numbers. |
| `IBatchSaleBuilder` | Build a validated batch preview: merge duplicates, expand R, allocate per-base or total-split amounts, and compute max payout. |
| `ISaleEntryService` | Create a sale with items and resolved numbers inside one DB transaction; compute commission; trigger auto-buy; support batch entries. |
| `IPayoutService` | Settle a draw, create payouts, mark payouts paid. |
| `IQuickAddService` | Build a Quick Add preview (numbers, per-number stake, potential payout) for a group. |

### 6.2 Transaction boundary

`IDatabaseService` currently opens a connection per call. Add a transaction-aware API:

```csharp
Task<T> ExecuteInTransactionAsync<T>(
    Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> action);
```

`SaleEntryService.CreateSaleAsync` uses this API to insert:

1. `sales` header,
2. `sale_items`,
3. `sale_item_numbers`,
4. audit row,
5. auto-buy evaluation.

Any failure rolls back the whole sale.

### 6.3 Auto-buy

Auto-buy runs inside the sale transaction and only when the sale is committed successfully. It reads the existing `auto_buy_rules` table; no background timer is added in M5.

---

## 7. Sales Entry UI

The existing `SalesFilterView` becomes a two-part page:

1. **Entry panel** (top): agent, open draw, bet entry, Quick Add, current sale lines, totals, Save.
2. **Filter/list panel** (bottom): the existing sales history filters and results.

### 7.1 Entry flow

```
Agent: [ select ▼ ]    Draw: [ 2026-11-11 12:00 ▼ ]

Base numbers: [ 45, 39, 23, 19, 40 ]  Amount: [ 1,000 ]  [x] R  [ Add batch ]
Sale type:    [ R | ပါ | ဘရိတ် | ထိပ် | နောက် | စုံစုံ | မမ | စုံမ | ပါဝါ | နက္ခတ် ]
              Number/digit: [ ... ]  Amount: [ ... ]  [ Add ]
Quick Add:    [ နက္ခတ် ▼ ] Amount: [ 1,000 ] [x] R   [ Add ]

Current sale:
  #  Sale type   Numbers           Amount   Per number   Max payout
  1  (base)      45                1,000    1,000        80,000
  2  (base)      39                1,000    1,000        80,000
  3  နက္ခတ်      07,18,24,35,69    1,000    200          16,000

Total stake: 3,000    Commission: ...    [ Save Sale ]
```

The sale-type selector shows the ten approved labels exactly as written. It must not show `Apa`, `Break`, `Top`, `Bottom`, `Direct`, or any other English/transliterated label.

### 7.2 Batch number entry

The Sale Panel must accept a continuous list of two-digit numbers and register the whole batch in one action.

```
Base numbers: [ 45, 39, 23, 19, 40 ]  Amount: [ 1,000 ]  [x] R  [ Add batch ]
```

**Input behaviour**

1. After every two digits, the field automatically inserts a separator: `45` becomes `45, `.
2. Pasting a continuous string is supported:
   - `4539231940` → `45, 39, 23, 19, 40`
   - `45 39 23 19 40` → `45, 39, 23, 19, 40`
   - `45,39,23,19,40` → `45, 39, 23, 19, 40`
3. Backspace and normal editing remain usable; the formatter must not trap the cursor or re-insert a separator the user just deleted.
4. Only digits and separators are accepted. Letters and other symbols are rejected.
5. A trailing separator is allowed while typing but is removed on save.
6. The maximum batch size is configurable (default 100 numbers).

**Parsing and validation**

- Every token must be exactly two digits in `00`–`99`.
- A partial token such as `4` blocks the Add action with a clear message.
- Duplicate numbers are detected. Default behaviour is to warn and merge duplicates; a dealer setting may allow duplicates as separate lines.
- An empty list is invalid.
- The parser is a pure, unit-tested component (`NumberBatchParser`), independent of WinUI.

**Amount behaviour**

The amount field applies to the batch. The default mode is **per base number**:

| Mode | Without R | With R |
|---|---|---|
| `PerBaseNumber` (default) | Each base number gets the full amount. | Each base number's amount is split 50/50 across the base and reverse lines. |
| `TotalSplit` (optional) | The total amount is divided evenly across all base numbers. | The total amount is divided across all resolved lines (base + reverse). |

Examples for `45, 39, 23, 19, 40` with amount `1,000`:

| Mode | R | Total stake | Per-line stake |
|---|---|---|---|
| `PerBaseNumber` | off | `5,000` | `1,000` on each of 5 numbers |
| `PerBaseNumber` | on | `5,000` | `500` on each of 10 lines |
| `TotalSplit` | off | `1,000` | `200` on each of 5 numbers |
| `TotalSplit` | on | `1,000` | `100` on each of 10 lines |

**R behaviour**

- `R` is optional and off by default.
- When `R` is on, every base number gets its reverse added to the same sale item.
- If a reverse number is already present in the batch, the duplicate is merged or skipped according to the duplicate setting; it is never silently double-charged.
- When `R` is off, the batch is saved with the base numbers only, and the save must succeed as long as the amount and numbers are valid.

**Save behaviour**

- One sale header is created.
- One `sale_item` is created per base number, with `bet_type = DIRECT` (R off) or `R` (R on).
- The base number and optional reverse are stored in `sale_item_numbers`.
- The sale header total is the sum of all item amounts.
- Commission and auto-buy run once for the whole sale.
- The whole batch is committed in one transaction; if any item fails, nothing is saved.
- A preview shows the resolved lines, per-line stake, total stake, and maximum payout before saving.

### 7.3 Validation

- Agent is required and must be active.
- Draw is required and must be `Open`.
- Amount is greater than zero and within the configured min/max.
- Number/digit input is valid for the selected sale type.
- A sale type that takes no input (`စုံစုံ`, `မမ`, `စုံမ`, `ပါဝါ`, `နက္ခတ်`) hides the number field.
- The sale-type selector uses the ten approved Burmese labels and no English/transliterated labels.
- Quick Add group must be active; R is disabled if the group has `allow_r = false`.
- Batch tokens must be exactly two digits; a partial token blocks Add.
- Duplicate numbers are merged by default; the dealer setting may allow duplicates as separate lines.
- Save recomputes totals server-side; client totals are display-only.

### 7.4 Number Wisdom admin page

A dealer page (`NumberWisdomView`, navigated from the shell):

- Lists all bet types and edits `payout_multiplier`, `is_active`, and `sort_order`.
- Lists all fixed groups and edits `allow_r`, `is_active`, `sort_order`, and the member list.
- Members accept continuous digits (for example `0718243569`) or comma-separated numbers; duplicates and non-two-digit tokens are rejected.
- Group edits replace members and update the group in one transaction.
- Sale Panel labels (`name_my`) and codes are locked in the UI; bet-type names remain database-owned.
- Changes apply to new sales only; historical sale items keep their multiplier, amount-mode, payout-mode, and match-mode snapshots.

---

## 8. Reporting

Add these views/reports:

| Report | Grouping |
|---|---|
| Sales by draw | Agent, bet type, number group |
| Sales by number | Number, total stake, total payout |
| Winning numbers | Draw result, winning sale items, payout total |
| Payout register | Agent, draw, status, paid date |
| Group performance | နက္ခတ်/ပါဝါ groups, base vs R, stake and payout |

Exports reuse the existing CSV/Excel/PDF services. The four 2D report types are implemented in
`ReportView` through `TwoDReportService` and `TwoDExportService`; `DrawsView` shows the payout
register for the selected draw and supports marking payouts paid.

---

## 9. Configuration Defaults

| Setting | Default | Configurable |
|---|---|---|
| Sale Panel labels | `R`, `ပါ`, `ဘရိတ်`, `ထိပ်`, `နောက်`, `စုံစုံ`, `မမ`, `စုံမ`, `ပါဝါ`, `နက္ခတ်` | Code + UI tests; no English labels |
| Sale-type multiplier | 80x | `bet_types.payout_multiplier` |
| R mode | Split total across numbers | `bet_types.amount_mode` / group |
| Batch amount mode | Per base number | `app_settings` |
| Batch max size | 100 numbers | `app_settings` |
| Batch duplicate handling | Merge + warn | `app_settings` |
| Group Quick Add amount | Total split | `number_groups` + UI |
| Group Quick Add R | Optional, off by default | `number_groups.allow_r` |
| စုံစုံ / မမ sets | 15 unordered pairs each | `bet_types.rule_config` |
| စုံမ set | 25 ordered mixed-parity numbers | `bet_types.rule_config` |
| Rounding | 2 decimals, remainder to first numbers | code + tests |
| Draw cutoff | Dealer-defined per draw | `draws` |

---

## 10. Open Decisions

| # | Decision | Recommendation | Status |
|---|---|---|---|
| D8 | Exact 19-number rule for `ဘရိတ်` | Dealer-configurable; seed as a 19-number digit set distinct from `ပါ` | **Pending** — seeded as `DigitSum` (10 numbers: `0`–`9`); configurable |
| D9 | Orientation of `စုံမ` | Dealer-configurable; seed one ordered 25-number set | **Pending** — seeded as even tens + odd ones (25 ordered numbers); configurable |
| D10 | Group Quick Add amount semantics | Total split across resolved numbers (matches 1,900 → 100 × 19 and R split) | **Proposed** |
| D11 | Group Quick Add R semantics | Add reverse numbers to the same item, split total across all numbers | **Proposed** |
| D12 | Base direct row label | No special-type label; the ten approved labels are the only named sale types | **Proposed** |
| D13 | Optional dealer types (Apuu, Big, Small, etc.) | Not seeded in the default Sale Panel; add only if the dealer enables them | **Proposed** |

---

## 11. Implementation Slices (M5)

| Slice | Deliverable | Tests | Status |
|---|---|---|---|
| M5.1 | Schema, models, seed data, migration | Migration idempotency; seed counts | ✅ Done — PostgreSQL 16 validation, second run idempotent |
| M5.2 | `NumberBatchParser` + `NumberResolutionService` + `PayoutCalculator` | Auto-format, paste, invalid tokens, duplicates, all bet types, R split, rounding, group R | ✅ Done — 24 parser + 33 resolution + 21 payout tests |
| M5.3 | Transactional `SaleEntryService` + auto-buy | Batch sale + items + numbers committed/rolled back together | ✅ Done — 12 amount-calculator tests; PostgreSQL rollback check |
| M5.4 | `DrawService` + `PayoutService` | Draw lifecycle, settlement, cancellation | ✅ Done — settlement verified, double settlement rejected |
| M5.5 | Batch entry + Quick Add + entry panel in `SalesFilterView` | View-model tests; batch auto-format/paste tests; label snapshot test (ten Burmese labels only); Windows UI smoke test | ✅ Code complete — 8 line-factory tests; Windows UI smoke test pending |
| M5.6 | Reports/exports + Number Wisdom page | Report totals; config edit round-trip | ✅ Done — 4 reports × 3 formats; Number Wisdom config round-trip validated |

**Definition of done:** 0 build warnings, all tests pass, the Sale Panel shows only the ten approved Burmese labels, Windows UI smoke test completed on a Windows desktop, and the design doc updated with the final decisions. Current state: 0 warnings, 255 tests passing; Windows UI smoke test remains.
