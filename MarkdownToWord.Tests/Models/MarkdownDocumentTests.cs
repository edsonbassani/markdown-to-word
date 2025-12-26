using MarkdownToWord.Models;

namespace MarkdownToWord.Tests.Models;

public class MarkdownDocumentTests
{
    [Fact]
    public void MarkdownDocument_RawContent_CanBeSet()
    {
        // Arrange & Act
        var document = new MarkdownDocument
        {
            RawContent = "# Test Markdown"
        };

        // Assert
        Assert.Equal("# Test Markdown", document.RawContent);
    }

    [Fact]
    public void MermaidDiagrams_DefaultValue_IsEmptyArray()
    {
        // Arrange & Act
        var document = new MarkdownDocument
        {
            RawContent = "# Test"
        };

        // Assert
        Assert.NotNull(document.MermaidDiagrams);
        Assert.Empty(document.MermaidDiagrams);
    }

    [Fact]
    public void MermaidDiagrams_CanBeSet()
    {
        // Arrange
        var diagrams = new List<MermaidDiagram>
        {
            new MermaidDiagram
            {
                Code = "graph TD",
                DiagramType = MermaidDiagramType.Flowchart,
                LayoutType = MermaidLayoutType.Dagre,
                Position = 0
            }
        };

        // Act
        var document = new MarkdownDocument
        {
            RawContent = "# Test",
            MermaidDiagrams = diagrams
        };

        // Assert
        Assert.Single(document.MermaidDiagrams);
        Assert.Equal("graph TD", document.MermaidDiagrams[0].Code);
    }

    [Fact]
    public void ParsedDocument_CanBeNull()
    {
        // Arrange & Act
        var document = new MarkdownDocument
        {
            RawContent = "# Test",
            ParsedDocument = null
        };

        // Assert
        Assert.Null(document.ParsedDocument);
    }

    [Fact]
    public void ParsedDocument_CanBeSet()
    {
        // Arrange
        var parsedObject = new object();

        // Act
        var document = new MarkdownDocument
        {
            RawContent = "# Test",
            ParsedDocument = parsedObject
        };

        // Assert
        Assert.NotNull(document.ParsedDocument);
        Assert.Same(parsedObject, document.ParsedDocument);
    }

    [Fact]
    public void MarkdownDocument_AllProperties_CanBeSet()
    {
        // Arrange
        var diagrams = new List<MermaidDiagram>
        {
            new MermaidDiagram
            {
                Code = "graph TD\n    A --> B",
                DiagramType = MermaidDiagramType.Flowchart,
                LayoutType = MermaidLayoutType.Elk,
                Position = 0
            },
            new MermaidDiagram
            {
                Code = "sequenceDiagram\n    Alice->>Bob: Hello",
                DiagramType = MermaidDiagramType.Sequence,
                LayoutType = MermaidLayoutType.Dagre,
                Position = 1
            }
        };
        var parsedObject = new object();

        // Act
        var document = new MarkdownDocument
        {
            RawContent = "# Test Document\n\nContent here",
            MermaidDiagrams = diagrams,
            ParsedDocument = parsedObject
        };

        // Assert
        Assert.Equal("# Test Document\n\nContent here", document.RawContent);
        Assert.Equal(2, document.MermaidDiagrams.Count);
        Assert.NotNull(document.ParsedDocument);
    }
}
