# Plan — All Design Upgrade (b-pro-max WinUI 3)

**Repository:** `/workspace/app` (branch `kilo/open-arrow-jim`)
**Plan file:** `.kilo/plans/1794612200000-all-design-upgrade.md`
**Status:** Implementation-ready

---

## 1. Current State Assessment

All 16 WinUI 3 views are functionally complete but lack design consistency:

| View | Layout Pattern | AutomationId Root | StatusMessage | Notes |
|------|---------------|-------------------|---------------|-------|
| AgentListView | StackPanel | ✅ | ✅ | Odd indentation (8-space xmlns) |
| AgentDetailView | StackPanel | ✅ | ✅ | Label width 120, input 300 |
| AgentSearchView | StackPanel | ✅ | ✅ | Mirrors AgentListView |
| AutoBuyView | StackPanel | ✅ | ✅ | 4 action buttons |
| BackupView | StackPanel | ✅ | ✅ | Browse + 2 action buttons |
| CompactDatabaseView | StackPanel | ✅ | ✅ | Single action button |
| CreateDatabaseView | StackPanel | ✅ | ✅ | Single input + action |
| DrawsView | StackPanel | ✅ | ✅ | Draw lifecycle + payouts |
| LoginView | Centered Grid | ❌ | ✅ | MaxWidth=400, Padding=24 |
| MaintenanceView | StackPanel | ✅ | ✅ | Description text + 2 buttons |
| NumberWisdomView | Pivot | ✅ | ✅ | Two tabs (Bet Types, Groups) |
| RegionalSettingsView | StackPanel | ✅ | ✅ | New (FIX-15) |
| RegisterView | Centered Grid | ❌ | ✅ | MaxWidth=400, Padding=24 |
| ReportView | StackPanel | ✅ | ✅ | Report type + format + export |
| SalesFilterView | StackPanel | ✅ | ✅ | Complex entry panel + history |
| SoundSettingsView | StackPanel | ✅ | ✅ | Checkbox + slider + 3 buttons |
| TimeLockView | StackPanel | ✅ | ✅ | Checkbox + 2 inputs + 2 buttons |

**Design issues:**
1. **No design token system** — No custom styles for colors, spacing, typography beyond built-in ThemeResource/StaticResource
2. **Inconsistent layout patterns** — LoginView/RegisterView use centered Grid (MaxWidth=400); all others use left-aligned StackPanels
3. **Inconsistent spacing** — StackPanel Spacing varies (4, 8, 12); Margin values inconsistent across views
4. **Inconsistent AutomationId** — LoginView/RegisterView missing root Page AutomationId; some controls missing IDs
5. **Inconsistent StatusMessage** — Different Grid.Row placement; different Foreground brush references
6. **Inconsistent button styling** — Login/Register buttons have Height=40/HorizontalAlignment=Stretch; others use default sizing
7. **Inconsistent input widths** — TextBox widths range 100–400 with no pattern
8. **XAML formatting** — AgentListView.xaml has 8-space xmlns indentation; mixed tab/space usage
9. **MainWindow nav** — `IsSettingsVisible="False"` but settings pages exist; no settings group/separator; `NavRegionalSettings` added but ungrouped
10. **Missing polish** — No hover effects, no focus visuals, no empty states, no consistent message styling

---

## 2. Decisions

| Decision | Resolution |
|----------|------------|
| Design system | Create shared XAML styles in `App.xaml` (colors, spacing, typography, button/input styles) referenced via `StaticResource` |
| Layout pattern | Standardize all views on left-aligned Grid + StackPanel pattern (login/register keep centered layout but align with design tokens) |
| Spacing scale | 4px (tight), 8px (normal), 12px (section), 24px (page margin) |
| Label width | 120px for label/input pairs; 140px for wider labels |
| Input width | 300px default; 150px for numeric; 400px for paths |
| Button height | 40px default for primary actions |
| AutomationId | Every interactive control gets `ViewName+ControlName` pattern; root Page gets `ViewName` |
| StatusMessage | Always `Grid.Row` of last row, `Margin="12"`, `Foreground="{ThemeResource SystemControlSuccessTextForegroundBrush}"` |
| Font integration | Apply regional FontFamily to `NavView` + `ContentFrame` via `MainWindow.ApplyFontFamily()` (FIX-16 from previous plan) |
| PDF fonts | `ReportService` embeds Pyidaungsu via `XPrivateFontCollection` when available (FIX-16) |
| XAML formatting | 4-space indentation, no leading whitespace on xmlns attributes |

---

## 3. Task List

### Phase 1 — Design token system (App.xaml)

1. **Create `App.xaml` resource dictionary** with shared styles:
   - `PageTitleStyle` (based on TitleTextBlockStyle)
   - `PageSubtitleStyle` (based on SubtitleTextBlockStyle)
   - `FormLabelStyle` (TextBlock, label width, vertical center)
   - `FormInputStyle` (TextBox, default width)
   - `PrimaryButtonStyle` (Height=40, HorizontalAlignment=Stretch)
   - `SecondaryButtonStyle` (default sizing)
   - `StatusMessageStyle` (TextBlock, margin, foreground)
   - `SectionPanelStyle` (StackPanel, margin, spacing)
   - `Color resources`: `PrimaryColor`, `SurfaceColor`, `ErrorColor`, `SuccessColor`

