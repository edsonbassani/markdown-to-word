using Microsoft.Extensions.Logging;
using Moq;
using MarkdownToWord.Models;
using MarkdownToWord.Services;

namespace MarkdownToWord.Tests.Services;

public class PlaceholderServiceTests : IDisposable
{
    private readonly Mock<ILogger<PlaceholderService>> _loggerMock;
    private readonly PlaceholderService _service;
    private readonly string _testDirectory;

    public PlaceholderServiceTests()
    {
        _loggerMock = new Mock<ILogger<PlaceholderService>>();
        _service = new PlaceholderService(_loggerMock.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PlaceholderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    #region LoadPlaceholdersAsync Tests

    [Fact]
    public async Task LoadPlaceholdersAsync_FileDoesNotExist_ReturnsEmptyDictionary()
    {
        // Arrange
        var markdownPath = Path.Combine(_testDirectory, "test.md");

        // Act
        var result = await _service.LoadPlaceholdersAsync(markdownPath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Replacements);
        Assert.False(result.IsLoaded);
    }

    [Fact]
    public async Task LoadPlaceholdersAsync_ValidJsonFile_LoadsPlaceholders()
    {
        // Arrange
        var placeholdersPath = Path.Combine(AppContext.BaseDirectory, "placeholders.json");
        var jsonContent = @"{
  ""{{TITULO}}"": ""Test Title"",
  ""{{AUTOR}}"": ""Test Author""
}";
        await File.WriteAllTextAsync(placeholdersPath, jsonContent);

        try
        {
            var markdownPath = Path.Combine(_testDirectory, "test.md");

            // Act
            var result = await _service.LoadPlaceholdersAsync(markdownPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Replacements.Count);
            Assert.Equal("Test Title", result.Replacements["{{TITULO}}"]);
            Assert.Equal("Test Author", result.Replacements["{{AUTOR}}"]);
            Assert.True(result.IsLoaded);
            Assert.NotNull(result.SourceFilePath);
        }
        finally
        {
            if (File.Exists(placeholdersPath))
            {
                File.Delete(placeholdersPath);
            }
        }
    }

    [Fact]
    public async Task LoadPlaceholdersAsync_PrefersMarkdownDirectoryOverExecutableDirectory()
    {
        // Arrange
        var markdownPlaceholdersPath = Path.Combine(_testDirectory, "placeholders.json");
        var markdownJson = @"{
    ""{{TITULO}}"": ""Markdown Title""
}";

        var basePlaceholdersPath = Path.Combine(AppContext.BaseDirectory, "placeholders.json");
        var baseJson = @"{
    ""{{TITULO}}"": ""Base Title""
}";

        await File.WriteAllTextAsync(markdownPlaceholdersPath, markdownJson);
        await File.WriteAllTextAsync(basePlaceholdersPath, baseJson);

        try
        {
            var markdownPath = Path.Combine(_testDirectory, "test.md");

            // Act
            var result = await _service.LoadPlaceholdersAsync(markdownPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsLoaded);
            Assert.Equal("Markdown Title", result.Replacements["{{TITULO}}"]); // prefer markdown directory
            Assert.Equal(markdownPlaceholdersPath, result.SourceFilePath);
        }
        finally
        {
            if (File.Exists(markdownPlaceholdersPath))
            {
                File.Delete(markdownPlaceholdersPath);
            }

            if (File.Exists(basePlaceholdersPath))
            {
                File.Delete(basePlaceholdersPath);
            }
        }
    }

    [Fact]
    public async Task LoadPlaceholdersAsync_EmptyJsonFile_ReturnsEmptyDictionary()
    {
        // Arrange
        var placeholdersPath = Path.Combine(AppContext.BaseDirectory, "placeholders.json");
        await File.WriteAllTextAsync(placeholdersPath, "{}");

        try
        {
            var markdownPath = Path.Combine(_testDirectory, "test.md");

            // Act
            var result = await _service.LoadPlaceholdersAsync(markdownPath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Replacements);
            Assert.False(result.IsLoaded);
        }
        finally
        {
            if (File.Exists(placeholdersPath))
            {
                File.Delete(placeholdersPath);
            }
        }
    }

    [Fact]
    public async Task LoadPlaceholdersAsync_InvalidJsonFile_ReturnsEmptyDictionary()
    {
        // Arrange
        var placeholdersPath = Path.Combine(AppContext.BaseDirectory, "placeholders.json");
        await File.WriteAllTextAsync(placeholdersPath, "{ invalid json");

        try
        {
            var markdownPath = Path.Combine(_testDirectory, "test.md");

            // Act
            var result = await _service.LoadPlaceholdersAsync(markdownPath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Replacements);
            Assert.False(result.IsLoaded);
        }
        finally
        {
            if (File.Exists(placeholdersPath))
            {
                File.Delete(placeholdersPath);
            }
        }
    }

    #endregion

    #region ValidatePlaceholders Tests

    [Fact]
    public void ValidatePlaceholders_EmptyDictionary_ReturnsTrue()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary();

        // Act
        var result = _service.ValidatePlaceholders(dictionary);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePlaceholders_ValidPlaceholders_ReturnsTrue()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "{{TITULO}}", "Test Title" },
                { "{{AUTOR}}", "Test Author" },
                { "{{DATA}}", "2024-01-01" }
            }
        };

        // Act
        var result = _service.ValidatePlaceholders(dictionary);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePlaceholders_MissingOpeningBraces_ReturnsFalse()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "TITULO}}", "Test Title" }
            }
        };

        // Act
        var result = _service.ValidatePlaceholders(dictionary);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePlaceholders_MissingClosingBraces_ReturnsFalse()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "{{TITULO", "Test Title" }
            }
        };

        // Act
        var result = _service.ValidatePlaceholders(dictionary);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePlaceholders_NoBraces_ReturnsFalse()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "TITULO", "Test Title" }
            }
        };

        // Act
        var result = _service.ValidatePlaceholders(dictionary);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePlaceholders_MixedValidAndInvalid_ReturnsFalse()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "{{TITULO}}", "Test Title" },
                { "INVALID", "Invalid Key" }
            }
        };

        // Act
        var result = _service.ValidatePlaceholders(dictionary);

        // Assert
        Assert.False(result);
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
