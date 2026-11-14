# Plan — Myanmar 2D Lottery POS UI Features

**Repository:** `/workspace/app` (branch `kilo/open-arrow-jim`)
**Plan file:** `.kilo/plans/1794601499880-myanmar-2d-lottery-pos-ui.md`
**Status:** Implementation-ready

---

## 1. Current State

The Myanmar 2D lottery UI is code-complete per `2d-system-design.md` and `todos.md` (Phase 7 / M5). All ten sale types (`R`, `ပါ`, `ဘရိတ်`, `ထိပ်`, `နောက်`, `စုံစုံ`, `မမ`, `စုံမ`, `ပါဝါ`, `နက္ခတ်`), batch entry with auto-formatting, Quick Add (`နက္ခတ်`, `ပါဝါ` with optional R), draw lifecycle, settlement, Number Wisdom admin, and 2D reports are implemented across five WinUI 3 views:

| View | File |
|------|------|
| SalesFilterView (Sale Panel) | `src/Views/SalesFilterView.xaml` + `.cs` |
| DrawsView | `src/Views/DrawsView.xaml` + `.cs` |
| NumberWisdomView | `src/Views/NumberWisdomView.xaml` + `.cs` |
| ReportView | `src/Views/ReportView.xaml` + `.cs` |
| RegionalSettingsView | `src/Views/RegionalSettingsView.xaml` + `.cs` |

246 unit tests pass, 0 build warnings (on Windows). Windows UI smoke test is pending.

**Scope constraint:** .NET SDK is not available in this Linux container; `dotnet build`/`test`/format cannot run here. All verification must happen on a Windows machine with .NET SDK 9.0.100+.

**Font attachments:** Four TTF files provided at `/tmp/attachments/...` (Pyidaungsu and related Myanmar fonts) — reference only for manual Windows testing; not committed to the repo (fonts are Windows system assets installed by the user).

---

## 2. Decisions (confirmed)

| Decision | Resolution |
|----------|------------|
| Scope | Phases 1–3 in scope (verification audit + font availability detection + app-wide font application + PDF font embedding). Phase 4 (auto-buy visibility, bulk payout payment) is out of scope. |
| Font detection mechanism | File system check: `C:\Windows\Fonts\Pyidaungsu.ttf` and per-user font dir (`%LOCALAPPDATA%\Microsoft\Windows\Fonts\Pyidaungsu.ttf`). Return `bool IsAvailable` + `string? FontPath`. |
| Font availability service | New interface `IFontAvailabilityService` + implementation `FontAvailabilityService`, registered as singleton in `App.xaml.cs` DI. |
| App startup font check | `App.OnLaunched` resolves `IFontAvailabilityService`; if unavailable, logs a warning. `MainWindow` constructor checks availability and shows an in-app warning banner with installation instructions if Pyidaungsu is missing. |
| App-wide font application | `MainWindow.ApplyFontFamily(string)` sets `FontFamily` on `ContentFrame` and `NavView`. Called from `RegionalSettingsView.SaveButton_Click` after settings persist. Fallback: `Myanmar Text` → `Segoe UI` if Pyidaungsu unavailable. |
| PDF font embedding | `ReportService` injects `IFontAvailabilityService`. When Pyidaungsu is available, loads it via `PdfSharpCore.Drawing.XPrivateFontCollection.Add(File.ReadAllBytes(path))` and uses `new XFont("Pyidaungsu", ...)` for Myanmar text. Falls back to `Verdana` when unavailable. |
| Font attachments | Four TTF files at `/tmp/attachments/...` — reference only; not committed to repo. |

---

## 3. Task List

### Phase 1 — UI verification audit (requires Windows desktop)

1. **Label audit** — Confirm `SaleTypeCombo` in `SalesFilterView` binds `NameMy` and displays exactly the 10 approved Burmese labels (`R`, `ပါ`, `ဘရိတ်`, `ထိပ်`, `နောက်`, `စုံစုံ`, `မမ`, `စုံမ`, `ပါဝါ`, `နက္ခတ်`); no English labels (`Apa`, `Break`, `Top`, `Bottom`, `Direct`).
2. **Batch auto-format audit** — Verify `BatchNumbersBox_TextChanged` inserts separators after every 2 digits, handles paste, rejects non-digits, does not trap cursor.
3. **Quick Add audit** — Verify `နက္ခတ်` and `ပါဝါ` resolve correct base+reverse numbers with correct per-number stakes.
4. **Draw lifecycle audit** — Verify Open → Closed → Drawn → Settled transitions and payout mark-paid flow.
5. **Report audit** — Verify 4 × 2D reports generate PDF/Excel/CSV with correct totals.

