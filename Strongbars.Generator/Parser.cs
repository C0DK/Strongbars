using System.IO;
using System.Text.RegularExpressions;
using Strongbars.Abstractions;

namespace Strongbars.Generator;

public class Parser
{
    public string TemplateName { get; }
    public string FileContent { get; }
    private readonly string? _fileDirectory;
    private readonly string? _projectRoot;
    private readonly Func<string, string?>? _fileReader;
    private readonly HashSet<string>? _includedPaths;

    public Parser(
        string templateName,
        string fileContent,
        string? fileDirectory = null,
        string? projectRoot = null,
        Func<string, string?>? fileReader = null,
        HashSet<string>? includedPaths = null
    )
    {
        TemplateName = templateName;
        FileContent = fileContent;
        _fileDirectory = fileDirectory;
        _projectRoot = projectRoot;
        _fileReader = fileReader;
        _includedPaths = includedPaths;
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
            var includeMatch = IncludeRegex.Match(content);
            if (includeMatch.Success)
            {
                index++;
                return ParseInclude(includeMatch.Groups[1].Value.Trim(), match, index);
            }
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

    private ITemplateNode ParseInclude(string includePath, Match match, int matchIndex)
    {
        string resolvedPath;
        if (includePath.StartsWith("/"))
        {
            if (string.IsNullOrEmpty(_projectRoot))
                throw new ParserError(
                    TemplateName,
                    FileContent,
                    match,
                    matchIndex,
                    $"Cannot resolve root-relative include '{includePath}': project root is unavailable"
                );
            resolvedPath = Path.GetFullPath(Path.Combine(_projectRoot, includePath.TrimStart('/')));
        }
        else
        {
            if (_fileDirectory == null)
                throw new ParserError(
                    TemplateName,
                    FileContent,
                    match,
                    matchIndex,
                    $"Cannot resolve include '{includePath}': file directory is unavailable"
                );
            resolvedPath = Path.GetFullPath(Path.Combine(_fileDirectory, includePath));
        }

        if (_includedPaths?.Contains(resolvedPath) == true)
            throw new ParserError(
                TemplateName,
                FileContent,
                match,
                matchIndex,
                $"Circular include detected for '{resolvedPath}'"
            );

        string? content;
        try
        {
            content = _fileReader != null ? _fileReader(resolvedPath) : File.ReadAllText(resolvedPath);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new ParserError(
                TemplateName,
                FileContent,
                match,
                matchIndex,
                $"Could not read include file '{resolvedPath}': {ex.Message}"
            );
        }

        if (content == null)
            throw new ParserError(
                TemplateName,
                FileContent,
                match,
                matchIndex,
                $"Include file not found: '{resolvedPath}'"
            );

        var newIncludedPaths = new HashSet<string>(_includedPaths ?? new HashSet<string>())
        {
            resolvedPath,
        };
        var includedDir = Path.GetDirectoryName(resolvedPath);
        return new Parser(
            resolvedPath,
            content,
            includedDir,
            _projectRoot,
            _fileReader,
            newIncludedPaths
        ).Parse();
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

    public static Regex IncludeRegex = new Regex(@"^include\s+(.+)$", RegexOptions.Compiled);

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

/// <summary>
/// Thrown when a template is syntactically valid but semantically inconsistent —
/// for example when the same variable name is used with conflicting types.
/// </summary>
public class TemplateError : Exception
{
    public TemplateError(string reason)
        : base(reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
