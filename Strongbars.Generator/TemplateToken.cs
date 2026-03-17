using System;
using System.Collections.Generic;
using System.Linq;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public interface ITemplateNode
{
    void GenerateStatements(List<string> statements);

    IEnumerable<Variable> GetVariables();
}

public class CompositeTemplateToken : ITemplateNode
{
    public CompositeTemplateToken(IReadOnlyList<ITemplateNode> tokens)
    {
        Tokens = tokens;
    }

    public IReadOnlyList<ITemplateNode> Tokens { get; }

    public void GenerateStatements(List<string> statements)
    {
        foreach (var token in Tokens)
            token.GenerateStatements(statements);
    }

    public IEnumerable<Variable> GetVariables()
    {
        foreach (var token in Tokens)
        foreach (var v in token.GetVariables())
            yield return v;
    }
}

public class LiteralTemplateToken : ITemplateNode
{
    public LiteralTemplateToken(string content)
    {
        Content = content;
    }

    public string Content { get; }

    public void GenerateStatements(List<string> statements)
    {
        if (!string.IsNullOrEmpty(Content))
            statements.Add($"_sb.Append(@\"{Escape(Content)}\");");
    }

    private static string Escape(string v) => v.Replace("\"", "\"\"");

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

    public void GenerateStatements(List<string> statements)
    {
        var expr = Variable.Array
            ? $"string.Join(\" \", _{Variable.Name}.Select(item => {RenderExpression("item", Variable.Type)}))"
            : RenderExpression($"_{Variable.Name}", Variable.Type);

        if (Variable.Optional)
        {
            statements.Add($"if (_{Variable.Name} != null)");
            statements.Add($"    _sb.Append({expr});");
        }
        else
        {
            statements.Add($"_sb.Append({expr});");
        }
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

    /// <summary>True for <c>{% unless %}</c> blocks; false for <c>{% if %}</c> blocks.</summary>
    public bool Inverted { get; }

    public ConditionalTemplateToken(
        Variable conditional,
        ITemplateNode ifTrue,
        ITemplateNode ifFalse,
        bool inverted = false
    )
    {
        Conditional = conditional;
        IfTrue = ifTrue;
        IfFalse = ifFalse;
        Inverted = inverted;
    }

    public void GenerateStatements(List<string> statements)
    {
        var condition = Inverted ? $"!_{Conditional.Name}" : $"_{Conditional.Name}";

        statements.Add($"if ({condition})");
        statements.Add("{");
        IfTrue.GenerateStatements(statements);
        statements.Add("}");

        var elseStatements = new List<string>();
        IfFalse.GenerateStatements(elseStatements);
        if (elseStatements.Count > 0)
        {
            statements.Add("else");
            statements.Add("{");
            statements.AddRange(elseStatements);
            statements.Add("}");
        }
    }

    public IEnumerable<Variable> GetVariables()
    {
        yield return Conditional;
        foreach (var v in IfTrue.GetVariables())
            yield return v;
        foreach (var v in IfFalse.GetVariables())
            yield return v;
    }
}
