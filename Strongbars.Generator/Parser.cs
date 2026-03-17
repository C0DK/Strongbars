using System.Text.RegularExpressions;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public class Parser
{
    public static ITemplateNode Parse(string fileContent)
    {
        int lastIndex = 0;
        var tokens = new List<ITemplateNode>();

        foreach (Match match in ExpressionRegex.Matches(fileContent))
        {
            // Literal text before this variable slot
            if (match.Index > lastIndex)
                tokens.Add(
                    new LiteralTemplateToken(
                        fileContent.Substring(lastIndex, match.Index - lastIndex)
                    )
                );

            if (match.Groups[1].Success)
            {
                var content = match.Groups[1].Value;
                var argumentMatch = ArgumentRegex.Match(content);
                tokens.Add(
                    new VariableTemplateToken(
                        new Variable(
                            name: argumentMatch.Groups[2].Value,
                            type: VariableType.TemplateArgument,
                            array: argumentMatch.Groups[1].Success,
                            optional: argumentMatch.Groups[3].Success
                        )
                    )
                );
            }
            else if (match.Groups[2].Success)
            {
                throw new NotImplementedException("CLAUDE FIX THIS");
            }
            lastIndex = match.Index + match.Length;
        }

        // Any trailing literal after the last variable
        if (lastIndex < fileContent.Length)
            tokens.Add(new LiteralTemplateToken(fileContent.Substring(lastIndex)));

        return new CompositeTemplateToken(tokens);
    }

    public static Regex ExpressionRegex = new Regex(
        @"(\{\{.*\}\})|({%.*%})",
        RegexOptions.Compiled
    );

    public static Regex ArgumentRegex = new Regex(
        @"\{\{\s*(\.{2})?([a-zA-Z]\w*)(\?)?\s*\}\}",
        RegexOptions.Compiled
    );
}
