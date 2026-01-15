using Pages;

Console.WriteLine("Template:");
Console.WriteLine(Paragraph.Template);
Console.WriteLine("Arguments:");
foreach (var a in Paragraph.Arguments)
{
    Console.WriteLine($"- '{a}'");
}
Console.WriteLine("Render:");
Console.WriteLine(new Paragraph(content: "test").Render());

Console.WriteLine(new Name(firstName: "Casper", lastName: "Bang").Render());
