using System.Text;
using Fluid;
using HandlebarsDotNet;
using Scriban;
using Scriban.Runtime;
using Strongbars.Benchmarks.Templates;
using Stubble.Core;
using Stubble.Core.Builders;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>
/// List rendering with 10 items.
/// Each engine uses its idiomatic loop syntax; Strongbars uses template composition
/// (rendering each ListItem and joining them – the compile-time equivalent of a loop).
/// </summary>
public sealed class ListScenario : ITemplateScenario
{
    public string Name => "List10Items";

    private static readonly string[] Items = Enumerable
        .Range(1, 10)
        .Select(i => $"Item {i}")
        .ToArray();

    // Engine-specific loop templates
    private const string ScribanSource =
        "<ul>{{ for item in items }}<li>{{ item }}</li>{{ end }}</ul>\n";

    private const string FluidSource =
        "<ul>{% for item in items %}<li>{{ item }}</li>{% endfor %}</ul>\n";

    private const string HandlebarsSource = "<ul>{{#each items}}<li>{{this}}</li>{{/each}}</ul>\n";

    private const string StubbleSource = "<ul>{{#items}}<li>{{.}}</li>{{/items}}</ul>\n";

    private Scriban.Template _scribanTemplate = null!;
    private IFluidTemplate _fluidTemplate = null!;
    private TemplateOptions _fluidOptions = null!;
    private HandlebarsTemplate<object, object> _handlebarsTemplate = null!;
    private StubbleVisitorRenderer _stubble = null!;

    public void Setup()
    {
        _scribanTemplate = Scriban.Template.Parse(ScribanSource);

        _fluidOptions = new TemplateOptions();
        var parser = new FluidParser();
        parser.TryParse(FluidSource, out _fluidTemplate!, out _);

        _handlebarsTemplate = Handlebars.Compile(HandlebarsSource);

        _stubble = new StubbleBuilder().Build();
        _stubble.Render(StubbleSource, new { items = Items });
    }

    /// <summary>
    /// Strongbars has no runtime loop construct; instead, you compose a list by
    /// rendering each ListItem and joining the results – a compile-time loop.
    /// </summary>
    public string RenderStrongbars()
    {
        var sb = new StringBuilder("<ul>");
        foreach (var item in Items)
            sb.Append(new ListItem(item).Render());
        sb.Append("</ul>\n");
        return sb.ToString();
    }

    public string RenderScriban()
    {
        var obj = new ScriptObject { ["items"] = Items };
        return _scribanTemplate.Render(obj);
    }

    public string RenderFluid()
    {
        var ctx = new Fluid.TemplateContext(_fluidOptions);
        ctx.SetValue("items", Items);
        return _fluidTemplate.RenderAsync(ctx).GetAwaiter().GetResult();
    }

    public string RenderHandlebars() => _handlebarsTemplate(new { items = Items });

    public string RenderStubble() => _stubble.Render(StubbleSource, new { items = Items });
}
