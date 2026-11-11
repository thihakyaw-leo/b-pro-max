# AGENTS.md — b-pro-max .NET Quality Baseline

## Repo-native baseline

| Item | Setting |
| --- | --- |
| Repo-root `.editorconfig` | Default source of truth for per-rule severity |
| `Directory.Build.props` | `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true`, `WarningsNotAsErrors=CA1707` |
| Analyzer pack | `Meziantou.Analyzer` 3.0.322 (repo-root Directory.Build.props) |
| Promoted-to-error rules | CA1305, CA1416, CA1848, CA1852, CA1862, Security category (CA3xxx/CA5xxx) |
| Phase 4 exception | `WarningsNotAsErrors=CA1707` (tests: 148 identifier-naming occurrences) |
| Test runner | VSTest via xunit (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`) |
| Coverage | `coverlet.collector` 6.0.2 |
| Target framework | `net9.0-windows10.0.19041.0`, Platform=x64, RuntimeIdentifiers=win-x64 |

## Quality pass commands (run in order)

From repo root `F:\b\b-pro-max`:

```cmd
# 1. Format check (verify no uncommitted formatting changes)
dotnet format --verify-no-changes

# 2. Build the whole solution (src + tests + e2e; 0 errors, 0 warnings expected)
dotnet build b-pro-max.sln -c Release

# 3. Build src only (0 errors, 0 warnings expected)
dotnet build src\b-pro-max.csproj

# 4. Build tests (0 errors, 0 warnings expected)
dotnet build tests\b-pro-max.tests.csproj

# 5. Run unit tests (VSTest/xunit)
dotnet test tests\b-pro-max.tests.csproj --no-build

# 6. Run tests with coverage
dotnet test tests\b-pro-max.tests.csproj --collect:"XPlat Code Coverage" --results-directory:.\coverage

# 7. Run e2e tests (separate project)
dotnet test tests\e2e\b-pro-max.e2e.csproj --no-build
```

## Rule ownership

- Formatting/style → `.editorconfig` + `dotnet format`
- Correctness/static analysis → SDK analyzers + Meziantou.Analyzer + `TreatWarningsAsErrors=true`
- Coverage → coverlet.collector, no coverage threshold gate currently enforced
- Architecture → no architecture tests configured yet
- Security scanning → no external security scanner configured yet
- Mutation testing → not configured

## Exceptions

`WarningsNotAsErrors=CA1707` is the only explicit exception — tests have 148 CA1707 (identifier naming) occurrences. It is reported as a warning but does not fail the build.

## Analyzer pack decisions

- **Meziantou.Analyzer 3.0.322 — adopted.** Broad design/usage/security/performance coverage; severities owned by the repo-root `.editorconfig`.
- **StyleCop.Analyzers — evaluated and not adopted (Phase 5 audit closed).** Rationale: StyleCop's rule set largely duplicates what the SDK analyzers (`AnalysisMode=Recommended` + `EnforceCodeStyleInBuild=true`) and `dotnet format` already enforce, while adding a large new warning surface that `TreatWarningsAsErrors=true` would turn into a wall of build breaks (documentation headers, file ordering, `this.` prefixes, and similar conventions) that carry no correctness value for this codebase. Revisit only if the team explicitly wants a single opinionated style owner and accepts a dedicated suppression/cleanup phase.

## Notes

- `b-pro-max.sln` parses and builds cleanly; the solution contains `src`, `tests`, and `tests/e2e`. The earlier MSB5014 came from a malformed `Format Version = 12.00` header, which is now `Format Version 12.00`.
- The root `benchmarks` entrypoint and `src/BenchmarksFolder` are local-only scratch (untracked, `.gitignore`d). The `BProMax.Benchmarks` namespace they reference no longer exists; the benchmark suite needs to be re-created as a proper `src/Benchmarks/` project before it can be tracked.
- WinRT activation (`REGDB_E_CLASSNOTREG`) is unavailable in container — app launch requires Windows desktop with WinRT runtime registered; this is an environment limitation, not a code issue.
- Transient CS2012 file-lock errors from Microsoft Defender Antivirus can occur during builds; retry usually succeeds.
- Temp build log files should be cleaned up after each validation.
