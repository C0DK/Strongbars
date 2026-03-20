using Strongbars.Abstractions;
using Strongbars.Tests.Output;

namespace Strongbars.Tests;

public class FileGeneratorConditionalTests
{
    [Test]
    public void ConditionalVariablesHaveCorrectMetadata()
    {
        Assert.That(
            Message.Variables,
            Is.EquivalentTo([
                new Variable("urgent", VariableType.Bool, array: false, optional: false),
                new Variable(
                    "message",
                    VariableType.TemplateArgument,
                    array: false,
                    optional: false
                ),
            ])
        );
    }

    [Test]
    public void ConditionalRendersContentWhenTrue()
    {
        var template = new Message(urgent: true, message: "Hello!");

        Assert.That(template.Render(), Does.Contain("urgent"));
        Assert.That(template.Render(), Does.Contain("Hello!"));
    }

    [Test]
    public void ConditionalRendersEmptyWhenFalse()
    {
        var template = new Message(urgent: false, message: "Hello!");

        Assert.That(template.Render(), Does.Not.Contain("urgent"));
        Assert.That(template.Render(), Does.Contain("Hello!"));
    }

    [Test]
    public void ConditionalFullRenderMatchesExpected()
    {
        Assert.That(
            new Message(urgent: true, message: "Buy now!").Render(),
            Is.EqualTo(@"<div class=""message urgent"">Buy now!</div>").IgnoreWhiteSpace
        );
        Assert.That(
            new Message(urgent: false, message: "Buy now!").Render(),
            Is.EqualTo(@"<div class=""message "">Buy now!</div>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void UnlessVariablesHaveCorrectMetadata()
    {
        Assert.That(
            Status.Variables,
            Is.EquivalentTo([
                new Variable("inactive", VariableType.Bool, array: false, optional: false),
                new Variable("label", VariableType.TemplateArgument, array: false, optional: false),
            ])
        );
    }

    [Test]
    public void UnlessRendersContentWhenFalse()
    {
        var template = new Status(inactive: false, label: "Online");

        Assert.That(template.Render(), Does.Contain("active"));
        Assert.That(template.Render(), Does.Contain("Online"));
    }

    [Test]
    public void UnlessRendersEmptyWhenTrue()
    {
        var template = new Status(inactive: true, label: "Offline");

        Assert.That(template.Render(), Does.Not.Contain("active"));
        Assert.That(template.Render(), Does.Contain("Offline"));
    }

    [Test]
    public void UnlessFullRenderMatchesExpected()
    {
        Assert.That(
            new Status(inactive: false, label: "Online").Render(),
            Is.EqualTo(@"<span class=""status active"">Online</span>").IgnoreWhiteSpace
        );
        Assert.That(
            new Status(inactive: true, label: "Offline").Render(),
            Is.EqualTo(@"<span class=""status "">Offline</span>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void IfElseRendersElseBranchWhenFalse()
    {
        Assert.That(
            new Toggle(enabled: true, label: "Go").Render(),
            Is.EqualTo(@"<button class=""enabled"">Go</button>").IgnoreWhiteSpace
        );
        Assert.That(
            new Toggle(enabled: false, label: "Go").Render(),
            Is.EqualTo(@"<button class=""disabled"">Go</button>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void UnlessElseRendersElseBranchWhenTrue()
    {
        Assert.That(
            new Subscription(premium: false).Render(),
            Is.EqualTo("<p>Free tier</p>").IgnoreWhiteSpace
        );
        Assert.That(
            new Subscription(premium: true).Render(),
            Is.EqualTo("<p>Premium member</p>").IgnoreWhiteSpace
        );
    }

    [Test]
    public void ElseBranchDoesNotAffectTemplatesWithoutElse()
    {
        // Existing templates without {% else %} must still behave identically
        Assert.That(
            new Message(urgent: false, message: "All good").Render(),
            Is.EqualTo(@"<div class=""message "">All good</div>").IgnoreWhiteSpace
        );
        Assert.That(
            new Status(inactive: true, label: "Offline").Render(),
            Is.EqualTo(@"<span class=""status "">Offline</span>").IgnoreWhiteSpace
        );
    }
}
