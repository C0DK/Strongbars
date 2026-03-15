using Fluid;
using HandlebarsDotNet;
using Scriban;
using Scriban.Runtime;
using Strongbars.Benchmarks.Templates;
using Stubble.Core;
using Stubble.Core.Builders;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>Four-variable substitution: title, author, date, summary.</summary>
public sealed class ArticleCardScenario : ITemplateScenario
{
    public string Name => "ArticleCard";

    private const string TemplateSource = """
        <div class="card">
          <h2>{{ title }}</h2>
          <p class="author">By {{ author }}</p>
          <time>{{ date }}</time>
          <p class="summary">{{ summary }}</p>
        </div>
        """;

    private Scriban.Template _scribanTemplate = null!;
    private IFluidTemplate _fluidTemplate = null!;
    private TemplateOptions _fluidOptions = null!;
    private HandlebarsTemplate<object, object> _handlebarsTemplate = null!;
    private StubbleVisitorRenderer _stubble = null!;

    private static readonly object Data = new
    {
        title = "10 Tips for Better C#",
        author = "Jane Doe",
        date = "2025-03-01",
        summary = "Practical advice for writing clean, maintainable C# code.",
    };

    public void Setup()
    {
        _scribanTemplate = Scriban.Template.Parse(TemplateSource);

        _fluidOptions = new TemplateOptions();
        var parser = new FluidParser();
        parser.TryParse(TemplateSource, out _fluidTemplate!, out _);

        _handlebarsTemplate = Handlebars.Compile(TemplateSource);

        _stubble = new StubbleBuilder().Build();
        _stubble.Render(TemplateSource, Data);
    }

    public string RenderStrongbars() =>
        new ArticleCard(
            "10 Tips for Better C#",
            "Jane Doe",
            "2025-03-01",
            "Practical advice for writing clean, maintainable C# code."
        ).Render();

    public string RenderScriban()
    {
        var obj = new ScriptObject
        {
            ["title"] = "10 Tips for Better C#",
            ["author"] = "Jane Doe",
            ["date"] = "2025-03-01",
            ["summary"] = "Practical advice for writing clean, maintainable C# code.",
        };
        return _scribanTemplate.Render(obj);
    }

    public string RenderFluid()
    {
        var ctx = new Fluid.TemplateContext(_fluidOptions);
        ctx.SetValue("title", "10 Tips for Better C#");
        ctx.SetValue("author", "Jane Doe");
        ctx.SetValue("date", "2025-03-01");
        ctx.SetValue("summary", "Practical advice for writing clean, maintainable C# code.");
        return _fluidTemplate.RenderAsync(ctx).GetAwaiter().GetResult();
    }

    public string RenderHandlebars() => _handlebarsTemplate(Data);

    public string RenderStubble() => _stubble.Render(TemplateSource, Data);
}
