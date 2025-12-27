using System.Text.Json;
using Microsoft.Extensions.Logging;
using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for loading and processing placeholder replacements.
/// </summary>
public class PlaceholderService : IPlaceholderService
{
    private readonly ILogger<PlaceholderService> _logger;
    private const string PlaceholderFileName = "placeholders.json";

    public PlaceholderService(ILogger<PlaceholderService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads placeholder dictionary from JSON file.
    /// Tries the Markdown file directory first (if provided), then falls back to the executable directory (AppContext.BaseDirectory).
    /// </summary>
    public async Task<PlaceholderDictionary> LoadPlaceholdersAsync(
        string markdownFilePath,
        CancellationToken cancellationToken = default)
    {
        var candidatePaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(markdownFilePath))
        {
            var markdownDirectory = Path.GetDirectoryName(markdownFilePath);
            if (!string.IsNullOrEmpty(markdownDirectory))
            {
                candidatePaths.Add(Path.Combine(markdownDirectory, PlaceholderFileName));
            }
        }

        if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
        {
            candidatePaths.Add(Path.Combine(AppContext.BaseDirectory, PlaceholderFileName));
        }

        string? placeholderPath = candidatePaths.Where(File.Exists).FirstOrDefault();

        if (placeholderPath == null)
        {
            _logger.LogWarning(
                "Placeholder file not found in search paths: {Paths}. Continuing without replacements.",
                candidatePaths.Count > 0 ? string.Join(", ", candidatePaths) : "<none>");
            return new PlaceholderDictionary();
        }

        try
        {
            _logger.LogInformation("Loading placeholders from: {Path}", placeholderPath);

            var jsonContent = await File.ReadAllTextAsync(placeholderPath, cancellationToken);
            var replacements = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

            if (replacements == null || replacements.Count == 0)
            {
                _logger.LogWarning("Placeholder file is empty or invalid: {Path}", placeholderPath);
                return new PlaceholderDictionary { SourceFilePath = placeholderPath };
            }

            _logger.LogInformation(
                "Loaded {Count} placeholder(s) from {Path}",
                replacements.Count,
                placeholderPath);

            return new PlaceholderDictionary
            {
                Replacements = replacements,
                SourceFilePath = placeholderPath
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse placeholder JSON: {Path}", placeholderPath);
            return new PlaceholderDictionary { SourceFilePath = placeholderPath };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load placeholders from: {Path}", placeholderPath);
            return new PlaceholderDictionary();
        }
    }

    /// <summary>
    /// Validates placeholder dictionary format and keys.
    /// </summary>
    public bool ValidatePlaceholders(PlaceholderDictionary dictionary)
    {
        if (dictionary.Replacements.Count == 0)
        {
            return true; // Empty dictionary is valid (no replacements needed)
        }

        var invalidKeys = dictionary.Replacements.Keys
            .Where(key => !key.StartsWith("{{") || !key.EndsWith("}}"))
            .ToList();

        if (invalidKeys.Count > 0)
        {
            _logger.LogWarning(
                "Found {Count} invalid placeholder key(s). Keys must be in format {{{{KEY}}}}: {Keys}",
                invalidKeys.Count,
                string.Join(", ", invalidKeys));
            return false;
        }

        _logger.LogInformation("All {Count} placeholder(s) are valid", dictionary.Replacements.Count);
        return true;
    }
}
