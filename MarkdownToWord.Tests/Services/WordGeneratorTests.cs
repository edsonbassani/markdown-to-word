using Microsoft.Extensions.Logging;
using Moq;
using MarkdownToWord.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MarkdownToWord.Tests.Services;

public class WordGeneratorTests : IDisposable
{
    private readonly Mock<ILogger<WordGenerator>> _loggerMock;
    private readonly WordGenerator _generator;
    private readonly string _testDirectory;

    public WordGeneratorTests()
    {
        _loggerMock = new Mock<ILogger<WordGenerator>>();
        _generator = new WordGenerator(_loggerMock.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WordGeneratorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    #region GetUsablePageWidthAsync Tests

    [Fact]
    public async Task GetUsablePageWidthAsync_TemplateDoesNotExist_ReturnsDefaultWidth()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.docx");

        // Act
        var result = await _generator.GetUsablePageWidthAsync(nonExistentPath);

        // Assert
        Assert.Equal(601, result); // Default usable width for A4
    }

    [Fact]
    public async Task GetUsablePageWidthAsync_ValidTemplate_ReturnsCalculatedWidth()
    {
        // Arrange
        var templatePath = Path.Combine(_testDirectory, "template.docx");
        CreateTestTemplate(templatePath);

        // Act
        var result = await _generator.GetUsablePageWidthAsync(templatePath);

        // Assert
        Assert.True(result > 0);
        Assert.True(result < 1000); // Reasonable range for A4 page
    }

    [Fact]
    public async Task GetUsablePageWidthAsync_EmptyTemplate_ReturnsDefaultWidth()
    {
        // Arrange
        var templatePath = Path.Combine(_testDirectory, "empty.docx");
        CreateEmptyTemplate(templatePath);

        // Act
        var result = await _generator.GetUsablePageWidthAsync(templatePath);

        // Assert
        Assert.Equal(601, result); // Falls back to default
    }

    #endregion

    #region Helper Methods

    private void CreateTestTemplate(string path)
    {
        using var document = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        // Add section properties with page size
        var body = mainPart.Document.Body!;
        var sectionProps = new SectionProperties(
            new PageSize { Width = 11906, Height = 16838 }, // A4
            new PageMargin { Left = 1440, Right = 1440, Top = 1440, Bottom = 1440 }
        );
        body.AppendChild(sectionProps);

        mainPart.Document.Save();
    }

    private void CreateEmptyTemplate(string path)
    {
        using var document = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        mainPart.Document.Save();
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                // Give time for file handles to be released
                System.Threading.Thread.Sleep(100);
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
