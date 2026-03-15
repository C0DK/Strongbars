using System.Reflection;
using SbTemplate = Strongbars.Abstractions.Template;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>
/// Discovers all Strongbars-generated template classes in the
/// <c>Strongbars.Benchmarks.Templates</c> namespace and creates a
/// <see cref="ReflectedScenario"/> for each one automatically.
///
/// Adding a new <c>.html</c> file to the Templates folder and rebuilding
/// is all that's needed to include it in benchmarks.
/// </summary>
public static class TemplateDiscovery
{
    private static readonly Lazy<IReadOnlyDictionary<string, ITemplateScenario>> _all = new(
        BuildRegistry,
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    public static IReadOnlyDictionary<string, ITemplateScenario> All => _all.Value;

    /// <summary>Scenario names sorted for stable BenchmarkDotNet param ordering.</summary>
    public static IEnumerable<string> ScenarioNames => All.Keys.OrderBy(k => k);

    private static IReadOnlyDictionary<string, ITemplateScenario> BuildRegistry() =>
        Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.IsSubclassOf(typeof(SbTemplate))
                && t.Namespace == "Strongbars.Benchmarks.Templates"
            )
            .Select(t => (ITemplateScenario)new ReflectedScenario(t))
            .ToDictionary(s => s.Name, s => s);
}
