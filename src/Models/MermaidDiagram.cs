namespace MarkdownToWord.Models;

/// <summary>
/// Represents a Mermaid diagram extracted from Markdown.
/// </summary>
public class MermaidDiagram
{
    /// <summary>
    /// The raw Mermaid code including frontmatter and diagram definition.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// The detected layout type (Dagre or ELK).
    /// </summary>
    public MermaidLayoutType LayoutType { get; init; }

    /// <summary>
    /// The detected diagram type.
    /// </summary>
    public MermaidDiagramType DiagramType { get; init; }

    /// <summary>
    /// Indicates whether this diagram type requires proportional page width rendering.
    /// </summary>
    public bool RequiresProportionalWidth => DiagramType switch
    {
        MermaidDiagramType.Gantt => true,
        MermaidDiagramType.Timeline => true,
        MermaidDiagramType.GitGraph => true,
        MermaidDiagramType.Journey => true,
        MermaidDiagramType.Pie => true,
        _ => false
    };

    /// <summary>
    /// Position in the original Markdown document (for ordering).
    /// </summary>
    public int Position { get; init; }
}
