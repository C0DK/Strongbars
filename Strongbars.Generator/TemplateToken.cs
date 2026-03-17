using System.Text;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public interface ITemplateToken
{
    public string GenerateRenderer();
    public IEnumerable<Variable> GetVariables();
}

public class LiteralTemplateToken : ITemplateToken
{
public LiteralTemplateToken(string content){
        Content = content;
    }

    public string Content { get; }

    public string GenerateRenderer()
    {
        return ""
        builder.Append(Content);
    }

    public IEnumerable<Variable> GetVariables()
    {
        yield break;
    }
}

public class VariableTemplateToken : ITemplateToken
{
public VariableTemplateToken(Variable variable) {
        Variable = variable;
    }

    public Variable Variable { get; }

    public void GenerateRenderer(StringBuilder builder)
    {
        builder.Append(content);
    }

    public IEnumerable<Variable> GetVariables()
    {
        yield return variable;
    }
}
