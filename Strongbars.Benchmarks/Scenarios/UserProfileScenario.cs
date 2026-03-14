using Fluid;
using HandlebarsDotNet;
using Scriban;
using Scriban.Runtime;
using Strongbars.Benchmarks.Templates;
using Stubble.Core.Builders;
using Stubble.Core;

namespace Strongbars.Benchmarks.Scenarios;

/// <summary>Six-variable substitution: name, email, role, department, location, bio.</summary>
public sealed class UserProfileScenario : ITemplateScenario
{
    public string Name => "UserProfile";

    private const string TemplateSource =
        """
        <div class="profile">
          <h1>{{ name }}</h1>
          <p class="email">{{ email }}</p>
          <p class="role">{{ role }} at {{ department }}</p>
          <p class="location">{{ location }}</p>
          <p class="bio">{{ bio }}</p>
        </div>
        """;

    private Scriban.Template _scribanTemplate = null!;
    private IFluidTemplate _fluidTemplate = null!;
    private TemplateOptions _fluidOptions = null!;
    private HandlebarsTemplate<object, object> _handlebarsTemplate = null!;
    private StubbleVisitorRenderer _stubble = null!;

    private static readonly object Data = new
    {
        name = "Alex Johnson",
        email = "alex@example.com",
        role = "Senior Engineer",
        department = "Platform",
        location = "Berlin, Germany",
        bio = "Building developer tools and open-source software."
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
        new UserProfile(
            "Alex Johnson",
            "alex@example.com",
            "Senior Engineer",
            "Platform",
            "Berlin, Germany",
            "Building developer tools and open-source software."
        ).Render();

    public string RenderScriban()
    {
        var obj = new ScriptObject
        {
            ["name"] = "Alex Johnson",
            ["email"] = "alex@example.com",
            ["role"] = "Senior Engineer",
            ["department"] = "Platform",
            ["location"] = "Berlin, Germany",
            ["bio"] = "Building developer tools and open-source software."
        };
        return _scribanTemplate.Render(obj);
    }

    public string RenderFluid()
    {
        var ctx = new Fluid.TemplateContext(_fluidOptions);
        ctx.SetValue("name", "Alex Johnson");
        ctx.SetValue("email", "alex@example.com");
        ctx.SetValue("role", "Senior Engineer");
        ctx.SetValue("department", "Platform");
        ctx.SetValue("location", "Berlin, Germany");
        ctx.SetValue("bio", "Building developer tools and open-source software.");
        return _fluidTemplate.RenderAsync(ctx).GetAwaiter().GetResult();
    }

    public string RenderHandlebars() => _handlebarsTemplate(Data);

    public string RenderStubble() => _stubble.Render(TemplateSource, Data);
}
