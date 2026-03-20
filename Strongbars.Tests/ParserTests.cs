using Strongbars.Abstractions;
using Strongbars.Generator;

namespace Strongbars.Tests;

/// <summary>
/// Unit tests for <see cref="Parser"/> in isolation, verifying the AST it builds.
/// </summary>
public class ParserTests
{
    private static ITemplateNode Parse(string content) => new Parser("test", content).Parse();

    [Test]
    public void PlainText_ReturnsSingleLiteralNode()
    {
        var node = Parse("hello world");

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo("hello world"));
    }

    [Test]
    public void EmptyTemplate_ReturnsSingleLiteralNode()
    {
        var node = Parse("");

        Assert.That(node, Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(((LiteralTemplateNode)node).Content, Is.EqualTo(""));
    }

    [Test]
    public void SingleVariable_ReturnsCompositeWithVariableNode()
    {
        var node = (CompositeTemplateNode)Parse("{{name}}");
        var variable = (VariableTemplateNode)node.Nodes.Single();

        Assert.That(variable.Variable.Name, Is.EqualTo("name"));
        Assert.That(variable.Variable.Array, Is.False);
        Assert.That(variable.Variable.Optional, Is.False);
    }

    [Test]
    public void OptionalVariable_SetsOptionalFlag()
    {
        var node = (CompositeTemplateNode)Parse("{{foo?}}");
        var variable = (VariableTemplateNode)node.Nodes.Single();

        Assert.That(variable.Variable.Name, Is.EqualTo("foo"));
        Assert.That(variable.Variable.Optional, Is.True);
    }

    [Test]
    public void ArrayVariable_SetsArrayFlag()
    {
        var node = (CompositeTemplateNode)Parse("{{..items}}");
        var variable = (VariableTemplateNode)node.Nodes.Single();

        Assert.That(variable.Variable.Name, Is.EqualTo("items"));
        Assert.That(variable.Variable.Array, Is.True);
    }

    [Test]
    public void VariableWithSurroundingLiterals_ProducesThreeNodes()
    {
        var node = (CompositeTemplateNode)Parse("Hello {{name}}!");

        Assert.That(node.Nodes, Has.Count.EqualTo(3));
        Assert.That(node.Nodes[0], Is.InstanceOf<LiteralTemplateNode>());
        Assert.That(node.Nodes[1], Is.InstanceOf<VariableTemplateNode>());
        Assert.That(node.Nodes[2], Is.InstanceOf<LiteralTemplateNode>());
    }

    [Test]
    public void MultipleVariables_AllParsed()
    {
        var node = (CompositeTemplateNode)Parse("{{first}} {{second}}");
        var variables = node.Nodes.OfType<VariableTemplateNode>().ToArray();

        Assert.That(variables, Has.Length.EqualTo(2));
        Assert.That(variables[0].Variable.Name, Is.EqualTo("first"));
        Assert.That(variables[1].Variable.Name, Is.EqualTo("second"));
    }

    [Test]
    public void GetVariables_ReturnsAllVariablesIncludingConditionals()
    {
        var node = Parse("{% if active %}{{message}}{% end %}");
        var names = node.GetVariables().Select(v => v.Name).ToArray();

        Assert.That(names, Is.EquivalentTo(new[] { "active", "message" }));
    }
}
