using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for detecting Mermaid diagram layout type (Dagre or ELK).
/// </summary>
public interface IMermaidLayoutDetector
{
    /// <summary>
    /// Detects the layout type from Mermaid diagram code.
    /// Analyzes YAML frontmatter and %%init%% directives.
    /// </summary>
    /// <param name="mermaidCode">The raw Mermaid diagram code.</param>
    /// <returns>The detected layout type (defaults to Dagre if not specified).</returns>
    MermaidLayoutType DetectLayoutType(string mermaidCode);

    /// <summary>
    /// Detects the diagram type from Mermaid code.
    /// </summary>
    /// <param name="mermaidCode">The raw Mermaid diagram code.</param>
    /// <returns>The detected diagram type.</returns>
    MermaidDiagramType DetectDiagramType(string mermaidCode);
}
