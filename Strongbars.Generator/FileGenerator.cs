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
                    var recursiveDir = pair.Right.GetAdditionalFileMetadata(
                        pair.Left,
                        "RecursiveDir"
                    );
                    return (Namespace: @namespace, RecursiveDir: recursiveDir, File: pair.Left);
                }
            )
            .Where(static pair => !string.IsNullOrEmpty(pair.Namespace));

        var combined = additionalFiles.Combine(globalOptions);

        context.RegisterSourceOutput(
            combined,
            (spc, pair) =>
            {
                var @namespace = ComputeNamespace(pair.Left.Namespace!, pair.Left.RecursiveDir);
                var visibility = pair.Right.Visibility;
                var file = pair.Left.File;
                var filename = Path.GetFileNameWithoutExtension(file.Path);
                var text = file.GetText();

                if (filename is null)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SB002",
                                "File name could not be determined",
                                "File name could not be determined: {0}",
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

                var @class = filename;

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

                var fileContent = text.ToString();
                var generator = new ClassGenerator();

                var allArgs = GetArgs(fileContent).ToArray();
                spc.AddSource(
                    hintName: $"{@namespace}.{@class}.g.cs",
                    source: ClassGenerator.GenerateFileContent(
                        visibility,
                        @namespace,
                        @class,
                        fileContent,
                        allArgs
                    )
                );
            }
        );
    }

    private static string ComputeNamespace(string baseNamespace, string? recursiveDir)
    {
        if (recursiveDir is null or "")
            return baseNamespace;

        var parts = recursiveDir.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? baseNamespace : baseNamespace + "." + string.Join(".", parts);
    }

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
