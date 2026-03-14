```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.10GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 8.0.125
  [Host]   : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  .NET 8.0 : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method               | ScenarioName   | Mean         | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |--------------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| **Strongbars**           | **ArticleCard**    |    **110.82 ns** |   **2.267 ns** |   **2.121 ns** |   **1.00** |    **0.03** | **0.0080** |      **-** |     **672 B** |        **1.00** |
| Scriban              | ArticleCard    | 10,831.32 ns | 200.823 ns | 187.850 ns |  97.77 |    2.43 | 0.3815 | 0.0305 |   33123 B |       49.29 |
| &#39;Fluid (Liquid)&#39;     | ArticleCard    |    462.84 ns |   3.393 ns |   3.173 ns |   4.18 |    0.08 | 0.0148 |      - |    1272 B |        1.89 |
| Handlebars.Net       | ArticleCard    |    624.94 ns |   6.930 ns |   5.786 ns |   5.64 |    0.12 | 0.0048 |      - |     432 B |        0.64 |
| &#39;Stubble (Mustache)&#39; | ArticleCard    |  2,079.44 ns |  31.543 ns |  27.962 ns |  18.77 |    0.42 | 0.0496 |      - |    4192 B |        6.24 |
|                      |                |              |            |            |        |         |        |        |           |             |
| **Strongbars**           | **List10Items**    |    **544.13 ns** |   **8.640 ns** |   **8.082 ns** |   **1.00** |    **0.02** | **0.0267** |      **-** |    **2280 B** |        **1.00** |
| Scriban              | List10Items    | 12,996.34 ns | 125.566 ns | 111.311 ns |  23.89 |    0.39 | 0.3815 | 0.0305 |   32802 B |       14.39 |
| &#39;Fluid (Liquid)&#39;     | List10Items    |  1,529.38 ns |  25.382 ns |  22.501 ns |   2.81 |    0.06 | 0.0210 |      - |    1904 B |        0.84 |
| Handlebars.Net       | List10Items    |    879.99 ns |  10.806 ns |  10.108 ns |   1.62 |    0.03 | 0.0038 |      - |     368 B |        0.16 |
| &#39;Stubble (Mustache)&#39; | List10Items    |  3,038.63 ns |  38.881 ns |  34.467 ns |   5.59 |    0.10 | 0.0839 |      - |    7152 B |        3.14 |
|                      |                |              |            |            |        |         |        |        |           |             |
| **Strongbars**           | **SimpleGreeting** |     **58.11 ns** |   **0.972 ns** |   **0.862 ns** |   **1.00** |    **0.02** | **0.0025** |      **-** |     **216 B** |        **1.00** |
| Scriban              | SimpleGreeting | 10,230.95 ns | 198.893 ns | 221.070 ns | 176.10 |    4.49 | 0.3662 | 0.0305 |   31059 B |      143.79 |
| &#39;Fluid (Liquid)&#39;     | SimpleGreeting |    268.10 ns |   4.288 ns |   3.802 ns |   4.61 |    0.09 | 0.0072 |      - |     600 B |        2.78 |
| Handlebars.Net       | SimpleGreeting |    340.18 ns |   4.523 ns |   4.231 ns |   5.86 |    0.11 | 0.0010 |      - |     104 B |        0.48 |
| &#39;Stubble (Mustache)&#39; | SimpleGreeting |    751.51 ns |   6.475 ns |   6.057 ns |  12.94 |    0.21 | 0.0343 |      - |    2888 B |       13.37 |
|                      |                |              |            |            |        |         |        |        |           |             |
| **Strongbars**           | **UserProfile**    |    **148.05 ns** |   **1.854 ns** |   **1.548 ns** |   **1.00** |    **0.01** | **0.0103** |      **-** |     **872 B** |        **1.00** |
| Scriban              | UserProfile    | 11,155.06 ns | 206.326 ns | 192.997 ns |  75.35 |    1.47 | 0.3967 | 0.0305 |   33252 B |       38.13 |
| &#39;Fluid (Liquid)&#39;     | UserProfile    |    590.89 ns |   9.569 ns |   8.482 ns |   3.99 |    0.07 | 0.0172 |      - |    1440 B |        1.65 |
| Handlebars.Net       | UserProfile    |    808.23 ns |  12.338 ns |  11.541 ns |   5.46 |    0.09 | 0.0057 |      - |     536 B |        0.61 |
| &#39;Stubble (Mustache)&#39; | UserProfile    |  1,989.06 ns |  25.464 ns |  23.819 ns |  13.44 |    0.21 | 0.0496 |      - |    4368 B |        5.01 |
