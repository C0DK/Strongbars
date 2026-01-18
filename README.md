# Strongbars
_Compile-time, type-safe, zero-runtime-error templates for .NET_

## Why Strongbars?

Most template engines discover missing variables at runtime.
Stronbars discovers them at compile time via a C# source generator.
You get:

- IntelliSense for every template parameter
- Build errors instead of blank or broken output
- No reflection, no dynamic compilation, no runtime failures
- Very fast templating (I haven't bothered to benchmark. Sorry)

> Think of it as “Razor without the runtime” or “Mustache with a compiler”.

# Install
```Bash

dotnet add package Strongbars
``` 

## How it works

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

See [example](/examples/ExampleConsoleApp) for a complete(r) example.



## Current feature set

- Variable injection: {{foo}}
- Whitespace inside delimiters is ignored
- Works in any text-based file (HTML, JSON, SQL, etc.)
- Generated code is internal by default; visibility can be tweaked via item metadata

## Roadmap 

- Automatic HTML-encoding for `.html` files
- {{#each}} loops
- {{#if}} conditionals
- Custom delimiters via `.csproj`  property


## Thanks to
Strongly inspired and forked from [ConstEmbed](https://github.com/podimo/Podimo.ConstEmbed)
