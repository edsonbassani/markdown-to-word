namespace MarkdownToWord.Models;

/// <summary>
/// Represents the type of Mermaid diagram.
/// </summary>
public enum MermaidDiagramType
{
    /// <summary>
    /// Flowchart diagram (graph, flowchart).
    /// </summary>
    Flowchart,

    /// <summary>
    /// Sequence diagram (sequenceDiagram).
    /// </summary>
    Sequence,

    /// <summary>
    /// Gantt chart diagram (gantt). Requires proportional page width.
    /// </summary>
    Gantt,

    /// <summary>
    /// Mindmap diagram (mindmap).
    /// </summary>
    Mindmap,

    /// <summary>
    /// Class diagram (classDiagram).
    /// </summary>
    Class,

    /// <summary>
    /// State diagram (stateDiagram).
    /// </summary>
    State,

    /// <summary>
    /// Entity Relationship diagram (erDiagram).
    /// </summary>
    Er,

    /// <summary>
    /// Pie chart (pie). Requires proportional page width.
    /// </summary>
    Pie,

    /// <summary>
    /// Timeline diagram (timeline). Requires proportional page width.
    /// </summary>
    Timeline,

    /// <summary>
    /// Git graph (gitGraph). Requires proportional page width.
    /// </summary>
    GitGraph,

    /// <summary>
    /// User journey diagram (journey). Requires proportional page width.
    /// </summary>
    Journey,

    /// <summary>
    /// Unknown or unsupported diagram type.
    /// </summary>
    Unknown
}
