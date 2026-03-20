using Strongbars.Abstractions;
using Strongbars.Generator;

namespace Strongbars.Tests;

public class ParserConditionalTests
{
    private static ITemplateNode Parse(string content) => new Parser("test", content).Parse();

    [Test]
    public void IfBlock_ProducesConditionalNode()
    {
        var node = (CompositeTemplateNode)Parse("{% if active %}yes{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();

        Assert.That(cond.Conditional.Name, Is.EqualTo("active"));
        Assert.That(cond.Conditional.Type, Is.EqualTo(VariableType.Bool));
    }

    [Test]
    public void IfBlock_IfTrueContainsLiteral()
    {
        var node = (CompositeTemplateNode)Parse("{% if active %}yes{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();
        var ifTrue = (CompositeTemplateNode)cond.IfTrue;

        Assert.That(ifTrue.Nodes.OfType<LiteralTemplateNode>().Single().Content, Is.EqualTo("yes"));
    }

    [Test]
    public void IfBlock_IfFalseIsEmptyComposite()
    {
        var node = (CompositeTemplateNode)Parse("{% if active %}yes{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();
        var ifFalse = (CompositeTemplateNode)cond.IfFalse;

        Assert.That(ifFalse.Nodes, Is.Empty);
    }

    [Test]
    public void IfElseBlock_BothBranchesPopulated()
    {
        var node = (CompositeTemplateNode)Parse("{% if enabled %}on{% else %}off{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();

        var ifTrue = (CompositeTemplateNode)cond.IfTrue;
        Assert.That(ifTrue.Nodes.OfType<LiteralTemplateNode>().Single().Content, Is.EqualTo("on"));

        var ifFalse = (CompositeTemplateNode)cond.IfFalse;
        Assert.That(
            ifFalse.Nodes.OfType<LiteralTemplateNode>().Single().Content,
            Is.EqualTo("off")
        );
    }

    [Test]
    public void UnlessBlock_InvertsIfTrueAndIfFalse()
    {
        // {% unless x %}body{% end %}: condition=x, IfTrue="" (shown when x=true), IfFalse="body"
        var node = (CompositeTemplateNode)Parse("{% unless x %}body{% end %}");
        var cond = (ConditionalTemplateNode)node.Nodes.Single();

        // IfTrue (rendered when condition is true) is empty for "unless"
        var ifTrue = (CompositeTemplateNode)cond.IfTrue;
        Assert.That(ifTrue.Nodes, Is.Empty);

        // IfFalse (rendered when condition is false) contains the body
        var ifFalse = (CompositeTemplateNode)cond.IfFalse;
        Assert.That(
            ifFalse.Nodes.OfType<LiteralTemplateNode>().Single().Content,
            Is.EqualTo("body")
        );
    }
}
