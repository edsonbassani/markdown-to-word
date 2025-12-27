using MarkdownToWord.Models;

namespace MarkdownToWord.Tests.Models;

public class PlaceholderDictionaryTests
{
    [Fact]
    public void IsLoaded_EmptyReplacements_ReturnsFalse()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary();

        // Act & Assert
        Assert.False(dictionary.IsLoaded);
    }

    [Fact]
    public void IsLoaded_WithReplacements_ReturnsTrue()
    {
        // Arrange
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "{{KEY}}", "Value" }
            }
        };

        // Act & Assert
        Assert.True(dictionary.IsLoaded);
    }

    [Fact]
    public void Replacements_DefaultValue_IsEmpty()
    {
        // Arrange & Act
        var dictionary = new PlaceholderDictionary();

        // Assert
        Assert.NotNull(dictionary.Replacements);
        Assert.Empty(dictionary.Replacements);
    }

    [Fact]
    public void SourceFilePath_CanBeNull()
    {
        // Arrange & Act
        var dictionary = new PlaceholderDictionary
        {
            SourceFilePath = null
        };

        // Assert
        Assert.Null(dictionary.SourceFilePath);
    }

    [Fact]
    public void SourceFilePath_CanBeSet()
    {
        // Arrange & Act
        var dictionary = new PlaceholderDictionary
        {
            SourceFilePath = @"C:\test\placeholders.json"
        };

        // Assert
        Assert.Equal(@"C:\test\placeholders.json", dictionary.SourceFilePath);
    }

    [Fact]
    public void Replacements_CanContainMultipleEntries()
    {
        // Arrange & Act
        var dictionary = new PlaceholderDictionary
        {
            Replacements = new Dictionary<string, string>
            {
                { "{{TITULO}}", "Test Title" },
                { "{{AUTOR}}", "Test Author" },
                { "{{DATA}}", "2024-01-01" }
            }
        };

        // Assert
        Assert.Equal(3, dictionary.Replacements.Count);
        Assert.Equal("Test Title", dictionary.Replacements["{{TITULO}}"]);
        Assert.Equal("Test Author", dictionary.Replacements["{{AUTOR}}"]);
        Assert.Equal("2024-01-01", dictionary.Replacements["{{DATA}}"]);
    }
}
