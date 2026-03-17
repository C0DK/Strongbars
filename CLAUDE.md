# CLAUDE.md — Strongbars Codebase Guide

## Project Overview

**Strongbars** is a compile-time, type-safe .NET source generator that transforms text templates (HTML, JSON, SQL, etc.) containing `{{variable}}` placeholder syntax into strongly-typed C# classes. The key design goal is zero runtime overhead and build-time validation of template parameters.

Published on NuGet as two packages:
- `Strongbars` — main package users reference
- `Strongbars.Abstractions` — base types that generated classes inherit from

---

## Repository Structure

```
Strongbars/
├── Strongbars/                     # NuGet package shell (no real C# code)
│   ├── Strongbars.csproj           # Version, NuGet metadata, package bundling
│   └── build/
│       └── Strongbars.props        # MSBuild props injected into consumer projects
├── Strongbars.Abstractions/        # Base types for generated classes
│   ├── Template.cs                 # Base class: Template : TemplateArgument
│   ├── Variable.cs                 # Variable metadata (name, type, optional, array)
│   └── VariableType.cs             # Enum: String, IFormattable, TemplateArgument, Bool
├── Strongbars.Generator/           # The actual source generator
│   ├── FileGenerator.cs            # IIncrementalGenerator entry point
│   ├── ClassGenerator.cs           # C# class code generation from AST
│   ├── Parser.cs                   # Template parser → ITemplateNode AST + ParserError
│   ├── TemplateToken.cs            # ITemplateNode implementations (AST node types)
│   ├── ProviderExtensions.cs       # MSBuild property/metadata helpers
│   └── EnumerableExtensions.cs     # DistinctBy polyfill for netstandard2.0
├── Strongbars.Tests/               # NUnit test suite
│   ├── FileGeneratorTests.cs       # End-to-end generator tests + error diagnostic tests
│   ├── ParserTests.cs              # Standalone Parser unit tests (AST structure + errors)
│   ├── sample/                     # HTML template fixtures used in generator tests
│   └── Utils/                      # Test infrastructure (mocks for analyzer APIs)
├── Strongbars.Benchmarks/          # BenchmarkDotNet performance comparison
│   ├── AllTemplatesBenchmark.cs    # Benchmark class (net10.0, RuntimeMoniker.Net10_0)
│   ├── Templates/                  # HTML templates for benchmarks (auto-discovered)
│   └── Scenarios/                  # Scenario wrappers + competitor engine adapters
├── examples/
│   └── ExampleConsoleApp/          # Working usage example
├── .github/workflows/
│   ├── dotnet.yml                  # CI: format check + tests on push/PR
│   └── nuget.yml                   # CD: publish to NuGet on version tags
├── .config/dotnet-tools.json       # Pins CSharpier version
├── Directory.Build.props           # Global C# settings for all projects
├── Strongbars.slnx                 # Solution file (new .slnx format)
├── check_version.nu                # NuShell script: validates tag matches csproj version
└── README.md                       # User-facing documentation
```

---

## Development Commands

```bash
# Restore dependencies and local tools
dotnet restore
dotnet tool restore

# Check code formatting (must pass in CI)
dotnet csharpier check .

# Auto-format code
dotnet csharpier format .

# Build all projects
dotnet build

# Run tests
dotnet test

# Run benchmarks (must be Release mode)
dotnet run -c Release --project Strongbars.Benchmarks

# Build release + create NuGet packages
dotnet build --configuration Release /p:Version=<version>
dotnet pack --output .
```

---

## Template Syntax

Strongbars templates use these constructs:

| Syntax | Meaning |
|--------|---------|
| `{{foo}}` | Required string variable |
| `{{foo?}}` | Optional string variable |
| `{{..foo}}` | Iterable variable (generates `IEnumerable<string>` + `IEnumerable<TemplateArgument>` overloads) |
| `{% if foo %}...{% end %}` | Conditional block |
| `{% if foo %}...{% else %}...{% end %}` | Conditional with else |
| `{% unless foo %}...{% end %}` | Inverted conditional |
| `{% unless foo %}...{% else %}...{% end %}` | Inverted conditional with else |

Variables can hold `string`, `IFormattable`, `TemplateArgument`, or `bool` — the generator detects the required type from usage context.

**Known limitation:** Each variable name must appear at most once per template. The generator does not deduplicate variables, so using `{{name}}` in two places generates a duplicate constructor parameter. See `EnumerableExtensions.DistinctBy` for the intended deduplication hook.

---

## Key Architecture Decisions

### Source Generator (`FileGenerator.cs` + `ClassGenerator.cs`)
- Implements `IIncrementalGenerator` (the modern incremental API, not legacy `ISourceGenerator`)
- Reads template files registered as `AdditionalFiles` with `StrongbarsNamespace` metadata
- `FileGenerator` handles MSBuild plumbing and diagnostic reporting (SB001–SB003)
- `ClassGenerator` generates the C# class source from the parsed AST
- Emits one C# class per template file; class name = filename without extension
- For `{{..array}}` variables, generates two constructor overloads: `IEnumerable<string>` and `IEnumerable<TemplateArgument>`

### Parser and AST (`Parser.cs` + `TemplateToken.cs`)
- `Parser` converts raw template text into an `ITemplateNode` tree via regex
- AST node types: `LiteralTemplateNode`, `VariableTemplateNode`, `CompositeTemplateNode`, `ConditionalTemplateNode`
- Each node implements `ITemplateNode` with:
  - `GenerateRenderExpression()` — returns a C# expression string that computes the rendered output
  - `GetVariables()` — yields all `Variable` instances referenced by this subtree
