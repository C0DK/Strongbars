using Pages;

Console.WriteLine("Raw:");
Console.WriteLine(Paragraph.Raw);
Console.WriteLine("Arguments:");
foreach (var a in Paragraph.Arguments)
{
    Console.WriteLine($"- '{a}'");
}
Console.WriteLine("Render:");
Console.WriteLine(Paragraph.Render(content: "test"));

Console.WriteLine(Name.Render(firstName: "Casper", lastName: "Bang"));
