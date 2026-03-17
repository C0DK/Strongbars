using System.Collections.Generic;
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
            // Literal text before this match
            if (match.Index > lastIndex)
                tokens.Add(
                    new LiteralTemplateToken(
                        fileContent.Substring(lastIndex, match.Index - lastIndex)
                    )
                );

            if (match.Groups[1].Success)
            {
                // {{variable}} token
                var argumentMatch = ArgumentRegex.Match(match.Groups[1].Value);
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
                // {% if foo %}...{% endif %} block
                var ifMatch = Template.ConditionalRegex.Match(match.Value);
                tokens.Add(
                    new ConditionalTemplateToken(
                        new Variable(
                            ifMatch.Groups[1].Value,
                            VariableType.Bool,
                            array: false,
                            optional: false
                        ),
                        Parse(ifMatch.Groups[2].Value),
                        Parse(ifMatch.Groups[3].Value),
                        inverted: false
                    )
                );
            }
            else if (match.Groups[3].Success)
            {
                // {% unless foo %}...{% endunless %} block
                var unlessMatch = Template.UnlessRegex.Match(match.Value);
                tokens.Add(
                    new ConditionalTemplateToken(
                        new Variable(
                            unlessMatch.Groups[1].Value,
                            VariableType.Bool,
                            array: false,
                            optional: false
                        ),
                        Parse(unlessMatch.Groups[2].Value),
                        Parse(unlessMatch.Groups[3].Value),
                        inverted: true
                    )
                );
            }

            lastIndex = match.Index + match.Length;
        }

        // Any trailing literal after the last match
        if (lastIndex < fileContent.Length)
            tokens.Add(new LiteralTemplateToken(fileContent.Substring(lastIndex)));

        return new CompositeTemplateToken(tokens);
    }

    // Group 1: {{variable}}
    // Group 2: {% if foo %}...{% endif %} (full block)
    // Group 3: {% unless foo %}...{% endunless %} (full block)
    public static readonly Regex ExpressionRegex = new Regex(
        @"(\{\{[^}]*\}\})"
            + @"|(\{%\s*if\s+[a-zA-Z]\w*\s*%\}[\s\S]*?\{%\s*endif\s*%\})"
            + @"|(\{%\s*unless\s+[a-zA-Z]\w*\s*%\}[\s\S]*?\{%\s*endunless\s*%\})",
        RegexOptions.Compiled
    );

    public static readonly Regex ArgumentRegex = new Regex(
        @"\{\{\s*(\.{2})?([a-zA-Z]\w*)(\?)?\s*\}\}",
        RegexOptions.Compiled
    );
}
