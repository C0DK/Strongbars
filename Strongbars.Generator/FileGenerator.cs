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
        var allArgs = GetArgs(fileContent).ToArray();
        var regularArgs = allArgs.Where(a => a.Type != VariableType.Bool).ToArray();

        var ifVarNames = GetConditionVarNames(fileContent, Template.ConditionalRegex);
        var unlessVarNames = GetConditionVarNames(fileContent, Template.UnlessRegex);
        var ifArgs = allArgs.Where(a => ifVarNames.Contains(a.Name)).ToArray();
        var unlessArgs = allArgs.Where(a => unlessVarNames.Contains(a.Name)).ToArray();

        var renderMethod =
            ifArgs.Any() || unlessArgs.Any()
                ? GenerateConditionalRender(ifArgs, unlessArgs, regularArgs)
                : GenerateSimpleRender(fileContent, regularArgs);

        return $@"
#nullable enable
using Strongbars.Abstractions;
namespace {@namespace}
{{
    {visibility} class {@class} : Template
    {{
        {string.Join("\n        ", GenerateConstructors(visibility, @class, allArgs))}
        {string.Join("\n        ", allArgs.Select(GenerateVarDef))}
        {renderMethod}
        public const string Template = @""{escape(fileContent)}"";

        public static Variable[] Variables = new Variable[] {{ {string.Join(", ", allArgs.Select(GenerateListSpec))} }};
    }}
}}";
    }

    private static HashSet<string> GetConditionVarNames(string fileContent, Regex regex) =>
        new HashSet<string>(
            regex.Matches(fileContent).Cast<Match>().Select(m => m.Groups[1].Value)
        );

    /// <summary>
    /// Pre-splits the template at code-generation time so the generated <c>Render()</c> emits a
    /// plain <c>string.Concat(...)</c> rather than running a regex on every invocation.
    /// </summary>
    private static string GenerateSimpleRender(string fileContent, IEnumerable<Variable> regularArgs)
    {
        var argsById = regularArgs.ToDictionary(v => v.Name);
        var segments = new List<string>();
        int lastIndex = 0;

        foreach (Match match in Template.ArgumentRegex.Matches(fileContent))
        {
            // Literal text before this variable slot
            if (match.Index > lastIndex)
            {
                var literal = fileContent.Substring(lastIndex, match.Index - lastIndex);
                segments.Add("@\"" + escape(literal) + "\"");
            }

            // Expression for this variable slot
            var varName = match.Groups[2].Value;
            if (argsById.TryGetValue(varName, out var v))
                segments.Add(BuildVarExpression(v));

            lastIndex = match.Index + match.Length;
        }

        // Any trailing literal after the last variable
        if (lastIndex < fileContent.Length)
        {
            var literal = fileContent.Substring(lastIndex);
            segments.Add("@\"" + escape(literal) + "\"");
        }

        if (segments.Count == 0)
            return "public override string Render() => \"\";";

        if (segments.Count == 1)
            return $"public override string Render() => {segments[0]};";

        return "public override string Render() =>\n            string.Concat(\n                "
            + string.Join(",\n                ", segments)
            + ");";
    }

    private static string BuildVarExpression(Variable v) =>
        (v.Optional ? $"_{v.Name} is null ? \"\" : " : "")
        + (
            v.Array
                ? $"string.Join(\" \", _{v.Name}.Select(item => {RenderExpression("item", v.Type)}))"
                : RenderExpression("_" + v.Name, v.Type)
        );

    private static string GenerateConditionalRender(
        Variable[] ifArgs,
        Variable[] unlessArgs,
        Variable[] regularArgs
    )
    {
        var steps = new List<string>();

        if (ifArgs.Any())
            steps.Add(
                $@"ConditionalRegex.Replace({{source}}, m => m.Groups[1].Value switch {{
                {string.Join("\n                ", ifArgs.Select(GenerateConditionalMatchResult))}
                var v => throw new ArgumentOutOfRangeException($""'{{v}}' was not a valid argument"")
            }})"
            );

        if (unlessArgs.Any())
            steps.Add(
                $@"UnlessRegex.Replace({{source}}, m => m.Groups[1].Value switch {{
                {string.Join("\n                ", unlessArgs.Select(GenerateUnlessMatchResult))}
                var v => throw new ArgumentOutOfRangeException($""'{{v}}' was not a valid argument"")
            }})"
            );

        if (regularArgs.Any())
            steps.Add(
                $@"ArgumentRegex.Replace({{source}}, m => m.Groups[2].Value switch {{
                {string.Join("\n                ", regularArgs.Select(GenerateMatchResult).Distinct())}
                var v => throw new ArgumentOutOfRangeException($""'{{v}}' was not a valid argument"")
            }})"
            );

        if (steps.Count == 1)
            return "public override string Render() =>\n            "
                + steps[0].Replace("{source}", "Template")
                + ";";

        var lines = new List<string> { "public override string Render()", "        {" };
        for (var i = 0; i < steps.Count; i++)
        {
            var source = i == 0 ? "Template" : "result";
            var expanded = steps[i].Replace("{source}", source);
            if (i == steps.Count - 1)
                lines.Add($"            return {expanded};");
            else if (i == 0)
                lines.Add($"            var result = {expanded};");
            else
                lines.Add($"            result = {expanded};");
        }
        lines.Add("        }");
        return string.Join("\n", lines);
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

    private static string GenerateConditionalMatchResult(Variable v) =>
        $"\"{v.Name}\" => _{v.Name} ? m.Groups[2].Value : m.Groups[3].Value,";

    private static string GenerateUnlessMatchResult(Variable v) =>
        $"\"{v.Name}\" => !_{v.Name} ? m.Groups[2].Value : m.Groups[3].Value,";

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
            VariableType.Bool => "bool",
            _ => throw new ArgumentOutOfRangeException(),
        };

    private static string escape(string v) => v.Replace("\"", "\"\"");

    private static IEnumerable<Variable> GetArgs(string fileContent)
    {
        var regularVars = Template
            .ArgumentRegex.Matches(fileContent)
            .Cast<Match>()
            .Select(match => new Variable(
                name: match.Groups[2].Value,
                type: VariableType.TemplateArgument,
                array: match.Groups[1].Success,
                optional: match.Groups[3].Success
            ));

        var conditionVars = Template
            .ConditionalRegex.Matches(fileContent)
            .Cast<Match>()
            .Select(match => new Variable(
                name: match.Groups[1].Value,
                type: VariableType.Bool,
                array: false,
                optional: false
            ));

        var unlessVars = Template
            .UnlessRegex.Matches(fileContent)
            .Cast<Match>()
            .Select(match => new Variable(
                name: match.Groups[1].Value,
                type: VariableType.Bool,
                array: false,
                optional: false
            ));

        return regularVars
            .Concat(conditionVars)
            .Concat(unlessVars)
            .GroupBy(v => v.Name)
            .Select(g =>
            {
                if (g.DistinctBy(v => v.Type).Count() > 1)
                    // TODO: unit test!
                    throw new InvalidOperationException($"{g.Key} is used with multiple types!");

                if (g.First().Type == VariableType.Bool)
                    return g.First();

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