- Parser errors throw `ParserError` (caught by `FileGenerator` and reported as SB003 diagnostics)

### Abstractions (`Template.cs`)
- All generated classes inherit from `Template : TemplateArgument`
- `TemplateArgument` has implicit conversions from `string` and `int`, enabling templates to be nested inside other templates
- Contains the canonical regex patterns used by both the generator (for parsing) and the generated classes (for rendering)

### Visibility Control
- Generated classes are `internal` by default
- Consumers set `<StrongbarsVisibility>public</StrongbarsVisibility>` in their `.csproj` to make them public
- This is exposed via `Strongbars.props` as a compiler-visible property

### Benchmarks (`Strongbars.Benchmarks/`)
- Targets `net10.0` with `RuntimeMoniker.Net10_0`
- `ReflectedScenario` auto-discovers generated template classes and creates benchmark scenarios
- Converts Strongbars template syntax to each competitor's dialect (Scriban, Fluid/Liquid, Handlebars, Stubble/Mustache)
- Add new `.html` files to `Templates/` to add new benchmark scenarios automatically
- **Constraint:** Benchmark templates must not repeat the same variable name (generator limitation)

---

## Code Conventions

### C# Style
- **Formatter**: CSharpier (enforced in CI — must pass before merging)
- **C# version**: LangVersion 10 (file-scoped namespaces, records if needed)
- **Nullable**: `#nullable enable` everywhere; `TreatWarningsAsErrors: true`
- **Naming**: PascalCase for types and public members; `_camelCase` for private fields

### Testing
- Framework: NUnit 4.x with `Assert.That(...)` fluent assertions
- `FileGeneratorTests.cs`: end-to-end tests via `OutputGenerator` (full generator pipeline)
- `ParserTests.cs`: unit tests that call `Parser.Parse()` directly, checking AST node types/structure
- Template fixtures live in `Strongbars.Tests/sample/` as real `.html` files
- Every public feature of the generator must have test coverage
- Test names are descriptive: e.g., `ConditionalRendersContentWhenTrue`

### Error Handling
- Warnings are errors — fix all nullable warnings, don't suppress with `!` unless unavoidable
- The MSBuild property `<MSBuildWarningsAsErrors>CS8785</MSBuildWarningsAsErrors>` catches source generator exceptions at build time

---

## Diagnostic Codes

| Code | Severity | Trigger |
|------|----------|---------|
| SB001 | Error | Template file could not be read |
| SB002 | Error | Template file name could not be determined |
| SB003 | Error | Parser failed (invalid variable name, invalid expression, unclosed conditional) |

---

## CI/CD Pipelines

### `dotnet.yml` — runs on every push and PR to `main`
1. **format** job: `dotnet csharpier check .` — fails if any file is not formatted
2. **test** job: `dotnet test`

### `nuget.yml` — runs on tags matching `v[0-9]+.[0-9]+.[0-9]+`
1. Verifies commit is on `main`
2. Runs `check_version.nu` to assert tag matches version in `Strongbars.csproj`
3. Builds in Release mode
4. Runs tests
5. Packs and pushes `Strongbars` + `Strongbars.Abstractions` to NuGet.org (uses `NUGET_KEY` secret)
6. Creates a GitHub Release with auto-generated notes

**To release a new version:**
1. Bump `<Version>` in `Strongbars/Strongbars.csproj`
2. Commit and merge to `main`
3. Create a git tag: `git tag v<version> && git push origin v<version>`

---

## Project Settings Reference

| Setting | Value | Where |
|---------|-------|-------|
| C# version | 10 | `Directory.Build.props` |
| Nullable | enabled | `Directory.Build.props` |
| Warnings as errors | true | `Directory.Build.props` |
| Target framework (lib) | `netstandard2.0` | individual `.csproj` |
| Target framework (tests) | `net10.0` | `Strongbars.Tests.csproj` |
| Target framework (benchmarks) | `net10.0` | `Strongbars.Benchmarks.csproj` |
| Code formatter | CSharpier 1.2.5 | `.config/dotnet-tools.json` |
| Current version | 1.4.0 | `Strongbars/Strongbars.csproj` |

---

## Adding a New Template Feature

1. Add sample `.html` fixture to `Strongbars.Tests/sample/`
2. Write failing tests in `FileGeneratorTests.cs` covering the new syntax
3. Add direct `Parser` tests in `ParserTests.cs` for the new AST structure
4. Update regex patterns in `Parser.cs` if needed
5. Add new `ITemplateNode` implementation in `TemplateToken.cs` if needed
6. Implement code generation in `ClassGenerator.cs`
7. Run `dotnet csharpier format .` to format
8. Run `dotnet test` to verify all tests pass
9. Update `README.md` with the new syntax

---

## Common Pitfalls

- **Do not use `ISourceGenerator`** — the project uses the incremental `IIncrementalGenerator` API for performance
- **Do not add runtime logic** to the main `Strongbars` package — it contains no C# source, only the NuGet packaging shell; runtime logic belongs in `Strongbars.Abstractions`
- **Always run `dotnet csharpier format .`** before committing — the CI check will fail otherwise
- **Version and tag must match** — the `check_version.nu` script enforces this during the publish pipeline
- **`netstandard2.0` target** — the library projects must remain on `netstandard2.0` for broad compatibility; only tests, benchmarks, and examples use `net10.0`
- **Variables must be unique per template** — the generator does not deduplicate; using the same variable name twice causes a duplicate-parameter compile error in the generated code
- **Benchmark templates must use `{% end %}` not `{% endif %}`** — the Strongbars parser uses `{% end %}` as the generic block closer; the benchmark's syntax converters map this to each engine's specific closing tag
