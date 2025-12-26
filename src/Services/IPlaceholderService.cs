using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for loading and processing placeholder replacements.
/// </summary>
public interface IPlaceholderService
{
    /// <summary>
    /// Loads placeholder dictionary from JSON file named "placeholders.json".
    /// Tries the Markdown file directory first (if provided), then falls back to the executable directory (AppContext.BaseDirectory).
    /// </summary>
    /// <param name="markdownFilePath">Path to the Markdown file used to locate placeholders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Placeholder dictionary (empty if file not found).</returns>
    Task<PlaceholderDictionary> LoadPlaceholdersAsync(
        string markdownFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates placeholder dictionary format and keys.
    /// </summary>
    /// <param name="dictionary">Dictionary to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidatePlaceholders(PlaceholderDictionary dictionary);
}
