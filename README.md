# Strongbars
_Compile-time, type-safe, zero-runtime-error templates for .NET_

## Why Strongbars?

Most template engines discover missing variables at runtime.
ConstEmbed discovers them at compile time via a C# source generator.
You get:

    IntelliSense for every template parameter
    Build errors instead of blank or broken output
    No reflection, no dynamic compilation, no runtime failures

> Think of it as “Razor without the runtime” or “Mustache with a compiler”.

# Install
```Bash

dotnet add package ConstEmbed
``` 

## How it works

Add something like this to your `*.csproj` file:


```xml
  <ItemGroup>
    <AdditionalFiles Include="Pages/*.html" StrongbarsNamespace="Sample.Pages" />
  </ItemGroup>
```

And any files in `Pages` will automatically be turned into a class. I.e a file `Hello.html`:
```html
<p>
    Hello
      {{ firstName }}
      {{ lastName }}
</p>
```

Build → the generator creates a strongly-typed class whose constructor expects exactly those variables:

```csharp
using Sample.Pages

var template = new Hello(firstName: "Alex", lastName: "Smith");
Console.WriteLine(template.Render());
```

Output:

```html

<p>
    Hello
      Alex
      Smith
</p>
```

See [example](/examples/ExampleConsoleApp) for a complete(r) example.



## Current feature set

- Variable injection: {{foo}}
- Whitespace inside delimiters is ignored
- Works in any text-based file (HTML, JSON, SQL, etc.)
- Generated code is internal by default; visibility can be tweaked via item metadata

## Roadmap / contributions welcome

- {{#each}} loops
- {{#if}} conditionals
- sanitizing input in HTML files.
- Custom delimiters via `.csproj`  property


## Thanks to
Strongly inspired and forked from [ConstEmbed](https://github.com/podimo/Podimo.ConstEmbed)
