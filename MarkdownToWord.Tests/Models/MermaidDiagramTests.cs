using MarkdownToWord.Models;

namespace MarkdownToWord.Tests.Models;

public class MermaidDiagramTests
{
    [Fact]
    public void RequiresProportionalWidth_Gantt_ReturnsTrue()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "gantt",
            DiagramType = MermaidDiagramType.Gantt,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.True(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Timeline_ReturnsTrue()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "timeline",
            DiagramType = MermaidDiagramType.Timeline,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.True(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_GitGraph_ReturnsTrue()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "gitGraph",
            DiagramType = MermaidDiagramType.GitGraph,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.True(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Journey_ReturnsTrue()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "journey",
            DiagramType = MermaidDiagramType.Journey,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.True(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Pie_ReturnsTrue()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "pie",
            DiagramType = MermaidDiagramType.Pie,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.True(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Flowchart_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "graph TD",
            DiagramType = MermaidDiagramType.Flowchart,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Sequence_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "sequenceDiagram",
            DiagramType = MermaidDiagramType.Sequence,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Mindmap_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "mindmap",
            DiagramType = MermaidDiagramType.Mindmap,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Class_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "classDiagram",
            DiagramType = MermaidDiagramType.Class,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_State_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "stateDiagram",
            DiagramType = MermaidDiagramType.State,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Er_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "erDiagram",
            DiagramType = MermaidDiagramType.Er,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void RequiresProportionalWidth_Unknown_ReturnsFalse()
    {
        // Arrange
        var diagram = new MermaidDiagram
        {
            Code = "unknown",
            DiagramType = MermaidDiagramType.Unknown,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 0
        };

        // Act & Assert
        Assert.False(diagram.RequiresProportionalWidth);
    }

    [Fact]
    public void Position_CanBeSet()
    {
        // Arrange & Act
        var diagram = new MermaidDiagram
        {
            Code = "graph TD",
            DiagramType = MermaidDiagramType.Flowchart,
            LayoutType = MermaidLayoutType.Dagre,
            Position = 42
        };

        // Assert
        Assert.Equal(42, diagram.Position);
    }

    [Fact]
    public void LayoutType_Elk_CanBeSet()
    {
        // Arrange & Act
        var diagram = new MermaidDiagram
        {
            Code = "graph TD",
            DiagramType = MermaidDiagramType.Flowchart,
            LayoutType = MermaidLayoutType.Elk,
            Position = 0
        };

        // Assert
        Assert.Equal(MermaidLayoutType.Elk, diagram.LayoutType);
    }
}
