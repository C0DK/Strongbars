using Strongbars.Abstractions;

namespace Strongbars.Generator;

public class ClassGenerator
{
    public static string GenerateFileContent(
        string visibility,
        string @namespace,
        string @class,
        string fileContent,
        string? fileDirectory = null,
        string? projectRoot = null,
        Func<string, string?>? fileReader = null,
        string? filePath = null
    )
    {
        var initialIncludedPaths = filePath != null ? new HashSet<string> { filePath } : null;
        var rootToken = new Parser(
            $"{@namespace}.{@class}",
            fileContent,
            fileDirectory,
            projectRoot,
            fileReader,
            initialIncludedPaths
        ).Parse();
        var variables = DeduplicateVariables(rootToken.GetVariables());

        return $@"
#nullable enable
using Strongbars.Abstractions;
namespace {@namespace}
{{
    {visibility} class {@class} : Template
    {{
        {string.Join("\n        ", GenerateConstructors(visibility, @class, variables))}
        {string.Join("\n        ", variables.Select(GenerateVarDef))}

        public override string Render() => {rootToken.GenerateRenderExpression()};
        public const string Template = @""{escape(fileContent)}"";

        public static Variable[] Variables = new Variable[] {{ {string.Join(", ", variables.Select(GenerateListSpec))} }};
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

    /// <summary>
    /// Collapses multiple occurrences of the same variable name into a single
    /// <see cref="Variable"/>, applying these rules:
    /// <list type="bullet">
    ///   <item>Conflicting types (e.g. Bool vs TemplateArgument) → <see cref="TemplateError"/></item>
    ///   <item>Array / scalar mismatch for the same name → <see cref="TemplateError"/></item>
    ///   <item>Optional: the variable is required if <em>any</em> occurrence is required</item>
    /// </list>
    /// </summary>
    internal static Variable[] DeduplicateVariables(IEnumerable<Variable> variables)
    {
        return variables
            .GroupBy(v => v.Name)
            .Select(g =>
            {
                var types = g.Select(v => v.Type).Distinct().ToArray();
                if (types.Length > 1)
                    throw new TemplateError(
                        $"Variable '{g.Key}' is used with conflicting types: "
                            + string.Join(", ", types)
                    );

                if (g.First().Type != VariableType.Bool)
                {
                    var arrays = g.Select(v => v.Array).Distinct().ToArray();
                    if (arrays.Length > 1)
                        throw new TemplateError(
                            $"Variable '{g.Key}' is used both as an array ({{..{g.Key}}}) and a scalar ({{{{{g.Key}}}}})"
                        );
                }

                // Required if any single occurrence is required (not optional)
                var optional = g.All(v => v.Optional);

                return new Variable(
                    name: g.Key,
                    type: g.First().Type,
                    array: g.First().Array,
                    optional: optional
                );
            })
            .ToArray();
    }
}
