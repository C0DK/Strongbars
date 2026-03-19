using Strongbars.Abstractions;

namespace Strongbars.Generator;

public interface ITemplateNode
{
    public string GenerateRenderExpression();
    public IEnumerable<Variable> GetVariables();
}

public class CompositeTemplateNode : ITemplateNode
{
    public CompositeTemplateNode(IReadOnlyList<ITemplateNode> nodes)
    {
        Nodes = nodes;
    }

    public IReadOnlyList<ITemplateNode> Nodes { get; }

    public string GenerateRenderExpression()
    {
        if (Nodes.Count == 0)
            return "\"\"";

        return string.Join(" + ", Nodes.Select(node => node.GenerateRenderExpression()));
    }

    public IEnumerable<Variable> GetVariables()
    {
        foreach (var node in Nodes)
        foreach (var var in node.GetVariables())
            yield return var;
    }
}

public class LiteralTemplateNode : ITemplateNode
{
    public LiteralTemplateNode(string content)
    {
        Content = content;
    }

    public string Content { get; }

    public string GenerateRenderExpression()
    {
        return "@\"" + Escape(Content) + "\"";
    }

    private static string Escape(string v) => v.Replace("\"", "\"\"");

    public IEnumerable<Variable> GetVariables()
    {
        yield break;
    }
}

public class VariableTemplateNode : ITemplateNode
{
    public VariableTemplateNode(Variable variable)
    {
        if (variable.Type is VariableType.Bool)
            throw new ArgumentException("Cannot use bool as variable");
        Variable = variable;
    }

    public Variable Variable { get; }

    public string GenerateRenderExpression()
    {
        return "("
            + (Variable.Optional ? $"_{Variable.Name} is null ? \"\" : " : "")
            + (
                Variable.Array
                    ? $"string.Join(\" \", _{Variable.Name}?.Select(item => {RenderExpression("item", Variable.Type)}) ?? [])"
                    : RenderExpression("_" + Variable.Name, Variable.Type)
            )
            + ")";
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

public class ConditionalTemplateNode : ITemplateNode
{
    public Variable Conditional { get; }
    public ITemplateNode IfTrue { get; }
    public ITemplateNode IfFalse { get; }

    public ConditionalTemplateNode(
        Variable conditional,
        ITemplateNode ifTrue,
        ITemplateNode ifFalse
    )
    {
        Conditional = conditional;
        IfTrue = ifTrue;
        IfFalse = ifFalse;
    }

    public string GenerateRenderExpression()
    {
        var truthy = Conditional switch
        {
            { Array: true } => $"_{Conditional.Name}.Count() > 0",
            { Type: VariableType.Bool } => $"_{Conditional.Name} is true",
            { Type: VariableType.String } => $"!string.IsNullOrWhiteSpace(_{Conditional.Name})",
            _ => throw new NotImplementedException(),
        };
        return $"({truthy} ? {IfTrue.GenerateRenderExpression()} : {IfFalse.GenerateRenderExpression()})";
    }

    public IEnumerable<Variable> GetVariables()
    {
        yield return Conditional;
        foreach (var var in IfTrue.GetVariables())
            yield return var;

        foreach (var var in IfFalse.GetVariables())
            yield return var;
    }
}
