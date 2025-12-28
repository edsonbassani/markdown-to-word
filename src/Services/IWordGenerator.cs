using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for generating Word documents from Markdown.
/// </summary>
public interface IWordGenerator
{
    /// <summary>
    /// Generates a Word document from parsed Markdown and rendered diagrams.
    /// </summary>
    /// <param name="options">Conversion options including input/output paths and template.</param>
    /// <param name="markdownDocument">Parsed Markdown document.</param>
    /// <param name="placeholders">Placeholder dictionary for replacements.</param>
    /// <param name="renderedDiagrams">Dictionary mapping diagram position to rendered PNG data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GenerateAsync(
        ConversionOptions options,
        MarkdownDocument markdownDocument,
        PlaceholderDictionary placeholders,
        IReadOnlyDictionary<int, byte[]> renderedDiagrams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the usable page width in pixels from a Word template.
    /// Calculated as: PageWidth - LeftMargin - RightMargin.
    /// </summary>
    /// <param name="templatePath">Path to Word template.</param>
    /// <returns>Usable width in pixels at 96 DPI.</returns>
    Task<int> GetUsablePageWidthAsync(string templatePath);
}
