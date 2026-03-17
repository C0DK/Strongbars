using Strongbars.Abstractions;

namespace Strongbars.Generator;
public interface ITemplateNode
{
    public string GenerateRenderer();
    public IEnumerable<Variable> GetVariables();
}

public class CompositeTemplateToken : ITemplateNode
{
    public CompositeTemplateToken(IReadOnlyList<ITemplateNode> tokens)
    {
        Tokens = tokens;
    }

    public IReadOnlyList<ITemplateNode> Tokens { get; }

    public string GenerateRenderer()
    {
        return string.Join(" + ", Tokens.Select(token => token.GenerateRenderer()));
    }

    public IEnumerable<Variable> GetVariables()
    {
        foreach (var token in Tokens)
        foreach (var var in token.GetVariables())
            yield return var;
    }
}

public class LiteralTemplateToken : ITemplateNode
{
    public LiteralTemplateToken(string content)
    {
        Content = content;
    }

    public string Content { get; }

    public string GenerateRenderer()
    {
        return "@\"" + escape(Content) + "\"";
    }

    private static string escape(string v) => v.Replace("\"", "\"\"");

    public IEnumerable<Variable> GetVariables()
    {
        yield break;
    }
}

public class VariableTemplateToken : ITemplateNode
{
    public VariableTemplateToken(Variable variable)
    {
        if (variable.Type is VariableType.Bool)
            throw new ArgumentException("Cannot use bool as variable");
        Variable = variable;
    }

    public Variable Variable { get; }

    public string GenerateRenderer()
    {
        return (Variable.Optional ? $"_{Variable.Name} is null ? \"\" : " : "")
            + (
                Variable.Array
                    ? $"string.Join(\" \", _{Variable.Name}.Select(item => {RenderExpression("item", Variable.Type)}))"
                    : RenderExpression("_" + Variable.Name, Variable.Type)
            );
    }

    public IEnumerable<Variable> GetVariables()
    {
        yield return Variable;
    }

    private static string RenderExpression(string varName, VariableType type) =>
        type switch
        {
            VariableType.String => $"{varName}.ToString()",
            VariableType.IFormattable => $"{varName}.ToString()",
            VariableType.TemplateArgument => $"{varName}.Render()",
            _ => throw new ArgumentOutOfRangeException(),
        };
}

public class ConditionalTemplateToken : ITemplateNode
{
    public Variable Conditional { get; }
    public ITemplateNode IfTrue { get; }
    public ITemplateNode IfFalse { get; }

    public ConditionalTemplateToken(
        Variable conditional,
        ITemplateNode ifTrue,
        ITemplateNode ifFalse
    )
    {
        Conditional = conditional;
        IfTrue = ifTrue;
        IfFalse = ifFalse;
    }

    public string GenerateRenderer()
    {
        var truthy = Conditional switch
        {
            { Array: true } => $"_{Conditional.Name}.Count() > 0",
            { Type: VariableType.Bool } => $"_{Conditional.Name} is true",
            { Type: VariableType.String } => $"!string.IsNullOrWhiteSpace(_{Conditional.Name})",
            _ => throw new NotImplementedException(),
        };
        return $"({truthy} ? {IfTrue.GenerateRenderer()} : {IfFalse.GenerateRenderer()})";
    }

    public IEnumerable<Variable> GetVariables()
    {
        yield return Conditional;
        foreach (var var in IfTrue.GetVariables())
            yield return var;

        foreach (var var in IfFalse.GetVariables())
            yield return var;
    }

    private static string RenderExpression(string varName, VariableType type) =>
        type switch
        {
            VariableType.String => $"{varName}.ToString()",
            VariableType.IFormattable => $"{varName}.ToString()",
            VariableType.TemplateArgument => $"{varName}.Render()",
            _ => throw new ArgumentOutOfRangeException(),
        };
}
