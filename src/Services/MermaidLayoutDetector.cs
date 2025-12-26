using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for detecting Mermaid diagram layout type and diagram type.
/// </summary>
public partial class MermaidLayoutDetector : IMermaidLayoutDetector
{
    private readonly ILogger<MermaidLayoutDetector> _logger;

    // Regex patterns for detection
    [GeneratedRegex(@"^\s*---\s*$", RegexOptions.Multiline)]
    private static partial Regex FrontmatterDelimiterRegex();

    [GeneratedRegex(@"layout:\s*elk", RegexOptions.IgnoreCase)]
    private static partial Regex ElkLayoutConfigRegex();

    [GeneratedRegex(@"%%\{init:.*?['""]defaultRenderer['""]:\s*['""]elk['""].*?\}%%", RegexOptions.IgnoreCase)]
    private static partial Regex ElkInitDirectiveRegex();

    [GeneratedRegex(@"^\s*(graph|flowchart|sequenceDiagram|gantt|mindmap|classDiagram|stateDiagram|erDiagram|pie|timeline|gitGraph|journey)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex DiagramTypeRegex();

    public MermaidLayoutDetector(ILogger<MermaidLayoutDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects the layout type from Mermaid diagram code.
    /// </summary>
    public MermaidLayoutType DetectLayoutType(string mermaidCode)
    {
        if (string.IsNullOrWhiteSpace(mermaidCode))
        {
            return MermaidLayoutType.Dagre;
        }

        // Check for ELK in YAML frontmatter
        if (HasElkInFrontmatter(mermaidCode))
        {
            _logger.LogDebug("Detected ELK layout from YAML frontmatter");
            return MermaidLayoutType.Elk;
        }

        // Check for ELK in %%init%% directive
        if (ElkInitDirectiveRegex().IsMatch(mermaidCode))
        {
            _logger.LogDebug("Detected ELK layout from %%init%% directive");
            return MermaidLayoutType.Elk;
        }

        // Default to Dagre
        _logger.LogDebug("No ELK layout detected, using default Dagre");
        return MermaidLayoutType.Dagre;
    }

    /// <summary>
    /// Detects the diagram type from Mermaid code.
    /// </summary>
    public MermaidDiagramType DetectDiagramType(string mermaidCode)
    {
        if (string.IsNullOrWhiteSpace(mermaidCode))
        {
            return MermaidDiagramType.Unknown;
        }

        // Skip frontmatter if present
        var codeToAnalyze = SkipFrontmatter(mermaidCode);

        // Match diagram type keyword
        var match = DiagramTypeRegex().Match(codeToAnalyze);
        if (!match.Success)
        {
            _logger.LogWarning("Could not detect diagram type from code");
            return MermaidDiagramType.Unknown;
        }

        var typeKeyword = match.Groups[1].Value.ToLowerInvariant();
        var diagramType = typeKeyword switch
        {
            "graph" or "flowchart" => MermaidDiagramType.Flowchart,
            "sequencediagram" => MermaidDiagramType.Sequence,
            "gantt" => MermaidDiagramType.Gantt,
            "mindmap" => MermaidDiagramType.Mindmap,
            "classdiagram" => MermaidDiagramType.Class,
            "statediagram" => MermaidDiagramType.State,
            "erdiagram" => MermaidDiagramType.Er,
            "pie" => MermaidDiagramType.Pie,
            "timeline" => MermaidDiagramType.Timeline,
            "gitgraph" => MermaidDiagramType.GitGraph,
            "journey" => MermaidDiagramType.Journey,
            _ => MermaidDiagramType.Unknown
        };

        _logger.LogDebug("Detected diagram type: {Type}", diagramType);
        return diagramType;
    }

    /// <summary>
    /// Checks if the code has ELK layout specified in YAML frontmatter.
    /// </summary>
    private bool HasElkInFrontmatter(string code)
    {
        var lines = code.Split('\n');
        bool inFrontmatter = false;
        int frontmatterCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Check for frontmatter delimiter (---)
            if (trimmed == "---")
            {
                frontmatterCount++;
                if (frontmatterCount == 1)
                {
                    inFrontmatter = true;
                    continue;
                }
                if (frontmatterCount == 2)
                {
                    // End of frontmatter, not found
                    return false;
                }
            }

            // If inside frontmatter, check for elk layout
            if (inFrontmatter && ElkLayoutConfigRegex().IsMatch(line))
            {
                return true;
            }

            // If we've passed frontmatter section, stop searching
            if (frontmatterCount >= 2)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// Skips YAML frontmatter and returns the diagram code.
    /// </summary>
    private string SkipFrontmatter(string code)
    {
        var lines = code.Split('\n');
        int frontmatterDelimiters = 0;
        var resultLines = new List<string>();
        bool frontmatterEnded = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (!frontmatterEnded && trimmed == "---")
            {
                frontmatterDelimiters++;
                if (frontmatterDelimiters == 2)
                {
                    frontmatterEnded = true;
                }
                continue;
            }

            if (frontmatterDelimiters == 0 || frontmatterEnded)
            {
                resultLines.Add(line);
            }
        }

        return string.Join('\n', resultLines);
    }
}
