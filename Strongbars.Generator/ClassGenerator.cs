using Microsoft.CodeAnalysis;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public class ClassGenerator
{
    public static string GenerateFileContent(
        SourceProductionContext context,
        string visibility,
        string @namespace,
        string @class,
        string fileContent
    )
    {
        var rootToken = new Parser(context, $"{@namespace}.{@class}", fileContent).Parse();
        var variables = rootToken.GetVariables().ToArray();

        return $@"
#nullable enable
using Strongbars.Abstractions;
namespace {@namespace}
{{
    {visibility} class {@class} : Template
    {{
        {string.Join("\n        ", GenerateConstructors(visibility, @class, variables))}
        {string.Join("\n        ", variables.Select(GenerateVarDef))}

        public override string Render() => {rootToken.GenerateRenderer()}; 
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
}
