using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

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
                    ProjectDirectory: provider.GetGlobalOptionOrDefault(
                        "MSBuildProjectDirectory",
                        ""
                    )
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
                var projectDirectory = pair.Right.ProjectDirectory;
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
                var fileDirectory = Path.GetDirectoryName(file.Path);
                var projectRoot = string.IsNullOrEmpty(projectDirectory) ? null : projectDirectory;

                static string? FileReader(string path)
                {
                    try
                    {
                        return File.ReadAllText(path);
                    }
                    catch
                    {
                        return null;
                    }
                }

                try
                {
                    spc.AddSource(
                        hintName: $"{@namespace}.{@class}.g.cs",
                        source: ClassGenerator.GenerateFileContent(
                            visibility,
                            @namespace,
                            @class,
                            fileContent,
                            fileDirectory,
                            projectRoot,
                            FileReader,
                            file.Path
                        )
                    );
                }
                catch (ParserError error)
                {
                    var sourceText = SourceText.From(error.Content);
                    var location = Location.Create(
                        file.Path,
                        new TextSpan(error.Match.Index, error.Match.Length),
                        sourceText.Lines.GetLinePositionSpan(
                            new TextSpan(error.Match.Index, error.Match.Length)
                        )
                    );
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SB003",
                                "Template error",
                                "Reason: {0}",
                                "Strongbars",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true
                            ),
                            location,
                            error.Reason,
                            error.MatchIndex
                        )
                    );
                }
                catch (TemplateError error)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SB003",
                                "Template error",
                                "Reason: {0}",
                                "Strongbars",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true
                            ),
                            Location.None,
                            error.Reason
                        )
                    );
                }
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
}
