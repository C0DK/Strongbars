using Pages;
using Pages.Components;

Console.WriteLine("Template:");
Console.WriteLine(Paragraph.Template);
Console.WriteLine("Arguments:");
foreach (var a in Paragraph.Variables)
{
    Console.WriteLine($"- '{a}'");
}
Console.WriteLine("Render:");
Console.WriteLine(new Paragraph(content: "test").Render());

var template = new Name(firstName: "Alex", lastName: "Smith");
Console.WriteLine(template.Render());

Console.WriteLine("\nConditional example:");
Console.WriteLine(new Message(urgent: true, message: "Server is down!").Render());
Console.WriteLine(new Message(urgent: false, message: "All systems nominal.").Render());

// Pages/Components/Alert.html is in namespace Pages.Components via subdirectory namespacing
Console.WriteLine("\nSubdirectory namespace example (Pages.Components.Alert):");
Console.WriteLine(new Alert(dismissible: true, text: "This is a dismissible alert.").Render());
Console.WriteLine(new Alert(dismissible: false, text: "This is a permanent alert.").Render());
