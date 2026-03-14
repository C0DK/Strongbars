# Strongbars
_Compile-time, type-safe, zero-runtime-error templates for .NET_

![NuGet Version](https://img.shields.io/nuget/v/Strongbars)

## Why Strongbars?

Most template engines discover missing variables at runtime.
Stronbars discovers them at compile time via a C# source generator.
You get:

- IntelliSense for every template parameter
- Build errors instead of blank or broken output
- No reflection, no dynamic compilation, no runtime failures
- Very fast templating – see [benchmarks](#benchmarks) below

> Think of it as “Razor without the runtime” or “Mustache with a compiler”.

## Motivation
I developed Strongbars after using string interpolation for my various micro-webapps.
Having used a variety of different templating engines they are all very dependent on 
dynamically typed input and are in my opinion both way too extensive and too difficult to debug.
I was unable to find a templating engine that gave me the compile-time validation so string interpolation seemed to be the only way.
However this left me with a lot of HTML code in the middle of a c# file. Not super good developer UX.
I missed dedicated files for the templates, but were unable to find any tool that fit my style.
This gave me the best of both worlds.

# Install
```Bash

dotnet add package Strongbars
``` 

# How it works

Add something like this to your `*.csproj`:


```xml
  <ItemGroup>
    <AdditionalFiles Include="Pages/*.html" StrongbarsNamespace="Sample.Pages" />
  </ItemGroup>
```

Every file in `Pages` becomes a strongly-typed class.
`Hello.html`:
```html
<p> 
    Hello {{ firstName }} {{ lastName }}
</p>
```

Build → the generator produces:
```csharp
public class Name
{
    public Name(string firstName, string lastName) {
      ...
    }

    public string Render() => ...
}
```

Usage:
```csharp
using Sample.Pages

var template = new Hello(firstName: "Alex", lastName: "Smith");
Console.WriteLine(template.Render());
```

Output:

```html
<p>
    Hello Alex Smith
</p>
```

You could also use the same variable multiple times, i.e:

`Hello.html`:
```html
<p> 
    Hello {{ firstName }} {{ lastName }} - {{ firstName }} is a pretty name!
</p>
```

See [example](/examples/ExampleConsoleApp) for a complete(r) example.

## Loops? 
Handlebars.js and similar frameworks have [loops](https://handlebarsjs.com/guide/builtin-helpers.html#each) and other helpers. 
They are not supported **by design**. Strongbars encourage high modularity and truely logic less templating. 
Instead, Strongbars allow a syntax for defining a variable as an array, which will then be concatenatted. 
This forces you to create very narrow and modular files, for better or worse (IMO better). 
I.e to implement the same code as in the link you need two files:

`PeopleList.html`:
```html
<ul class="people_list">
  {{ ..items }}
</ul>
```

`ListItem.html`:
```html
<li>
  {{value}}
</li>
```

Which can be used like this: 

```csharp
var template = new PeopleList([
    new ListItem("Yehuda Katz"),
    new ListItem("Alan Johnson"),
    new ListItem("Charles Jolley"),
]);
```

### Warning
If a template has a variable of the same name multiple places with both `..` and without, it will fail.

## Conditionals

Strongbars supports inline conditional blocks using `{% if %}` and `{% unless %}` tags.

### `{% if %}`

Renders the block content when the condition is `true`. An optional `{% else %}` branch is rendered when the condition is `false`.

`Message.html`:
```html
<div class="message {% if urgent %}urgent{% else %}normal{% endif %}">{{message}}</div>
```

Build → the generator produces a `bool urgent` and `TemplateArgument message` constructor parameter:

```csharp
var urgent = new Message(urgent: true,  message: "Server is down!");
var normal = new Message(urgent: false, message: "All systems nominal.");
```

Output:
```html
<div class="message urgent">Server is down!</div>
<div class="message normal">All systems nominal.</div>
```

### `{% unless %}`

The inverse of `{% if %}` — renders the block content when the condition is `false`.
Mirrors [`{{#unless}}`](https://handlebarsjs.com/guide/builtin-helpers.html#unless) in Handlebars.
An optional `{% else %}` branch is rendered when the condition is `true`.

`Subscription.html`:
```html
<p>{% unless premium %}Free tier{% else %}Premium member{% endunless %}</p>
```

```csharp
var free    = new Subscription(premium: false); // → <p>Free tier</p>
var premium = new Subscription(premium: true);  // → <p>Premium member</p>
```

Both tags can be combined freely in the same template, and they compose naturally with `{{ variable }}` interpolation.

### Note
If you still prefer to keep conditional logic in your C# code (e.g. for a proper if/else fallback), optional variables still work well for that:

```csharp
var template = new Entry(
    author
    ? new EntryAuthor(firstName: "Casper", lastName: "Bang")
    : null
);
```

### Warning
If a variable is both marked as optional and not optional it will fallback to being not-optional.

# Current feature set

- Variable injection: `{{foo}}`
- Iterable variables: a `..` preceding a variable name, i.e `{{..foo}}`
- Optional variables: a `?` after the variable name, i.e `{{foo?}}` (Can be combined with iterables)
- Conditional blocks: `{% if condition %}...{% else %}...{% endif %}` — renders first branch when `bool` is `true`, optional `else` branch otherwise
- Inverse conditional blocks: `{% unless condition %}...{% else %}...{% endunless %}` — renders first branch when `bool` is `false`, optional `else` branch otherwise
- Whitespace inside delimiters is ignored
- Works in any text-based file (HTML, JSON, SQL, etc.)
- Generated code is internal by default; visibility can be tweaked via item metadata

## Roadmap 

- Automatic HTML-encoding for `.html` files
- Custom delimiters via `.csproj`  property


## Benchmarks

Rendered with [BenchmarkDotNet](https://benchmarkdotnet.org/) on .NET 8 – **render-only phase** (templates are pre-compiled in setup for all runtime engines; Strongbars compiles entirely at **build time** so there is no parse cost at runtime at all).

Four scenarios are auto-discovered through reflection and run against every engine.
Adding a new template file to `Strongbars.Benchmarks/Templates/` automatically creates a new benchmark row.

```
dotnet run -c Release --project Strongbars.Benchmarks
```

### Results

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.10GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 8.0.125 · .NET 8.0.25 (8.0.2526.11203), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

| Method               | Scenario       |        Mean |    Error |   StdDev | Ratio | Allocated | Alloc Ratio |
|--------------------- |--------------- |------------:|---------:|---------:|------:|----------:|------------:|
| **Strongbars**       | **ArticleCard**    | **1,115.0 ns** | **19.11 ns** | **17.88 ns** |  **1.00** |  **2,992 B** |        **1.00** |
| Scriban              | ArticleCard    | 11,472.1 ns | 218.84 ns | 204.70 ns | 10.29 | 33,123 B |       11.07 |
| Fluid (Liquid)       | ArticleCard    |    490.9 ns |   9.70 ns |   8.60 ns |  0.44 |  1,272 B |        0.43 |
| Handlebars.Net       | ArticleCard    |    657.7 ns |  12.39 ns |  10.98 ns |  0.59 |    432 B |        0.14 |
| Stubble (Mustache)   | ArticleCard    |  2,028.9 ns |  30.48 ns |  27.02 ns |  1.82 |  4,192 B |        1.40 |
|                      |                |             |          |          |       |          |             |
| **Strongbars**       | **List10Items**    | **4,551.6 ns** | **60.55 ns** | **56.64 ns** |  **1.00** |  **8,760 B** |        **1.00** |
| Scriban              | List10Items    | 13,932.4 ns | 246.81 ns | 230.86 ns |  3.06 | 32,802 B |        3.74 |
| Fluid (Liquid)       | List10Items    |  1,503.5 ns |  19.26 ns |  18.01 ns |  0.33 |  1,904 B |        0.22 |
| Handlebars.Net       | List10Items    |    877.2 ns |  17.19 ns |  16.89 ns |  0.19 |    368 B |        0.04 |
| Stubble (Mustache)   | List10Items    |  3,068.2 ns |  58.14 ns |  59.70 ns |  0.67 |  7,152 B |        0.82 |
|                      |                |             |          |          |       |          |             |
| **Strongbars**       | **SimpleGreeting** |   **667.3 ns** |  **8.25 ns** |  **7.32 ns** |  **1.00** |  **1,400 B** |        **1.00** |
| Scriban              | SimpleGreeting | 10,569.3 ns | 125.39 ns | 117.29 ns | 15.84 | 31,059 B |       22.18 |
| Fluid (Liquid)       | SimpleGreeting |    274.9 ns |   2.37 ns |   2.22 ns |  0.41 |    600 B |        0.43 |
| Handlebars.Net       | SimpleGreeting |    355.0 ns |   6.96 ns |   8.01 ns |  0.53 |    104 B |        0.07 |
| Stubble (Mustache)   | SimpleGreeting |    780.1 ns |  11.46 ns |  10.72 ns |  1.17 |  2,888 B |        2.06 |
|                      |                |             |          |          |       |          |             |
| **Strongbars**       | **UserProfile**    | **1,620.6 ns** | **31.49 ns** | **49.03 ns** |  **1.00** |  **4,336 B** |        **1.00** |
| Scriban              | UserProfile    | 11,466.4 ns | 174.30 ns | 163.04 ns |  7.08 | 33,252 B |        7.67 |
| Fluid (Liquid)       | UserProfile    |    587.2 ns |   8.62 ns |   7.65 ns |  0.36 |  1,440 B |        0.33 |
| Handlebars.Net       | UserProfile    |    798.1 ns |   9.67 ns |   8.57 ns |  0.49 |    536 B |        0.12 |
| Stubble (Mustache)   | UserProfile    |  2,074.4 ns |  36.36 ns |  34.01 ns |  1.28 |  4,368 B |        1.01 |

### Notes

- **Strongbars** is used as the baseline (Ratio = 1.00).
- **Scriban** carries the overhead of its full scripting engine on every render; it is 7–16× slower than Strongbars for simple substitution.
- **Fluid (Liquid)** and **Handlebars.Net** compile templates to internal delegates and are faster at pure render time. They also allocate less because they avoid constructing typed model objects.
- **Strongbars list rendering** works through template composition (one `ListItem.Render()` call per element) rather than a native loop, which accounts for its higher allocation in the `List10Items` scenario. The trade-off is compile-time verification of every element type.
- All runtime engines are pre-compiled once in `[GlobalSetup]`; Strongbars has **no** equivalent setup step because its templates are compiled into the binary at build time.

## Thanks to
Strongly inspired and forked from [ConstEmbed](https://github.com/podimo/Podimo.ConstEmbed)
