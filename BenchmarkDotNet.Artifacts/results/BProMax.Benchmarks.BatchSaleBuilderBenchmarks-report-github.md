```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 9.0.321
  [Host]     : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2
  Job-SQTCOV : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2

IterationCount=3  WarmupCount=1  

```
| Method         | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |-----------:|-----------:|---------:|------:|--------:|----------:|------------:|
| PerBaseNumber  | 1,527.5 ns |   615.4 ns | 33.73 ns |  1.00 |    0.03 |   3.73 KB |        1.00 |
| TotalSplit     | 1,631.0 ns | 1,066.2 ns | 58.44 ns |  1.07 |    0.04 |   3.66 KB |        0.98 |
| WithDuplicates |   999.2 ns |   834.1 ns | 45.72 ns |  0.65 |    0.03 |   2.51 KB |        0.67 |
