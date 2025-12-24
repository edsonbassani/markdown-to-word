using Markdig;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using MarkdownToWord.Models;
using MarkdigDocument = Markdig.Syntax.MarkdownDocument;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for parsing Markdown files and extracting Mermaid diagrams.
/// </summary>
public class MarkdownParser : IMarkdownParser
{
    private readonly IMermaidLayoutDetector _layoutDetector;
    private readonly ILogger<MarkdownParser> _logger;
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public MarkdownParser(
        IMermaidLayoutDetector layoutDetector,
        ILogger<MarkdownParser> logger)
    {
        _layoutDetector = layoutDetector;
        _logger = logger;
    }

    /// <summary>
    /// Parses a Markdown file and extracts its content and diagrams.
    /// </summary>
    public async Task<Models.MarkdownDocument> ParseAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Markdown file not found: {filePath}", filePath);
        }

        _logger.LogInformation("Parsing Markdown file: {Path}", filePath);

        var rawContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        // Parse the document
        var parsedDocument = Markdown.Parse(rawContent, Pipeline);

        // Extract Mermaid diagrams
        var mermaidDiagrams = ExtractMermaidDiagrams(parsedDocument);

        _logger.LogInformation(
            "Parsed Markdown document. Found {Count} Mermaid diagram(s)",
            mermaidDiagrams.Count);

        return new Models.MarkdownDocument
        {
            RawContent = rawContent,
            MermaidDiagrams = mermaidDiagrams,
            ParsedDocument = parsedDocument
        };
    }

    /// <summary>
    /// Extracts Mermaid diagrams from a parsed Markdown document.
    /// </summary>
    private List<MermaidDiagram> ExtractMermaidDiagrams(MarkdigDocument parsedDocument)
    {
        var diagrams = new List<MermaidDiagram>();
        int position = 0;

        // Walk the document tree looking for fenced code blocks
        foreach (var block in parsedDocument.Descendants<FencedCodeBlock>())
        {
            // Check if this is a Mermaid code block
            var info = block.Info?.ToLowerInvariant();
            if (info != "mermaid")
            {
                continue;
            }

            // Extract the code
            var code = block.Lines.ToString();
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Found empty Mermaid code block at position {Position}", position);
                continue;
            }

            // Detect layout and diagram type
            var layoutType = _layoutDetector.DetectLayoutType(code);
            var diagramType = _layoutDetector.DetectDiagramType(code);

            var diagram = new MermaidDiagram
            {
                Code = code,
                LayoutType = layoutType,
                DiagramType = diagramType,
                Position = position++
            };

            diagrams.Add(diagram);

            _logger.LogDebug(
                "Extracted Mermaid diagram: Type={Type}, Layout={Layout}, Position={Position}",
                diagramType,
                layoutType,
                diagram.Position);
        }

        return diagrams;
    }
}
