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
using System.Text.RegularExpressions;
using Strongbars.Abstractions;
namespace {@namespace}
{{
    {visibility} class {@class}
    {{
        {visibility} {@class}({string.Join(", ", args.Select(GenerateArgDefinition))}) {{
            {string.Join("\n", args.Select(GenerateConstructorVarAssignment))}
        }}

        {string.Join("\n        ", args.Select(GenerateVarDef))}
        public string Render() => TemplateRegex.ArgumentRegex.Replace(Template, m => m.Groups[1].Value switch {{
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

    private static string GenerateArgDefinition(Variable v) => $"{ToType(v)} {v.Name}";
    private static string GenerateConstructorVarAssignment(Variable v) => $"_{v.Name} = {v.Name};";
    private static string GenerateVarDef(Variable v) => $"private readonly {ToType(v)} _{v.Name};";

    private static string GenerateListSpec(Variable v) =>
        $"new Variable(\"{v.Name}\", VariableType.{v.Type})";

    private static string GenerateMatchResult(Variable v) =>
        $@"""{v.Name}"" => "
        + v.Type switch
        {
            VariableType.String => $"_{v.Name}",
            VariableType.Array => $@"string.Join("" "", _{v.Name})",
            VariableType.OptionalString => $@"_{v.Name} is not null ? _{v.Name} : """" ",
            _ => throw new ArgumentOutOfRangeException(),
        }
        + ",";

    private static string ToType(Variable v) => ToType(v.Type);

    private static string ToType(VariableType t) =>
        t switch
        {
            VariableType.String => "string",
            VariableType.Array => "string[]",
            VariableType.OptionalString => "string?",
            _ => throw new ArgumentOutOfRangeException(),
        };

    private static string escape(string v) => v.Replace("\"", "\"\"");

    private static IEnumerable<Variable> GetArgs(string fileContent)
    {
        var matches = TemplateRegex.ArgumentRegex.Matches(fileContent);

        return matches
            .Cast<Match>()
            .Select(match => new Variable(match.Groups[1].Value, VariableType.String))
            .Distinct();
    }
}
