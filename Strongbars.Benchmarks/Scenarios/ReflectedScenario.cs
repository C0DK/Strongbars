using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Fluid;
using HandlebarsDotNet;
using Scriban;
using Scriban.Runtime;
using Stubble.Core;
using Stubble.Core.Builders;
using SbTemplate = Strongbars.Abstractions.Template;

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
    private readonly string _strongbarsSource;
    private readonly string _scribanSource;
    private readonly string _fluidSource;
    private readonly string _handlebarsSource;
    private readonly string _stubbleSource;
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
        _strongbarsSource = (string)
            templateType
                .GetField("Template", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

        // The generator emits a public static Variable[] Variables on every class.
        var variables = (Strongbars.Abstractions.Variable[])
            templateType
                .GetField("Variables", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

        // Build sample data: variable name → "Sample{Name}" string (or true for bools).
        _data = variables.ToDictionary(
            v => v.Name,
            v =>
                v.Type == Strongbars.Abstractions.VariableType.Bool
                    ? (object)true
                    : (object)$"Sample{v.Name}"
        );

        // Pre-compile a zero-reflection ctor factory so the hot path is fast.
        _factory = BuildFactory(templateType, variables);

        // Derive per-engine template strings. Fluid uses the same Liquid syntax
        // as Strongbars; Scriban, Handlebars, and Stubble need their own dialects.
        _fluidSource = _strongbarsSource; // identical: {% if %}...{% endif %}
        _scribanSource = ToScribanSyntax(_strongbarsSource);
        _handlebarsSource = ToHandlebarsSyntax(_strongbarsSource);
        _stubbleSource = ToStubbleSyntax(_strongbarsSource, variables);
    }

    private static Func<SbTemplate> BuildFactory(
        Type type,
        Strongbars.Abstractions.Variable[] variables
    )
    {
        var ctor = type.GetConstructors()[0];
        var argExprs = variables.Select(v =>
            v.Type == Strongbars.Abstractions.VariableType.Bool
                ? Expression.Constant(true, typeof(bool))
                : (Expression)
                    Expression.Constant(
                        (Strongbars.Abstractions.TemplateArgument)$"Sample{v.Name}",
                        typeof(Strongbars.Abstractions.TemplateArgument)
                    )
        );

        var newExpr = Expression.New(ctor, argExprs);
        return Expression.Lambda<Func<SbTemplate>>(newExpr).Compile();
    }

    // {% if VAR %}...{% else %}...{% endif %} → {{ if VAR }}...{{ else }}...{{ end }}
    private static string ToScribanSyntax(string source)
    {
        source = Regex.Replace(source, @"\{%\s*if\s+(\w+)\s*%\}", "{{ if $1 }}");
        source = Regex.Replace(source, @"\{%\s*unless\s+(\w+)\s*%\}", "{{ unless $1 }}");
        source = Regex.Replace(source, @"\{%\s*else\s*%\}", "{{ else }}");
        source = Regex.Replace(source, @"\{%\s*endif\s*%\}", "{{ end }}");
        source = Regex.Replace(source, @"\{%\s*endunless\s*%\}", "{{ end }}");
        return source;
    }

    // {% if VAR %}...{% else %}...{% endif %} → {{#if VAR}}...{{else}}...{{/if}}
    private static string ToHandlebarsSyntax(string source)
    {
        source = Regex.Replace(source, @"\{%\s*if\s+(\w+)\s*%\}", "{{#if $1}}");
        source = Regex.Replace(source, @"\{%\s*unless\s+(\w+)\s*%\}", "{{#unless $1}}");
        source = Regex.Replace(source, @"\{%\s*else\s*%\}", "{{else}}");
        source = Regex.Replace(source, @"\{%\s*endif\s*%\}", "{{/if}}");
        source = Regex.Replace(source, @"\{%\s*endunless\s*%\}", "{{/unless}}");
        return source;
    }

    // {% if VAR %}...{% endif %} → {{#VAR}}...{{/VAR}}
    // {% unless VAR %}...{% endunless %} → {{^VAR}}...{{/VAR}}
    private static string ToStubbleSyntax(
        string source,
        Strongbars.Abstractions.Variable[] variables
    )
    {
        foreach (
            var name in variables
                .Where(v => v.Type == Strongbars.Abstractions.VariableType.Bool)
                .Select(v => v.Name)
        )
        {
            source = Regex.Replace(
                source,
                $@"\{{%\s*if\s+{name}\s*%\}}([\s\S]*?)\{{%\s*endif\s*%\}}",
                $"{{{{#{name}}}}}$1{{{{/{name}}}}}"
            );
            source = Regex.Replace(
                source,
                $@"\{{%\s*unless\s+{name}\s*%\}}([\s\S]*?)\{{%\s*endunless\s*%\}}",
                $"{{{{^{name}}}}}$1{{{{/{name}}}}}"
            );
        }
        return source;
    }

    public void Setup()
    {
        _scribanTemplate = Scriban.Template.Parse(_scribanSource);

        _fluidOptions = new TemplateOptions();
        var parser = new FluidParser();
        parser.TryParse(_fluidSource, out _fluidTemplate!, out _);

        _handlebarsTemplate = Handlebars.Compile(_handlebarsSource);

        _stubble = new StubbleBuilder().Build();
        _stubble.Render(_stubbleSource, _data); // warm up parse cache
    }

    public string RenderStrongbars() => _factory().Render();

    public string RenderScriban()
    {
        var obj = new ScriptObject();
        foreach (var (k, v) in _data)
            obj[k] = v;
        return _scribanTemplate.Render(obj);
    }

    public string RenderFluid()
    {
        var ctx = new Fluid.TemplateContext(_fluidOptions);
        foreach (var (k, v) in _data)
        {
            if (v is bool b)
                ctx.SetValue(k, b);
            else
                ctx.SetValue(k, (string)v);
        }
        return _fluidTemplate.RenderAsync(ctx).GetAwaiter().GetResult();
    }

    public string RenderHandlebars() => _handlebarsTemplate(_data);

    public string RenderStubble() => _stubble.Render(_stubbleSource, _data);
}
