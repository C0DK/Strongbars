using Fluid;
using HandlebarsDotNet;
using Scriban;
using Scriban.Runtime;
using Strongbars.Benchmarks.Templates;
using Stubble.Core;
using Stubble.Core.Builders;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>Two-variable substitution: firstName, lastName.</summary>
public sealed class SimpleGreetingScenario : ITemplateScenario
{
    public string Name => "SimpleGreeting";

    // The same {{ variable }} syntax works for all five engines on simple substitutions.
    private const string TemplateSource = "<p>Hello {{ firstName }} {{ lastName }}</p>\n";

    private Scriban.Template _scribanTemplate = null!;
    private IFluidTemplate _fluidTemplate = null!;
    private TemplateOptions _fluidOptions = null!;
    private HandlebarsTemplate<object, object> _handlebarsTemplate = null!;
    private StubbleVisitorRenderer _stubble = null!;

    public void Setup()
    {
        _scribanTemplate = Scriban.Template.Parse(TemplateSource);

        _fluidOptions = new TemplateOptions();
        var parser = new FluidParser();
        parser.TryParse(TemplateSource, out _fluidTemplate!, out _);

        _handlebarsTemplate = Handlebars.Compile(TemplateSource);

        _stubble = new StubbleBuilder().Build();
        // warm up Stubble's internal parse cache
        _stubble.Render(TemplateSource, new { firstName = "Alex", lastName = "Smith" });
    }

    public string RenderStrongbars() => new SimpleGreeting("Alex", "Smith").Render();

    public string RenderScriban()
    {
        var obj = new ScriptObject { ["firstName"] = "Alex", ["lastName"] = "Smith" };
        return _scribanTemplate.Render(obj);
    }

    public string RenderFluid()
    {
        var ctx = new Fluid.TemplateContext(_fluidOptions);
        ctx.SetValue("firstName", "Alex");
        ctx.SetValue("lastName", "Smith");
        return _fluidTemplate.RenderAsync(ctx).GetAwaiter().GetResult();
    }

    public string RenderHandlebars() =>
        _handlebarsTemplate(new { firstName = "Alex", lastName = "Smith" });

    public string RenderStubble() =>
        _stubble.Render(TemplateSource, new { firstName = "Alex", lastName = "Smith" });
}
