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
│   ├── Template.cs                 # Base class with regex parsing logic
│   ├── Variable.cs                 # Variable metadata (name, type, optional, array)
│   └── VariableType.cs             # Enum: String, IFormattable, TemplateArgument, Bool
├── Strongbars.Generator/           # The actual source generator
│   ├── FileGenerator.cs            # IIncrementalGenerator implementation (~370 lines)
│   └── ProviderExtensions.cs       # MSBuild property/metadata helpers
├── Strongbars.Tests/               # NUnit test suite
│   ├── FileGeneratorTests.cs       # Comprehensive generator tests
│   ├── sample/                     # HTML template fixtures used in tests
│   └── Utils/                      # Test infrastructure (mocks for analyzer APIs)
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
dotnet csharpier .

# Build all projects
dotnet build

# Run tests
dotnet test

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
| `{% if foo %}...{% endif %}` | Conditional block |
| `{% if foo %}...{% else %}...{% endif %}` | Conditional with else |
| `{% unless foo %}...{% endunless %}` | Inverted conditional |
| `{% unless foo %}...{% else %}...{% endunless %}` | Inverted conditional with else |

Variables can hold `string`, `IFormattable`, `TemplateArgument`, or `bool` — the generator detects the required type from usage context.

---

## Key Architecture Decisions

### Source Generator (`FileGenerator.cs`)
- Implements `IIncrementalGenerator` (the modern incremental API, not legacy `ISourceGenerator`)
- Reads template files registered as `AdditionalFiles` with `StrongbarsNamespace` metadata
- Extracts variables using compiled regex patterns (defined in `Template.cs`)
- Emits one C# class per template file; class name = filename without extension
- For `{{..array}}` variables, generates two constructor overloads: `IEnumerable<string>` and `IEnumerable<TemplateArgument>`

### Abstractions (`Template.cs`)
- All generated classes inherit from `Template : TemplateArgument`
- `TemplateArgument` has implicit conversions from `string` and `int`, enabling templates to be nested inside other templates
- Contains the canonical regex patterns used by both the generator (for parsing) and the generated classes (for rendering)

### Visibility Control
- Generated classes are `internal` by default
- Consumers set `<StrongbarsVisibility>public</StrongbarsVisibility>` in their `.csproj` to make them public
- This is exposed via `Strongbars.props` as a compiler-visible property

---

## Code Conventions

### C# Style
- **Formatter**: CSharpier (enforced in CI — must pass before merging)
- **C# version**: LangVersion 10 (file-scoped namespaces, records if needed)
- **Nullable**: `#nullable enable` everywhere; `TreatWarningsAsErrors: true`
- **Naming**: PascalCase for types and public members; `_camelCase` for private fields

### Testing
- Framework: NUnit 4.x with `Assert.That(...)` fluent assertions
- Tests drive the source generator directly using `TestAdditionalText` / `TestAnalyzerConfigOptions` mock utilities
- Template fixtures live in `Strongbars.Tests/sample/` as real `.html` files
- Every public feature of the generator must have test coverage
- Test names are descriptive: e.g., `ConditionalRendersContentWhenTrue`

### Error Handling
- Warnings are errors — fix all nullable warnings, don't suppress with `!` unless unavoidable
- The MSBuild property `<MSBuildWarningsAsErrors>CS8785</MSBuildWarningsAsErrors>` catches source generator exceptions at build time

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
| Code formatter | CSharpier 1.2.5 | `.config/dotnet-tools.json` |
| Current version | 1.4.0 | `Strongbars/Strongbars.csproj` |

---

## Adding a New Template Feature

1. Add sample `.html` fixture to `Strongbars.Tests/sample/`
2. Write failing tests in `FileGeneratorTests.cs` covering the new syntax
3. Update the regex patterns in `Template.cs` (Abstractions) if the new syntax needs runtime parsing
4. Implement the code-generation logic in `FileGenerator.cs`
5. Run `dotnet csharpier .` to format
6. Run `dotnet test` to verify all tests pass
7. Update `README.md` with the new syntax

---

## Common Pitfalls

- **Do not use `ISourceGenerator`** — the project uses the incremental `IIncrementalGenerator` API for performance
- **Do not add runtime logic** to the main `Strongbars` package — it contains no C# source, only the NuGet packaging shell; runtime logic belongs in `Strongbars.Abstractions`
- **Always run `dotnet csharpier .`** before committing — the CI check will fail otherwise
- **Version and tag must match** — the `check_version.nu` script enforces this during the publish pipeline
- **`netstandard2.0` target** — the library projects must remain on `netstandard2.0` for broad compatibility; only tests and examples use `net10.0`
