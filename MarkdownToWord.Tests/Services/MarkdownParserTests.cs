using Microsoft.Extensions.Logging;
using Moq;
using MarkdownToWord.Models;
using MarkdownToWord.Services;

namespace MarkdownToWord.Tests.Services;

public class MarkdownParserTests : IDisposable
{
    private readonly Mock<IMermaidLayoutDetector> _layoutDetectorMock;
    private readonly Mock<ILogger<MarkdownParser>> _loggerMock;
    private readonly MarkdownParser _parser;
    private readonly string _testDirectory;

    public MarkdownParserTests()
    {
        _layoutDetectorMock = new Mock<IMermaidLayoutDetector>();
        _loggerMock = new Mock<ILogger<MarkdownParser>>();
        _parser = new MarkdownParser(_layoutDetectorMock.Object, _loggerMock.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"MarkdownParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    #region ParseAsync Tests

    [Fact]
    public async Task ParseAsync_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.md");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _parser.ParseAsync(nonExistentPath));
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsDocumentWithNoContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "empty.md");
        await File.WriteAllTextAsync(filePath, string.Empty);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.RawContent);
        Assert.Empty(result.MermaidDiagrams);
        Assert.NotNull(result.ParsedDocument);
    }

    [Fact]
    public async Task ParseAsync_SimpleMarkdown_ReturnsDocument()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "simple.md");
        var content = @"# Heading 1

This is a paragraph.

## Heading 2

Another paragraph.";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result.RawContent);
        Assert.Empty(result.MermaidDiagrams);
        Assert.NotNull(result.ParsedDocument);
    }

    [Fact]
    public async Task ParseAsync_OneMermaidDiagram_ExtractsDiagram()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "mermaid.md");
        var content = @"# Heading

```mermaid
graph TD
    A --> B
```";
        await File.WriteAllTextAsync(filePath, content);

        _layoutDetectorMock
            .Setup(x => x.DetectLayoutType(It.IsAny<string>()))
            .Returns(MermaidLayoutType.Dagre);
        _layoutDetectorMock
            .Setup(x => x.DetectDiagramType(It.IsAny<string>()))
            .Returns(MermaidDiagramType.Flowchart);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.MermaidDiagrams);
        var diagram = result.MermaidDiagrams[0];
        Assert.Contains("graph TD", diagram.Code);
        Assert.Contains("A --> B", diagram.Code);
        Assert.Equal(MermaidLayoutType.Dagre, diagram.LayoutType);
        Assert.Equal(MermaidDiagramType.Flowchart, diagram.DiagramType);
        Assert.Equal(0, diagram.Position);
    }

    [Fact]
    public async Task ParseAsync_MultipleMermaidDiagrams_ExtractsAllDiagrams()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "multiple.md");
        var content = @"# Heading

```mermaid
graph TD
    A --> B
```

Some text in between.

```mermaid
sequenceDiagram
    Alice->>Bob: Hello
```";
        await File.WriteAllTextAsync(filePath, content);

        _layoutDetectorMock
            .Setup(x => x.DetectLayoutType(It.IsAny<string>()))
            .Returns(MermaidLayoutType.Dagre);
        _layoutDetectorMock
            .Setup(x => x.DetectDiagramType(It.Is<string>(s => s.Contains("graph"))))
            .Returns(MermaidDiagramType.Flowchart);
        _layoutDetectorMock
            .Setup(x => x.DetectDiagramType(It.Is<string>(s => s.Contains("sequenceDiagram"))))
            .Returns(MermaidDiagramType.Sequence);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.MermaidDiagrams.Count);
        Assert.Equal(0, result.MermaidDiagrams[0].Position);
        Assert.Equal(1, result.MermaidDiagrams[1].Position);
        Assert.Contains("graph TD", result.MermaidDiagrams[0].Code);
        Assert.Contains("sequenceDiagram", result.MermaidDiagrams[1].Code);
    }

    [Fact]
    public async Task ParseAsync_EmptyMermaidBlock_SkipsDiagram()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "empty_mermaid.md");
        var content = @"# Heading

```mermaid
```";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.MermaidDiagrams);
    }

    [Fact]
    public async Task ParseAsync_NonMermaidCodeBlock_IgnoresBlock()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "code.md");
        var content = @"# Heading

```csharp
var x = 10;
```";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.MermaidDiagrams);
    }

    [Fact]
    public async Task ParseAsync_MermaidWithFrontmatter_ExtractsDiagram()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "frontmatter.md");
        var content = @"# Heading

```mermaid
---
layout: elk
---
graph TD
    A --> B
```";
        await File.WriteAllTextAsync(filePath, content);

        _layoutDetectorMock
            .Setup(x => x.DetectLayoutType(It.IsAny<string>()))
            .Returns(MermaidLayoutType.Elk);
        _layoutDetectorMock
            .Setup(x => x.DetectDiagramType(It.IsAny<string>()))
            .Returns(MermaidDiagramType.Flowchart);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.MermaidDiagrams);
        var diagram = result.MermaidDiagrams[0];
        Assert.Contains("layout: elk", diagram.Code);
        Assert.Equal(MermaidLayoutType.Elk, diagram.LayoutType);
    }

    [Fact]
    public async Task ParseAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.md");
        await File.WriteAllTextAsync(filePath, "# Test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _parser.ParseAsync(filePath, cts.Token));
    }

    [Fact]
    public async Task ParseAsync_MermaidCaseInsensitive_ExtractsDiagram()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "case.md");
        var content = @"```MERMAID
graph TD
    A --> B
```";
        await File.WriteAllTextAsync(filePath, content);

        _layoutDetectorMock
            .Setup(x => x.DetectLayoutType(It.IsAny<string>()))
            .Returns(MermaidLayoutType.Dagre);
        _layoutDetectorMock
            .Setup(x => x.DetectDiagramType(It.IsAny<string>()))
            .Returns(MermaidDiagramType.Flowchart);

        // Act
        var result = await _parser.ParseAsync(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.MermaidDiagrams);
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
