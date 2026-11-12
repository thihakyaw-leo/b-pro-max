```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 9.0.321
  [Host]     : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2
  Job-SQTCOV : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2

IterationCount=3  WarmupCount=1  

```
| Method                           | Mean      | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------- |----------:|-----------:|---------:|------:|--------:|----------:|------------:|
| CalculateCommission              |  35.68 ns |   5.367 ns | 0.294 ns |  1.00 |    0.01 |         - |          NA |
| CalculateCommission1             |  35.51 ns |   1.237 ns | 0.068 ns |  1.00 |    0.01 |         - |          NA |
| CalculateNetCommission           |  81.39 ns |   2.624 ns | 0.144 ns |  2.28 |    0.02 |         - |          NA |
| CalculateTotals                  | 643.47 ns | 148.425 ns | 8.136 ns | 18.04 |    0.24 |     816 B |          NA |
| ConvertCurrency                  |  44.34 ns |   4.931 ns | 0.270 ns |  1.24 |    0.01 |         - |          NA |
| CalculateCommissionInCurrency    |  76.85 ns |   4.409 ns | 0.242 ns |  2.15 |    0.02 |         - |          NA |
| CalculateNetCommissionInCurrency | 163.61 ns |  20.262 ns | 1.111 ns |  4.59 |    0.04 |         - |          NA |
