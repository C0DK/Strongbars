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

## Conditionals?
Similar to loops, conditionals are not supported. Any if/else has to be defined in your application-code.

I.e to [do the if statement in the handlebar example](https://handlebarsjs.com/guide/builtin-helpers.html#if), you would do:


`Entry.html`:
```html
<div class="entry">
{{ author? }}
</div>
```

`EntryAuthor.html`: 
```
<h1>{{firstName}} {{lastName}}</h1>
```

Which can be used like this: 

```csharp
var template = new Entry(
    author 
    ? new EntryAuthor(firstName: "Casper",  lastName: "Bang")
    : null
);
```

If one wanted another fallback (i.e and actual if-else) you'd do:

`AuthorUnknown.html`: 
```
<h1>Author unknown - Sorry</h1>
```

```csharp
var template = new Entry(
    author 
    ? new EntryAuthor(firstName: "Casper",  lastName: "Bang")
    : new AuthorUnknown()
);
```

### Warning
If a variable is both marked as optional and not optional it will fallback to being not-optional.

# Current feature set

- Variable injection: `{{foo}}`
- Iterable variables: a `..` preceding a variable name, i.e `{{..foo}}`
- Optional variables: a `?` after the variable name, i.e `{{foo?}}` (Can be combine with iterables)
- Whitespace inside delimiters is ignored
- Works in any text-based file (HTML, JSON, SQL, etc.)
- Generated code is internal by default; visibility can be tweaked via item metadata

## Roadmap 

- Automatic HTML-encoding for `.html` files
- Custom delimiters via `.csproj`  property


## Thanks to
Strongly inspired and forked from [ConstEmbed](https://github.com/podimo/Podimo.ConstEmbed)