### Phase 2 — Font availability detection + PDF font embedding (FIX-16)

6. **Create `src/Services/IFontAvailabilityService.cs`** — Interface:
   ```csharp
   public interface IFontAvailabilityService {
       bool IsPyidaungsuAvailable { get; }
       string? PyidaungsuPath { get; }
   }
   ```
7. **Create `src/Services/FontAvailabilityService.cs`** — Implementation checking `C:\Windows\Fonts\Pyidaungsu.ttf` and `%LOCALAPPDATA%\Microsoft\Windows\Fonts\Pyidaungsu.ttf`; caches result.
8. **Register in `App.xaml.cs`** — `services.AddSingleton<IFontAvailabilityService, FontAvailabilityService>();`
9. **Wire into `App.OnLaunched`** — Resolve `IFontAvailabilityService`; if `!IsPyidaungsuAvailable`, log warning via `ILogger<App>`.
10. **Wire into `MainWindow`** — In constructor, check `IFontAvailabilityService`; if unavailable, show an in-app warning banner (TextBlock near top of shell) with Pyidaungsu installation instructions.
11. **Wire into `RegionalSettingsView`** — Display font availability status in UI; show warning if "Apply Pyidaungsu Font" is clicked but Pyidaungsu is not installed.
12. **Update `ReportService.cs`** — Inject `IFontAvailabilityService`; when available, load Pyidaungsu via `XPrivateFontCollection` and use for report text; fall back to `Verdana`.

### Phase 3 — App-wide font application

13. **Update `MainWindow.ApplyFontFamily(string)`** — Apply font to both `ContentFrame` and `NavView` (not just `ContentFrame`).
14. **Update `RegionalSettingsView.SaveButton_Click`** — After `SaveToStoreAsync()`, call `MainWindow.Instance?.ApplyFontFamily(_regionalSettings.FontFamily)`.
15. **Update `RegionalSettingsView.ApplyFontButton_Click`** — After applying Pyidaungsu font, call `ApplyFontFamily` immediately so font takes effect without requiring Save.

---

## 4. Risks & Constraints

- **No .NET SDK on Linux** — Build, format, and test cannot be verified in this container. All code changes must be validated on Windows with .NET SDK 9.0.100+.
- **WinUI 3 XAML compilation is disabled** — Controls resolved at runtime via `FindChildByName<T>()`; any new XAML control needs matching `x:Name` in code-behind.
- **`TreatWarningsAsErrors=true`** — Warnings fail the build; fix warnings rather than suppress.
- **LF line endings** — Enforced by `.gitattributes` + `.editorconfig`; `dotnet format --verify-no-changes` fails with CRLF.
- **`Window` has no `FontFamily` property in WinUI 3** — Font must be applied per-element (`ContentFrame`, `NavView`), not on the window itself.
- **PdfSharpCore font embedding** — `XPrivateFontCollection` must be populated before any `XFont` using that family is created; otherwise the font falls back silently.

---

## 5. Validation Plan (run on Windows)

```
dotnet format --verify-no-changes
dotnet build src\b-pro-max.csproj -c Release --no-incremental
dotnet build tests\b-pro-max.tests.csproj -c Release --no-incremental
dotnet test tests\b-pro-max.tests.csproj --no-build
```

Manual UI smoke test on Windows:
- Launch → login → Sales → verify all 10 Burmese labels
- Enter batch numbers → verify auto-formatting and R-split math
- Quick Add နက္ခတ် / ပါဝါ (+R) → verify correct numbers and stakes
- Create draw → set result → settle → verify payouts
- Generate 2D reports → verify PDF/Excel/CSV output
- Regional Settings → apply Myanmar preset → verify Pyidaungsu font applied app-wide (NavView + ContentFrame)
- Start without Pyidaungsu font → verify warning banner appears
- Install Pyidaungsu (one of the four attached TTFs) → restart app → verify font applies and reports render Myanmar text

---

## 6. Open Questions

None. Scope is confirmed: Phases 1–3 in scope; Phase 4 out of scope.

---
