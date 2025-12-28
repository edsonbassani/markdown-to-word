using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for parsing Markdown files and extracting Mermaid diagrams.
/// </summary>
public interface IMarkdownParser
{
    /// <summary>
    /// Parses a Markdown file and extracts its content and diagrams.
    /// </summary>
    /// <param name="filePath">Full path to the Markdown file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed Markdown document with extracted diagrams.</returns>
    Task<MarkdownDocument> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}
