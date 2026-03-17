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

        // Derive per-engine template strings. Each engine has its own dialect.
        _fluidSource = ToFluidSyntax(_strongbarsSource);
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

    // Shared compiled regex for Liquid-style block tags: {% keyword arg %}
    private static readonly Regex BlockTagPattern = new Regex(
        @"\{%\s*(\w+)(?:\s+(\w+))?\s*%\}",
        RegexOptions.Compiled
    );

    // {% if VAR %}...{% else %}...{% end %} → {% if VAR %}...{% else %}...{% endif %}
    // Fluid uses standard Liquid syntax with explicit {% endif %} / {% endunless %}
    private static string ToFluidSyntax(string source)
    {
        var sb = new System.Text.StringBuilder();
        var blockStack = new Stack<string>();
        var pos = 0;
        foreach (Match m in BlockTagPattern.Matches(source))
        {
            sb.Append(source, pos, m.Index - pos);
            pos = m.Index + m.Length;
            var keyword = m.Groups[1].Value;
            var arg = m.Groups[2].Value;
            switch (keyword)
            {
                case "if":
                    sb.Append($"{{% if {arg} %}}");
                    blockStack.Push("if");
                    break;
                case "unless":
                    sb.Append($"{{% unless {arg} %}}");
                    blockStack.Push("unless");
                    break;
                case "else":
                    sb.Append("{% else %}");
                    break;
                case "end":
                    var fluidType = blockStack.Count > 0 ? blockStack.Pop() : "if";
                    sb.Append($"{{% end{fluidType} %}}");
                    break;
                default:
                    sb.Append(m.Value);
                    break;
            }
        }
        sb.Append(source, pos, source.Length - pos);
        return sb.ToString();
    }

    // {% if VAR %}...{% else %}...{% end %} → {{ if VAR }}...{{ else }}...{{ end }}
    private static string ToScribanSyntax(string source)
    {
        source = Regex.Replace(source, @"\{%\s*if\s+(\w+)\s*%\}", "{{ if $1 }}");
        source = Regex.Replace(source, @"\{%\s*unless\s+(\w+)\s*%\}", "{{ unless $1 }}");
        source = Regex.Replace(source, @"\{%\s*else\s*%\}", "{{ else }}");
        source = Regex.Replace(source, @"\{%\s*end\s*%\}", "{{ end }}");
        return source;
    }

    // {% if VAR %}...{% else %}...{% end %} → {{#if VAR}}...{{else}}...{{/if}}
    // Uses a stack to emit the correct closing tag for each block type.
    private static string ToHandlebarsSyntax(string source)
    {
        var sb = new System.Text.StringBuilder();
        var blockStack = new Stack<string>();
        var pos = 0;
        foreach (Match m in BlockTagPattern.Matches(source))
        {
            sb.Append(source, pos, m.Index - pos);
            pos = m.Index + m.Length;
            var keyword = m.Groups[1].Value;
            var arg = m.Groups[2].Value;
            switch (keyword)
            {
                case "if":
                    sb.Append($"{{{{#if {arg}}}}}");
                    blockStack.Push("if");
                    break;
                case "unless":
                    sb.Append($"{{{{#unless {arg}}}}}");
                    blockStack.Push("unless");
                    break;
                case "else":
                    sb.Append("{{else}}");
                    break;
                case "end":
                    var hbsType = blockStack.Count > 0 ? blockStack.Pop() : "if";
                    sb.Append($"{{{{/{hbsType}}}}}");
                    break;
                default:
                    sb.Append(m.Value);
                    break;
            }
        }
        sb.Append(source, pos, source.Length - pos);
        return sb.ToString();
    }

    // {% if VAR %}...{% end %} → {{#VAR}}...{{/VAR}}
    // {% unless VAR %}...{% end %} → {{^VAR}}...{{/VAR}}
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
                $@"\{{%\s*if\s+{name}\s*%\}}([\s\S]*?)\{{%\s*end\s*%\}}",
                $"{{{{#{name}}}}}$1{{{{/{name}}}}}"
            );
            source = Regex.Replace(
                source,
                $@"\{{%\s*unless\s+{name}\s*%\}}([\s\S]*?)\{{%\s*end\s*%\}}",
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
