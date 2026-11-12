```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 9.0.321
  [Host]     : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2
  Job-SQTCOV : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2

IterationCount=3  WarmupCount=1  

```
| Method                    | Mean      | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
| PerWinningNumberExact     |  94.17 ns | 56.60 ns | 3.102 ns |  1.00 |    0.04 |     264 B |        1.00 |
| PerWinningNumberUnordered | 157.35 ns | 18.43 ns | 1.010 ns |  1.67 |    0.05 |     456 B |        1.73 |
| TotalIfHit                |  89.09 ns | 25.81 ns | 1.415 ns |  0.95 |    0.03 |     264 B |        1.00 |
