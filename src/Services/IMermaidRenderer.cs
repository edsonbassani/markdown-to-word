using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for rendering Mermaid diagrams to PNG images using Playwright.
/// </summary>
public interface IMermaidRenderer : IAsyncDisposable
{
    /// <summary>
    /// Initializes the shared browser instance (idempotent and thread-safe).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a Mermaid diagram to a PNG image.
    /// Uses system Edge/Chrome with Mermaid.js 11.x and ELK support.
    /// </summary>
    Task<byte[]> RenderDiagramAsync(
        MermaidDiagram diagram,
        int? pageWidthInPixels = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a Mermaid diagram with automatic retry logic and browser recovery.
    /// Recommended for use in parallel rendering scenarios.
    /// </summary>
    Task<byte[]> RenderDiagramWithRetryAsync(
        MermaidDiagram diagram,
        int? pageWidthInPixels = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders multiple diagrams sequentially using the shared browser.
    /// </summary>
    Task<IReadOnlyList<byte[]>> RenderDiagramsBatchAsync(
        IEnumerable<MermaidDiagram> diagrams,
        int? pageWidthInPixels = null,
        CancellationToken cancellationToken = default);
}
