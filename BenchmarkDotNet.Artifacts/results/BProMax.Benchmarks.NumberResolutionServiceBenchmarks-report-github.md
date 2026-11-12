```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9711)
Unknown processor
.NET SDK 9.0.321
  [Host]     : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2
  Job-SQTCOV : .NET 9.0.23 (9.0.2326.52216), X64 RyuJIT AVX2

IterationCount=3  WarmupCount=1  

```
| Method                | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| Exact                 |    32.13 ns |  23.75 ns |  1.302 ns |  1.00 |    0.05 |     176 B |        1.00 |
| ExactAndReverse       |   144.16 ns | 111.72 ns |  6.124 ns |  4.49 |    0.23 |     536 B |        3.05 |
| DigitInEitherPosition | 1,994.05 ns | 646.70 ns | 35.448 ns | 62.13 |    2.34 |    3944 B |       22.41 |
| DigitInPosition       |   385.85 ns | 124.55 ns |  6.827 ns | 12.02 |    0.45 |     760 B |        4.32 |
| DigitSum              |   493.84 ns |  53.59 ns |  2.937 ns | 15.39 |    0.54 |     704 B |        4.00 |
| FixedGroup            |   305.25 ns |  42.42 ns |  2.325 ns |  9.51 |    0.33 |     944 B |        5.36 |
| Parity                | 1,263.08 ns |  87.29 ns |  4.785 ns | 39.35 |    1.36 |    1896 B |       10.77 |
| Doubles               |   243.50 ns |  33.82 ns |  1.854 ns |  7.59 |    0.27 |     560 B |        3.18 |
| Range                 |   407.89 ns |  27.70 ns |  1.518 ns | 12.71 |    0.44 |     808 B |        4.59 |
