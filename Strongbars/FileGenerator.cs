using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Strongbars;

[Generator]
public class FileGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, _) =>
                (
                    Namespace: provider.GetGlobalOptionOrDefault(
                        "StrongbarsNamespace",
                        "Strongbars.Out"
                    ),
                    Visibility: provider.GetGlobalOptionOrDefault(
                        "StrongbarsVisibility",
                        "internal"
                    )
                )
        );

        var additionalFiles = context
            .AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Select(
                static (pair, token) =>
                {
                    var @foo = pair.Right.GetAdditionalFileMetadata(pair.Left, "Strongbars");
                    return (Foo: @foo, File: pair.Left);
                }
            )
            .Where(static pair => !string.IsNullOrEmpty(pair.Foo));

        var combined = additionalFiles.Combine(globalOptions);

        context.RegisterSourceOutput(
            combined,
            static (spc, pair) =>
            {
                var @namespace = pair.Right.Namespace;
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
        var replacements = Regex.Matches(fileContent, @"\{\{(.*)\}\}");

        var args = GetReplacers(fileContent).ToArray();

        return $@"namespace {@namespace}
{{
    {visibility} static class {@class}
    {{
        public static string Render({string.Join(", ", args.Select(a => $"string {a.arg}").Distinct())}) => Raw{string.Join("", args.Select(a => $@".Replace(@""{escape(a.replacer)}"", {a.arg})").Distinct())};
        public const string Raw = @""{escape(fileContent)}"";

        public static string[] Arguments = new[] {{ {string.Join(", ", args.Select(a => $"\"{a.arg}\""))} }};
    }}
}}";
    }

    private static string escape(string v) => v.Replace("\"", "\"\"");

    private static IEnumerable<(string arg, string replacer)> GetReplacers(string fileContent)
    {
        var matches = HandleBarRegex.Matches(fileContent);

        return matches.Select(match => (match.Groups[1].Value.Trim(), match.Value));
    }

    private static Regex HandleBarRegex = new Regex(@"\{\{(\s*[a-zA-Z]\w*\s*)\}\}");
}
