namespace MarkdownToWord.Models;

/// <summary>
/// Represents the layout engine type for Mermaid diagrams.
/// </summary>
public enum MermaidLayoutType
{
    /// <summary>
    /// Default Dagre layout engine (used when not specified).
    /// </summary>
    Dagre,

    /// <summary>
    /// ELK (Eclipse Layout Kernel) layout engine.
    /// </summary>
    Elk
}
