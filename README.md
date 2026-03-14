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

Render-only, all engines pre-compiled. Run with `dotnet run -c Release --project Strongbars.Benchmarks`.
Full results: [`BenchmarkDotNet.Artifacts/results/`](BenchmarkDotNet.Artifacts/results/).

| Engine | SimpleGreeting | ArticleCard | UserProfile | ListItem |
|---|---:|---:|---:|---:|
| **Strongbars** | **58 ns** | **111 ns** | **148 ns** | **29 ns** |
| Fluid (Liquid) | 268 ns | 463 ns | 591 ns | 107 ns |
| Handlebars.Net | 340 ns | 625 ns | 808 ns | 181 ns |
| Stubble (Mustache) | 752 ns | 2,079 ns | 1,989 ns | 373 ns |
| Scriban | 10,231 ns | 10,831 ns | 11,155 ns | 9,643 ns |

## Thanks to
Strongly inspired and forked from [ConstEmbed](https://github.com/podimo/Podimo.ConstEmbed)
