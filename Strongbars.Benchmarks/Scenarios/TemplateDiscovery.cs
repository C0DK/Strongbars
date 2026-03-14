using System.Reflection;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>
/// Uses reflection to discover every <see cref="ITemplateScenario"/> implementation
/// in this assembly.  Adding a new scenario class is all that's needed to include
/// it in the benchmarks – no manual registration required.
/// </summary>
public static class TemplateDiscovery
{
    private static readonly Lazy<IReadOnlyDictionary<string, ITemplateScenario>> _all =
        new(BuildRegistry, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyDictionary<string, ITemplateScenario> All => _all.Value;

    /// <summary>Scenario names sorted for stable BenchmarkDotNet param ordering.</summary>
    public static IEnumerable<string> ScenarioNames => All.Keys.OrderBy(k => k);

    private static IReadOnlyDictionary<string, ITemplateScenario> BuildRegistry()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ITemplateScenario).IsAssignableFrom(t))
            .Select(t => (ITemplateScenario)Activator.CreateInstance(t)!)
            .ToDictionary(s => s.Name, s => s);
    }
}
