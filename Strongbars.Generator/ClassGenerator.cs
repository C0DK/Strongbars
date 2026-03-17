using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public class ClassGenerator
{
    public static string GenerateFileContent(
        string visibility,
        string @namespace,
        string @class,
        string fileContent
    )
    {
        var rootToken = Parser.Parse(fileContent);
        var allArgs = Deduplicate(rootToken.GetVariables()).ToArray();

        var renderStatements = new List<string>();
        rootToken.GenerateStatements(renderStatements);
        var renderMethod = GenerateRenderMethod(renderStatements);

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

    private static string GenerateRenderMethod(List<string> statements)
    {
        var body =
            statements.Count > 0
                ? "\n            " + string.Join("\n            ", statements) + "\n            "
                : "\n            ";
        return $"public override string Render(){{\n            var _sb = new System.Text.StringBuilder();{body}return _sb.ToString();\n        }}";
    }

    private static IEnumerable<Variable> Deduplicate(IEnumerable<Variable> vars) =>
        vars.GroupBy(v => v.Name)
            .Select(g =>
            {
                if (g.First().Type == VariableType.Bool)
                    return g.First();
                return new Variable(
                    name: g.Key,
                    type: g.First().Type,
                    array: g.First().Array,
                    optional: !g.Any(v => !v.Optional)
                );
            });

    private static HashSet<string> GetConditionVarNames(string fileContent, Regex regex) =>
        new HashSet<string>(
            regex.Matches(fileContent).Cast<Match>().Select(m => m.Groups[1].Value)
        );

    /// <summary>
    /// Pre-splits the template at code-generation time so the generated <c>Render()</c> emits a
    /// plain <c>string.Concat(...)</c> rather than running a regex on every invocation.
    /// </summary>
    private static string GenerateSimpleRender(
        string fileContent,
        IEnumerable<Variable> regularArgs
    )
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
}
