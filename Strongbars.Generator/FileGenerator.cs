using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

[Generator]
public class FileGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, _) =>
                (
                    Visibility: provider.GetGlobalOptionOrDefault("StrongbarsVisibility", "public"),
                    foo: ""
                )
        );

        var additionalFiles = context
            .AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Select(
                static (pair, token) =>
                {
                    var @namespace = pair.Right.GetAdditionalFileMetadata(
                        pair.Left,
                        "StrongbarsNamespace"
                    );
                    return (Namespace: @namespace, File: pair.Left);
                }
            )
            .Where(static pair => !string.IsNullOrEmpty(pair.Namespace));

        var combined = additionalFiles.Combine(globalOptions);

        context.RegisterSourceOutput(
            combined,
            static (spc, pair) =>
            {
                var @namespace = pair.Left.Namespace!;
                var visibility = pair.Right.Visibility;
                var file = pair.Left.File;
                var filename = Path.GetFileNameWithoutExtension(file.Path);
                var @class = filename;
                var text = file.GetText();

                if (text is null)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SB001",
                                "File could not be read",
                                "File could not be read: {0}",
                                "Strongbars",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true
                            ),
                            Location.None,
                            file.Path
                        )
                    );
                    return;
                }

                spc.AddSource(
                    hintName: $"{@class}.{filename}.g.cs",
                    source: GenerateFileContent(visibility, @namespace, @class, text.ToString())
                );
            }
        );
    }

    private static string GenerateFileContent(
        string visibility,
        string @namespace,
        string @class,
        string fileContent
    )
    {
        var args = GetArgs(fileContent).ToArray();

        return $@"
#nullable enable
using System.Text.RegularExpressions;
using Strongbars.Abstractions;
namespace {@namespace}
{{
    {visibility} class {@class} : Template
    {{
        {visibility} {@class}({GenerateArgDefinitions(args)}) {{
            {string.Join("\n", args.Select(GenerateConstructorVarAssignment))}
        }}

        {string.Join("\n        ", args.Select(GenerateVarDef))}
        public override string Render() => ArgumentRegex.Replace(Template, m => m.Groups[2].Value switch {{
            {string.Join("\n            ", args.Select(GenerateMatchResult).Distinct())}
            var v => throw new ArgumentOutOfRangeException($""'{{v}}' was not a valid argument"")
        }});
        public const string Template = @""{escape(fileContent)}"";

        public static Variable[] Variables = new Variable[] {{ {string.Join(", ", args.Select(GenerateListSpec))} }};

        public override string ToString() => Render();
        public static implicit operator string({@class} template) => template.Render();
    }}
}}";
    }

    private static string GenerateArgDefinitions(IReadOnlyList<Variable> args)
    {
        if (args.Count() == 1 && args.First() is { Type: VariableType.Array, Optional: false })
        {
            return $"params {GenerateArgDefinition(args.First())}";
        }

        return string.Join(", ", args.Select(GenerateArgDefinition));
    }

    private static string GenerateArgDefinition(Variable v) => $"{ToType(v)} {v.Name}";

    private static string GenerateConstructorVarAssignment(Variable v) => $"_{v.Name} = {v.Name};";

    private static string GenerateVarDef(Variable v) => $"private readonly {ToType(v)} _{v.Name};";

    private static string GenerateListSpec(Variable v) =>
        $"new Variable(\"{v.Name}\", VariableType.{v.Type}, {(v.Optional ? "true" : "false")})";

    private static string GenerateMatchResult(Variable v) =>
        $@"""{v.Name}"" => "
        + (v.Optional ? $@"_{v.Name} is null ? """" : " : "")
        + v.Type switch
        {
            VariableType.String => $"_{v.Name}.Render()",
            VariableType.Array => $@"string.Join("" "", _{v.Name})",
            _ => throw new ArgumentOutOfRangeException(),
        }
        + ",";

    private static string ToType(Variable v) =>
        v.Type switch
        {
            VariableType.String => "TemplateArgument",
            VariableType.Array => "IEnumerable<TemplateArgument>",
            _ => throw new ArgumentOutOfRangeException(),
        } + (v.Optional ? "?" : "");

    private static string escape(string v) => v.Replace("\"", "\"\"");

    private static IEnumerable<Variable> GetArgs(string fileContent)
    {
        var matches = Template.ArgumentRegex.Matches(fileContent);

        return matches
            .Cast<Match>()
            .Select(match => new Variable(
                match.Groups[2].Value,
                match.Groups[1].Success ? VariableType.Array : VariableType.String,
                match.Groups[3].Success
            ))
            .GroupBy(v => v.Name)
            .Select(g =>
            {
                if (g.DistinctBy(v => v.Type).Count() > 1)
                    // TODO: unit test!
                    throw new InvalidOperationException($"{g.Key} is used with multiple types!");

                // TODO: unit test null thing!
                return new Variable(g.Key, g.First().Type, !g.Any(v => !v.Optional));
            });
    }
}

public static class EnumerableExtensions
{
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> items,
        Func<T, TKey> property
    )
    {
        return items.GroupBy(property).Select(x => x.First());
    }
}