2. **Update `App.xaml.cs`** — Ensure resources are loaded; no code changes needed if styles are in XAML resources

### Phase 2 — View-by-view design standardization

3. **AgentListView.xaml** — Fix indentation; apply design tokens; ensure consistent spacing
4. **AgentDetailView.xaml** — Apply design tokens; standardize label/input widths
5. **AgentSearchView.xaml** — Apply design tokens; mirror AgentListView patterns
6. **AutoBuyView.xaml** — Apply design tokens; standardize button group
7. **BackupView.xaml** — Apply design tokens; standardize browse + action layout
8. **CompactDatabaseView.xaml** — Apply design tokens; simplify
9. **CreateDatabaseView.xaml** — Apply design tokens; standardize input + action
10. **DrawsView.xaml** — Apply design tokens; standardize draw lifecycle + payout layout
11. **LoginView.xaml** — Add AutomationId to root Page; align spacing with design tokens
12. **MaintenanceView.xaml** — Apply design tokens; standardize description + buttons
13. **NumberWisdomView.xaml** — Apply design tokens to both Pivot tabs
14. **RegisterView.xaml** — Add AutomationId to root Page; align spacing with design tokens
15. **ReportView.xaml** — Apply design tokens; standardize report type + format + export
16. **SalesFilterView.xaml** — Apply design tokens; standardize entry panel + history
17. **SoundSettingsView.xaml** — Apply design tokens; standardize checkbox + slider + buttons
18. **TimeLockView.xaml** — Apply design tokens; standardize inputs + buttons

### Phase 3 — AutomationId audit completion (FIX-17)

19. **Audit every interactive control** in all 16 views for stable `AutomationProperties.AutomationId`
20. **Add missing AutomationId** to any control lacking it (buttons, inputs, checkboxes, list items)
21. **Ensure consistent naming pattern**: `ViewName+ControlName` (e.g., `AgentListSearchBox`)
22. **Verify root Page AutomationId** on all views (LoginView, RegisterView currently missing)

### Phase 4 — Regional font integration (FIX-16)

23. **Create `src/Services/IFontAvailabilityService.cs`** — Interface: `bool IsPyidaungsuAvailable`, `string? PyidaungsuPath`
24. **Create `src/Services/FontAvailabilityService.cs`** — File system check for `C:\Windows\Fonts\Pyidaungsu.ttf` and `%LOCALAPPDATA%\Microsoft\Windows\Fonts\Pyidaungsu.ttf`
25. **Register in `App.xaml.cs`** — `services.AddSingleton<IFontAvailabilityService, FontAvailabilityService>()`
26. **Wire into `App.OnLaunched`** — Log warning if Pyidaungsu missing
27. **Wire into `MainWindow`** — Show in-app warning banner if font missing
28. **Wire into `RegionalSettingsView`** — Show font availability status; warn/disable apply if missing
29. **Update `MainWindow.ApplyFontFamily()`** — Apply to `NavView` + `ContentFrame`
30. **Update `ReportService.cs`** — Inject `IFontAvailabilityService`; embed Pyidaungsu via `XPrivateFontCollection`; fall back to `Verdana`

### Phase 5 — MainWindow navigation polish

31. **Add settings group separator** in `MainWindow.xaml` NavigationView
32. **Group settings nav items** (NavSound, NavTimeLock, NavRegionalSettings) under settings
33. **Ensure `IsSettingsVisible`** consistent with settings group presence

### Phase 6 — Validation (Windows only)

```
dotnet format --verify-no-changes
dotnet build src\b-pro-max.csproj -c Release --no-incremental
dotnet build tests\b-pro-max.tests.csproj -c Release --no-incremental
dotnet test tests\b-pro-max.tests.csproj --no-build
```

Manual UI smoke test on Windows:
- Launch → all views render with consistent styling
- All 10 Burmese labels display correctly in Sale Panel
- Regional Settings → apply Myanmar preset → font applies app-wide
- All interactive controls have AutomationId
- No XAML compilation errors

---

## 4. Risks & Constraints

- **No .NET SDK on Linux** — Build, format, and test cannot be verified in this container
- **WinUI 3 XAML compilation is disabled** — Controls resolved at runtime via `FindChildByName<T>()`; any renamed control must update code-behind references
- **`TreatWarningsAsErrors=true`** — Warnings fail the build; unused styles/resources will cause warnings
- **LF line endings** — `.gitattributes` + `.editorconfig` enforce `eol=lf`
- **Style scope** — Styles defined in `App.xaml` resources apply app-wide; ensure no unintended overrides
- **`Window` has no `FontFamily`** — Font must be applied per-element (`ContentFrame`, `NavView`)

---

## 6. Open Questions

None. All design decisions are resolved. Ready for implementation.

---
