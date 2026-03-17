using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public class Parser
{
    public SourceProductionContext Context { get; }
    public string TemplateName { get; }
    public string FileContent { get; }

    public Parser(SourceProductionContext context, string templateName, string fileContent)
    {
        Context = context;
        TemplateName = templateName;
        FileContent = fileContent;
    }

    public ITemplateNode Parse()
    {
        var tokens = new List<ITemplateNode>();

        var stack = new List<Match>();
        var matches = ExpressionRegex.Matches(FileContent);
        if (matches.Count == 0)
            return new LiteralTemplateNode(FileContent);
        var i = 0;
        while (i < matches.Count)
        {
            var match = matches[i];
            if (DiffSinceLastMatch(matches, i) is { Length: > 0 } literal)
                tokens.Add(new LiteralTemplateNode(literal));

            tokens.Add(ParseMatch(matches, ref i));
        }

        // Any trailing literal after the last variable
        var endOfLastMatch = matches[i - 1].Index + matches[i - 1].Length;
        if (endOfLastMatch < FileContent.Length)
            tokens.Add(new LiteralTemplateNode(FileContent.Substring(endOfLastMatch)));

        return new CompositeTemplateNode(tokens);
    }

    private ITemplateNode ParseConditional(
        Match conditionalStatementMatch,
        MatchCollection matches,
        ref int matchIndex
    )
    {
        var inverse = conditionalStatementMatch.Groups[1].Value == "unless";
        var conditionalIndex = matchIndex;

        // TODO: support non bool things
        var conditional = new Variable(
            name: conditionalStatementMatch.Groups[2].Value,
            type: VariableType.Bool,
            array: false,
            optional: false
        );
        matchIndex++;
        var ifTokens = new List<ITemplateNode>();
        var elseTokens = new List<ITemplateNode>();
        bool hasElse = false;

        while (true)
        {
            if (matchIndex >= matches.Count)
                throw new ParserError(
                    TemplateName,
                    FileContent,
                    matches[conditionalIndex],
                    matchIndex,
                    "Conditional doesnt end!"
                );
            var match = matches[matchIndex];
            if (DiffSinceLastMatch(matches, matchIndex) is { Length: > 0 } literal)
                ifTokens.Add(new LiteralTemplateNode(literal));

            if (match.Groups[2].Value == "else")
            {
                matchIndex++;
                hasElse = true;
                break;
            }
            if (match.Groups[2].Value == "end")
            {
                matchIndex++;
                break;
            }
            ifTokens.Add(ParseMatch(matches, ref matchIndex));
        }
        while (hasElse)
        {
            if (matchIndex >= matches.Count)
                throw new ParserError(
                    TemplateName,
                    FileContent,
                    matches[conditionalIndex],
                    matchIndex,
                    "Conditional doesnt end!"
                );

            var match = matches[matchIndex];

            if (DiffSinceLastMatch(matches, matchIndex) is { Length: > 0 } literal)
                elseTokens.Add(new LiteralTemplateNode(literal));
            if (match.Groups[2].Value == "end")
            {
                matchIndex++;
                break;
            }
            elseTokens.Add(ParseMatch(matches, ref matchIndex));
        }
        var ifToken = new CompositeTemplateNode(ifTokens);
        var elseToken = new CompositeTemplateNode(elseTokens);
        return new ConditionalTemplateNode(
            conditional,
            inverse ? elseToken : ifToken,
            inverse ? ifToken : elseToken
        );
    }

    private ITemplateNode ParseMatch(MatchCollection matches, ref int index)
    {
        var match = matches[index];
        if (match.Groups[1].Success)
        {
            var content = match.Groups[1].Value;
            var variableMatch = VariableRegex.Match(content);
            if (!variableMatch.Success)
                throw new ParserError(
                    TemplateName,
                    FileContent,
                    match,
                    index,
                    $"'{content}' is not a valid variable name"
                );
            var variable = new Variable(
                name: variableMatch.Groups[2].Value,
                type: VariableType.TemplateArgument,
                array: variableMatch.Groups[1].Success,
                optional: variableMatch.Groups[3].Success
            );
            index++;
            return new VariableTemplateNode(variable);
        }
        else if (match.Groups[2].Success)
        {
            var content = match.Groups[2].Value;
            var conditionalStatementMatch = ConditionalStartRegex.Match(content);
            if (conditionalStatementMatch.Success)
                return ParseConditional(conditionalStatementMatch, matches, ref index);
            else
                throw new ParserError(
                    TemplateName,
                    FileContent,
                    match,
                    index,
                    $"'{content}' is not a valid expression"
                );
        }

        throw new ParserError(TemplateName, FileContent, match, index, "invalid token");
    }

    private string DiffSinceLastMatch(MatchCollection matches, int index)
    {
        var match = matches[index];
        if (index == 0)
            return FileContent.Substring(0, match.Index);

        var priorMatch = matches[index - 1];
        var endOfLastMatch = priorMatch.Index + priorMatch.Length;
        return FileContent.Substring(endOfLastMatch, match.Index - endOfLastMatch);
    }

    public static Regex ConditionalStartRegex = new Regex(
        @"(if|unless)\s+([a-zA-Z]\w*)",
        RegexOptions.Compiled
    );

    public static Regex ExpressionRegex = new Regex(
        @"{{\s*(.*?)\s*}}|{%\s*(.*?)\s*%}",
        RegexOptions.Compiled
    );

    public static Regex VariableRegex = new Regex(
        @"^(\.{2})?([a-zA-Z]\w*)(\?)?$",
        RegexOptions.Compiled
    );
}

public class ParserError : Exception
{
    public ParserError(
        string templateName,
        string content,
        Match match,
        int matchIndex,
        string reason
    )
        : base($"[{templateName}]: {reason}")
    {
        TemplateName = templateName;
        Content = content;
        Match = match;
        MatchIndex = matchIndex;
        Reason = reason;
    }

    public string TemplateName { get; }
    public string Content { get; }
    public Match Match { get; }
    public int MatchIndex { get; }
    public string Reason { get; }
}
