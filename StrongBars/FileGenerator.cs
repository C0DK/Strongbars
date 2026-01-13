using Microsoft.CodeAnalysis;
namespace Strongbars;

[Generator]
public class FileGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
        (
            Namespace: provider.GetGlobalOptionOrDefault("StrongbarsNamespace", "Strongbars.Out"),
            Visibility: provider.GetGlobalOptionOrDefault("StrongbarsVisibility", "internal")
        ));

        var additionalFiles =
            context.AdditionalTextsProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, token) =>
                {
                    var @foo = pair.Right.GetAdditionalFileMetadata(pair.Left, "Strongbars");
                    return (Foo: @foo, File: pair.Left);
                })
                .Where(static pair => !string.IsNullOrEmpty(pair.Foo));

        var combined = additionalFiles.Combine(globalOptions);

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var @namespace = pair.Right.Namespace;
            var visibility = pair.Right.Visibility;
            var file = pair.Left.File;
            var filename = Path.GetFileNameWithoutExtension(file.Path);
            var @class = filename;
            var escapedContent = file
                .GetText()?
                .ToString()?
                .Replace("\"", "\"\"");

            spc.AddSource(
                hintName: $"{@class}.{filename}.g.cs",
                source: $@"namespace {@namespace}
{{
    {visibility} class {@class}
    {{
        public st
        public const string Raw = @""{escapedContent}"";
    }}
}}");
        });
    }
}
