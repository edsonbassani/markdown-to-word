namespace MarkdownToWord.Models;

/// <summary>
/// Represents a dictionary of placeholder replacements loaded from JSON.
/// </summary>
public class PlaceholderDictionary
{
    /// <summary>
    /// Key-value pairs where key is the placeholder (e.g., "{{TITULO}}") 
    /// and value is the replacement text.
    /// </summary>
    public Dictionary<string, string> Replacements { get; init; } = new();

    /// <summary>
    /// The file path from which the placeholders were loaded.
    /// </summary>
    public string? SourceFilePath { get; init; }

    /// <summary>
    /// Indicates whether placeholders were successfully loaded.
    /// </summary>
    public bool IsLoaded => Replacements.Count > 0;
}
