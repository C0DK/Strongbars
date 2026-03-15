using System.Linq.Expressions;
using System.Reflection;
using Fluid;
using HandlebarsDotNet;
using Scriban;
using Scriban.Runtime;
using SbTemplate = Strongbars.Abstractions.Template;
using Stubble.Core;
using Stubble.Core.Builders;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>
/// Automatically creates a benchmark scenario from any Strongbars-generated template class.
/// Reads the class's static <c>Variables</c> array to discover parameters and its
/// <c>Template</c> constant for competitor engines — no hand-written data needed.
///
/// Adding a new <c>.html</c> file to the Templates folder will automatically produce
/// a new benchmark scenario at the next build.
/// </summary>
public sealed class ReflectedScenario : ITemplateScenario
{
    private readonly string _templateSource;
    private readonly Dictionary<string, object> _data;
    private readonly Func<SbTemplate> _factory;

    private Scriban.Template _scribanTemplate = null!;
    private IFluidTemplate _fluidTemplate = null!;
    private TemplateOptions _fluidOptions = null!;
    private HandlebarsTemplate<object, object> _handlebarsTemplate = null!;
    private StubbleVisitorRenderer _stubble = null!;

    public string Name { get; }

    public ReflectedScenario(Type templateType)
    {
        Name = templateType.Name;

        // The generator emits a public const string Template on every class.
        _templateSource = (string)templateType
            .GetField("Template", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

        // The generator emits a public static Variable[] Variables on every class.
        var variables = (Strongbars.Abstractions.Variable[])templateType
            .GetField("Variables", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

        // Build sample data: variable name → "Sample{Name}" string.
        _data = variables.ToDictionary(
            v => v.Name,
            v => (object)$"Sample{v.Name}");

        // Pre-compile a zero-reflection ctor factory so the hot path is fast.
        _factory = BuildFactory(templateType, variables);
    }

    private static Func<SbTemplate> BuildFactory(Type type, Strongbars.Abstractions.Variable[] variables)
    {
        var ctor = type.GetConstructors()[0];
        // Each ctor parameter is a TemplateArgument; supply a pre-created string argument.
        var argExprs = variables.Select(v =>
            Expression.Constant(
                (Strongbars.Abstractions.TemplateArgument)$"Sample{v.Name}",
                typeof(Strongbars.Abstractions.TemplateArgument)));

        var newExpr = Expression.New(ctor, argExprs);
        return Expression.Lambda<Func<SbTemplate>>(newExpr).Compile();
    }

    public void Setup()
    {
        _scribanTemplate = Scriban.Template.Parse(_templateSource);

        _fluidOptions = new TemplateOptions();
        var parser = new FluidParser();
        parser.TryParse(_templateSource, out _fluidTemplate!, out _);

        _handlebarsTemplate = Handlebars.Compile(_templateSource);

        _stubble = new StubbleBuilder().Build();
        _stubble.Render(_templateSource, _data); // warm up parse cache
    }

    public string RenderStrongbars() => _factory().Render();

    public string RenderScriban()
    {
        var obj = new ScriptObject();
        foreach (var (k, v) in _data) obj[k] = v;
        return _scribanTemplate.Render(obj);
    }

    public string RenderFluid()
    {
        var ctx = new Fluid.TemplateContext(_fluidOptions);
        foreach (var (k, v) in _data) ctx.SetValue(k, (string)v);
        return _fluidTemplate.RenderAsync(ctx).GetAwaiter().GetResult();
    }

    public string RenderHandlebars() => _handlebarsTemplate(_data);

    public string RenderStubble() => _stubble.Render(_templateSource, _data);
}
