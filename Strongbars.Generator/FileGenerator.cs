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
            (spc, pair) =>
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
using Strongbars.Abstractions;
namespace {@namespace}
{{
    {visibility} class {@class} : Template
    {{
        {string.Join("\n        ", GenerateConstructors(visibility, @class, args))}
        {string.Join("\n        ", args.Select(GenerateVarDef))}
        public override string Render() => 
            ArgumentRegex.Replace(Template, m => m.Groups[2].Value switch {{
                {string.Join("\n                ", args.Select(GenerateMatchResult).Distinct())}
                var v => throw new ArgumentOutOfRangeException($""'{{v}}' was not a valid argument"")
            }});
        public const string Template = @""{escape(fileContent)}"";

        public static Variable[] Variables = new Variable[] {{ {string.Join(", ", args.Select(GenerateListSpec))} }};
    }}
}}";
    }

    private static IEnumerable<string> GenerateConstructors(
        string visibility,
        string @class,
        IReadOnlyList<Variable> args,
        int argIndex = 0
    )
    {
        if (argIndex == args.Count())
        {
            yield return GenerateConstructor(visibility, @class, args);
        }
        else if (!args[argIndex].Array)
        {
            foreach (var variant in GenerateConstructors(visibility, @class, args, argIndex + 1))
                yield return variant;
        }
        else
        {
            foreach (
                var variant in GenerateConstructors(
                    visibility,
                    @class,
                    args.Select(
                            (a, i) => argIndex == i ? a.AsType(VariableType.TemplateArgument) : a
                        )
                        .ToArray(),
                    argIndex + 1
                )
            )
                yield return variant;
            foreach (
                var variant in GenerateConstructors(
                    visibility,
                    @class,
                    args.Select((a, i) => argIndex == i ? a.AsType(VariableType.String) : a)
                        .ToArray(),
                    argIndex + 1
                )
            )
                yield return variant;
        }
    }

    private static string GenerateConstructor(
        string visibility,
        string @class,
        IReadOnlyList<Variable> args
    ) =>
        $@"
        {visibility} {@class}({GenerateArgDefinitions(args)}) {{
            {string.Join("\n            ", args.Select(GenerateConstructorVarAssignment))}
        }}
    ";

    private static string GenerateArgDefinitions(IReadOnlyList<Variable> args)
    {
        // also if optional?
        if (args.Count() == 1 && args.First() is { Array: true, Optional: false })
        {
            return $"params {GenerateArgDefinition(args.First())}";
        }

        return string.Join(", ", args.Select(GenerateArgDefinition));
    }

    private static string GenerateArgDefinition(Variable v) => $"{ToType(v)} {v.Name}";

    // TODO: For each array also support iformattable
    private static string GenerateConstructorVarAssignment(Variable v) =>
        v switch
        {
            { Array: true, Type: VariableType.String } =>
                $"_{v.Name} = {v.Name}{(v.Optional ? "?" : "")}.Select(s => new StringArgument(s));",
            _ => $"_{v.Name} = {v.Name};",
        };

    private static string GenerateVarDef(Variable v) => $"private readonly {ToType(v)} _{v.Name};";

    private static string GenerateListSpec(Variable v) =>
        $"new Variable(\"{v.Name}\", type: VariableType.{v.Type}, array: {(v.Array ? "true" : "false")}, optional: {(v.Optional ? "true" : "false")})";

    private static string GenerateMatchResult(Variable v) =>
        $@"""{v.Name}"" => "
        + (v.Optional ? $@"_{v.Name} is null ? """" : " : "")
        + (
            v.Array
                ? $@"string.Join("" "", _{v.Name}.Select(v => {RenderExpression("v", v.Type)}))"
                : RenderExpression("_" + v.Name, v.Type)
        )
        + ",";

    private static string RenderExpression(string varName, VariableType type) =>
        type switch
        {
            VariableType.String => $"{varName}.ToString()",
            VariableType.IFormattable => $"{varName}.ToString()",
            VariableType.TemplateArgument => $"{varName}.Render()",
            _ => throw new ArgumentOutOfRangeException(),
        };

    private static string ToType(Variable v) =>
        (v.Array ? $"IEnumerable<{ToType(v.Type)}>" : ToType(v.Type)) + (v.Optional ? "?" : "");

    private static string ToType(VariableType type) =>
        type switch
        {
            VariableType.String => "string",
            VariableType.IFormattable => "IFormattable",
            VariableType.TemplateArgument => "TemplateArgument",
            _ => throw new ArgumentOutOfRangeException(),
        };

    private static string escape(string v) => v.Replace("\"", "\"\"");

    private static IEnumerable<Variable> GetArgs(string fileContent) =>
        Template
            .ArgumentRegex.Matches(fileContent)
            .Cast<Match>()
            .Select(match => new Variable(
                name: match.Groups[2].Value,
                type: VariableType.TemplateArgument,
                array: match.Groups[1].Success,
                optional: match.Groups[3].Success
            ))
            .GroupBy(v => v.Name)
            .Select(g =>
            {
                if (g.DistinctBy(v => v.Type).Count() > 1)
                    // TODO: unit test!
                    throw new InvalidOperationException($"{g.Key} is used with multiple types!");

                // TODO: unit test null thing!
                var isArray = g.Select(v => v.Array);
                if (isArray.Distinct().Count() > 1)
                    throw new InvalidOperationException(
                        $"{g.Key} is used both as array and non array!"
                    );
                return new Variable(
                    name: g.Key,
                    type: g.First().Type,
                    array: isArray.First(),
                    optional: !g.Any(v => !v.Optional)
                );
            });
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
