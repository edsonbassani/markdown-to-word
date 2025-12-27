namespace MarkdownToWord.Models;

/// <summary>
/// Options for Markdown to Word conversion.
/// </summary>
public class ConversionOptions
{
    /// <summary>
    /// Full path to the input Markdown file.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Full path to the output Word document.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Optional full path to the Word template file.
    /// If not provided, a basic document will be generated.
    /// </summary>
    public string? TemplatePath { get; init; }

    /// <summary>
    /// Timeout in seconds for rendering each Mermaid diagram.
    /// Default: 30 seconds.
    /// </summary>
    public int MermaidTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// DPI scale factor for diagram screenshots (1 = standard, 2 = high resolution).
    /// Default: 2 (high resolution).
    /// </summary>
    public int DiagramDpiScale { get; init; } = 2;
}
