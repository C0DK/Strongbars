```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.10GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 8.0.125
  [Host]   : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  .NET 8.0 : .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method               | ScenarioName   | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |--------------- |------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| **Strongbars**           | **ArticleCard**    |  **1,115.0 ns** |  **19.11 ns** |  **17.88 ns** |  **1.00** |    **0.02** | **0.0343** |      **-** |    **2992 B** |        **1.00** |
| Scriban              | ArticleCard    | 11,472.1 ns | 218.84 ns | 204.70 ns | 10.29 |    0.24 | 0.3815 | 0.0305 |   33123 B |       11.07 |
| &#39;Fluid (Liquid)&#39;     | ArticleCard    |    490.9 ns |   9.70 ns |   8.60 ns |  0.44 |    0.01 | 0.0143 |      - |    1272 B |        0.43 |
| Handlebars.Net       | ArticleCard    |    657.7 ns |  12.39 ns |  10.98 ns |  0.59 |    0.01 | 0.0048 |      - |     432 B |        0.14 |
| &#39;Stubble (Mustache)&#39; | ArticleCard    |  2,028.9 ns |  30.48 ns |  27.02 ns |  1.82 |    0.04 | 0.0496 |      - |    4192 B |        1.40 |
|                      |                |             |           |           |       |         |        |        |           |             |
| **Strongbars**           | **List10Items**    |  **4,551.6 ns** |  **60.55 ns** |  **56.64 ns** |  **1.00** |    **0.02** | **0.0992** |      **-** |    **8760 B** |        **1.00** |
| Scriban              | List10Items    | 13,932.4 ns | 246.81 ns | 230.86 ns |  3.06 |    0.06 | 0.3815 | 0.0305 |   32802 B |        3.74 |
| &#39;Fluid (Liquid)&#39;     | List10Items    |  1,503.5 ns |  19.26 ns |  18.01 ns |  0.33 |    0.01 | 0.0210 |      - |    1904 B |        0.22 |
| Handlebars.Net       | List10Items    |    877.2 ns |  17.19 ns |  16.89 ns |  0.19 |    0.00 | 0.0038 |      - |     368 B |        0.04 |
| &#39;Stubble (Mustache)&#39; | List10Items    |  3,068.2 ns |  58.14 ns |  59.70 ns |  0.67 |    0.02 | 0.0839 |      - |    7152 B |        0.82 |
|                      |                |             |           |           |       |         |        |        |           |             |
| **Strongbars**           | **SimpleGreeting** |    **667.3 ns** |   **8.25 ns** |   **7.32 ns** |  **1.00** |    **0.01** | **0.0162** |      **-** |    **1400 B** |        **1.00** |
| Scriban              | SimpleGreeting | 10,569.3 ns | 125.39 ns | 117.29 ns | 15.84 |    0.24 | 0.3662 | 0.0305 |   31059 B |       22.18 |
| &#39;Fluid (Liquid)&#39;     | SimpleGreeting |    274.9 ns |   2.37 ns |   2.22 ns |  0.41 |    0.01 | 0.0072 |      - |     600 B |        0.43 |
| Handlebars.Net       | SimpleGreeting |    355.0 ns |   6.96 ns |   8.01 ns |  0.53 |    0.01 | 0.0010 |      - |     104 B |        0.07 |
| &#39;Stubble (Mustache)&#39; | SimpleGreeting |    780.1 ns |  11.46 ns |  10.72 ns |  1.17 |    0.02 | 0.0343 |      - |    2888 B |        2.06 |
|                      |                |             |           |           |       |         |        |        |           |             |
| **Strongbars**           | **UserProfile**    |  **1,620.6 ns** |  **31.49 ns** |  **49.03 ns** |  **1.00** |    **0.04** | **0.0515** |      **-** |    **4336 B** |        **1.00** |
| Scriban              | UserProfile    | 11,466.4 ns | 174.30 ns | 163.04 ns |  7.08 |    0.23 | 0.3967 | 0.0305 |   33252 B |        7.67 |
| &#39;Fluid (Liquid)&#39;     | UserProfile    |    587.2 ns |   8.62 ns |   7.65 ns |  0.36 |    0.01 | 0.0172 |      - |    1440 B |        0.33 |
| Handlebars.Net       | UserProfile    |    798.1 ns |   9.67 ns |   8.57 ns |  0.49 |    0.02 | 0.0057 |      - |     536 B |        0.12 |
| &#39;Stubble (Mustache)&#39; | UserProfile    |  2,074.4 ns |  36.36 ns |  34.01 ns |  1.28 |    0.04 | 0.0496 |      - |    4368 B |        1.01 |
