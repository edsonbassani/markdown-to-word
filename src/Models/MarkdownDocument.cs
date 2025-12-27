namespace MarkdownToWord.Models;

/// <summary>
/// Represents a parsed Markdown document with extracted content and diagrams.
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The raw Markdown content.
    /// </summary>
    public required string RawContent { get; init; }

    /// <summary>
    /// Collection of Mermaid diagrams found in the document.
    /// </summary>
    public IReadOnlyList<MermaidDiagram> MermaidDiagrams { get; init; } = Array.Empty<MermaidDiagram>();

    /// <summary>
    /// The Markdig document structure (for processing).
    /// </summary>
    public object? ParsedDocument { get; init; }
}
