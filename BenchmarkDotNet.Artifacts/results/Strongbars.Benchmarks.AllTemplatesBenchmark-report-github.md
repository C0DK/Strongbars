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
| **Strongbars**           | **ArticleCard**    |    **113.81 ns** |   **1.766 ns** |   **1.652 ns** |   **1.00** |    **0.02** | **0.0080** |      **-** |     **672 B** |        **1.00** |
| Scriban              | ArticleCard    | 11,195.94 ns | 201.792 ns | 302.033 ns |  98.39 |    2.96 | 0.3815 | 0.0305 |   33123 B |       49.29 |
| &#39;Fluid (Liquid)&#39;     | ArticleCard    |    471.48 ns |   5.840 ns |   5.463 ns |   4.14 |    0.07 | 0.0143 |      - |    1272 B |        1.89 |
| Handlebars.Net       | ArticleCard    |    671.19 ns |  13.383 ns |  12.518 ns |   5.90 |    0.14 | 0.0048 |      - |     432 B |        0.64 |
| &#39;Stubble (Mustache)&#39; | ArticleCard    |  2,104.13 ns |  41.706 ns |  55.677 ns |  18.49 |    0.55 | 0.0496 |      - |    4192 B |        6.24 |
|                      |                |              |            |            |        |         |        |        |           |             |
| **Strongbars**           | **List10Items**    |    **549.14 ns** |  **10.434 ns** |  **11.597 ns** |   **1.00** |    **0.03** | **0.0267** |      **-** |    **2280 B** |        **1.00** |
| Scriban              | List10Items    | 13,354.50 ns | 193.912 ns | 171.898 ns |  24.33 |    0.58 | 0.3815 | 0.0305 |   32802 B |       14.39 |
| &#39;Fluid (Liquid)&#39;     | List10Items    |  1,521.34 ns |  27.091 ns |  25.341 ns |   2.77 |    0.07 | 0.0210 |      - |    1904 B |        0.84 |
| Handlebars.Net       | List10Items    |    855.72 ns |  16.183 ns |  15.894 ns |   1.56 |    0.04 | 0.0038 |      - |     368 B |        0.16 |
| &#39;Stubble (Mustache)&#39; | List10Items    |  3,026.82 ns |  37.856 ns |  35.411 ns |   5.51 |    0.13 | 0.0839 |      - |    7152 B |        3.14 |
|                      |                |              |            |            |        |         |        |        |           |             |
| **Strongbars**           | **SimpleGreeting** |     **64.18 ns** |   **0.885 ns** |   **0.739 ns** |   **1.00** |    **0.02** | **0.0025** |      **-** |     **216 B** |        **1.00** |
| Scriban              | SimpleGreeting | 10,325.13 ns | 191.471 ns | 196.627 ns | 160.90 |    3.48 | 0.3662 | 0.0305 |   31059 B |      143.79 |
| &#39;Fluid (Liquid)&#39;     | SimpleGreeting |    272.95 ns |   4.192 ns |   3.716 ns |   4.25 |    0.07 | 0.0072 |      - |     600 B |        2.78 |
| Handlebars.Net       | SimpleGreeting |    345.87 ns |   4.057 ns |   3.795 ns |   5.39 |    0.08 | 0.0010 |      - |     104 B |        0.48 |
| &#39;Stubble (Mustache)&#39; | SimpleGreeting |    773.85 ns |  15.138 ns |  14.160 ns |  12.06 |    0.25 | 0.0343 |      - |    2888 B |       13.37 |
|                      |                |              |            |            |        |         |        |        |           |             |
| **Strongbars**           | **UserProfile**    |    **154.84 ns** |   **2.191 ns** |   **1.942 ns** |   **1.00** |    **0.02** | **0.0103** |      **-** |     **872 B** |        **1.00** |
| Scriban              | UserProfile    | 11,077.02 ns | 125.138 ns | 117.054 ns |  71.55 |    1.13 | 0.3967 | 0.0305 |   33252 B |       38.13 |
| &#39;Fluid (Liquid)&#39;     | UserProfile    |    583.17 ns |   8.687 ns |   7.254 ns |   3.77 |    0.06 | 0.0172 |      - |    1440 B |        1.65 |
| Handlebars.Net       | UserProfile    |    801.48 ns |  12.249 ns |  10.858 ns |   5.18 |    0.09 | 0.0057 |      - |     536 B |        0.61 |
| &#39;Stubble (Mustache)&#39; | UserProfile    |  2,008.19 ns |  33.935 ns |  31.743 ns |  12.97 |    0.25 | 0.0496 |      - |    4368 B |        5.01 |
