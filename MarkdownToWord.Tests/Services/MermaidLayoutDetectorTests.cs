using Microsoft.Extensions.Logging;
using Moq;
using MarkdownToWord.Models;
using MarkdownToWord.Services;

namespace MarkdownToWord.Tests.Services;

public class MermaidLayoutDetectorTests
{
    private readonly Mock<ILogger<MermaidLayoutDetector>> _loggerMock;
    private readonly MermaidLayoutDetector _detector;

    public MermaidLayoutDetectorTests()
    {
        _loggerMock = new Mock<ILogger<MermaidLayoutDetector>>();
        _detector = new MermaidLayoutDetector(_loggerMock.Object);
    }

    #region DetectLayoutType Tests

    [Fact]
    public void DetectLayoutType_EmptyCode_ReturnsDagre()
    {
        // Act
        var result = _detector.DetectLayoutType(string.Empty);

        // Assert
        Assert.Equal(MermaidLayoutType.Dagre, result);
    }

    [Fact]
    public void DetectLayoutType_NullCode_ReturnsDagre()
    {
        // Act
        var result = _detector.DetectLayoutType(null!);

        // Assert
        Assert.Equal(MermaidLayoutType.Dagre, result);
    }

    [Fact]
    public void DetectLayoutType_NoLayoutSpecified_ReturnsDagre()
    {
        // Arrange
        var code = @"
graph TD
    A --> B
    B --> C
";

        // Act
        var result = _detector.DetectLayoutType(code);

        // Assert
        Assert.Equal(MermaidLayoutType.Dagre, result);
    }

    [Fact]
    public void DetectLayoutType_ElkInFrontmatter_ReturnsElk()
    {
        // Arrange
        var code = @"---
layout: elk
---
graph TD
    A --> B
";

        // Act
        var result = _detector.DetectLayoutType(code);

        // Assert
        Assert.Equal(MermaidLayoutType.Elk, result);
    }

    [Fact]
    public void DetectLayoutType_ElkInFrontmatterUpperCase_ReturnsElk()
    {
        // Arrange
        var code = @"---
layout: ELK
---
graph TD
    A --> B
";

        // Act
        var result = _detector.DetectLayoutType(code);

        // Assert
        Assert.Equal(MermaidLayoutType.Elk, result);
    }

    [Fact]
    public void DetectLayoutType_ElkInInitDirective_ReturnsElk()
    {
        // Arrange
        var code = @"%%{init: {'defaultRenderer':'elk'}}%%
graph TD
    A --> B
";

        // Act
        var result = _detector.DetectLayoutType(code);

        // Assert
        Assert.Equal(MermaidLayoutType.Elk, result);
    }

    [Fact]
    public void DetectLayoutType_ElkInInitDirectiveWithDoubleQuotes_ReturnsElk()
    {
        // Arrange
        var code = @"%%{init: {""defaultRenderer"":""elk""}}%%
graph TD
    A --> B
";

        // Act
        var result = _detector.DetectLayoutType(code);

        // Assert
        Assert.Equal(MermaidLayoutType.Elk, result);
    }

    #endregion

    #region DetectDiagramType Tests

    [Fact]
    public void DetectDiagramType_EmptyCode_ReturnsUnknown()
    {
        // Act
        var result = _detector.DetectDiagramType(string.Empty);

        // Assert
        Assert.Equal(MermaidDiagramType.Unknown, result);
    }

    [Fact]
    public void DetectDiagramType_NullCode_ReturnsUnknown()
    {
        // Act
        var result = _detector.DetectDiagramType(null!);

        // Assert
        Assert.Equal(MermaidDiagramType.Unknown, result);
    }

    [Fact]
    public void DetectDiagramType_GraphKeyword_ReturnsFlowchart()
    {
        // Arrange
        var code = @"graph TD
    A --> B";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Flowchart, result);
    }

    [Fact]
    public void DetectDiagramType_FlowchartKeyword_ReturnsFlowchart()
    {
        // Arrange
        var code = @"flowchart LR
    A --> B";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Flowchart, result);
    }

    [Fact]
    public void DetectDiagramType_SequenceDiagram_ReturnsSequence()
    {
        // Arrange
        var code = @"sequenceDiagram
    Alice->>Bob: Hello";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Sequence, result);
    }

    [Fact]
    public void DetectDiagramType_Gantt_ReturnsGantt()
    {
        // Arrange
        var code = @"gantt
    title A Gantt Diagram
    section Section";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Gantt, result);
    }

    [Fact]
    public void DetectDiagramType_Mindmap_ReturnsMindmap()
    {
        // Arrange
        var code = @"mindmap
  Root
    A
      B";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Mindmap, result);
    }

    [Fact]
    public void DetectDiagramType_ClassDiagram_ReturnsClass()
    {
        // Arrange
        var code = @"classDiagram
    Animal <|-- Duck";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Class, result);
    }

    [Fact]
    public void DetectDiagramType_StateDiagram_ReturnsState()
    {
        // Arrange
        var code = @"stateDiagram
    [*] --> Still";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.State, result);
    }

    [Fact]
    public void DetectDiagramType_ErDiagram_ReturnsEr()
    {
        // Arrange
        var code = @"erDiagram
    CUSTOMER ||--o{ ORDER : places";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Er, result);
    }

    [Fact]
    public void DetectDiagramType_Pie_ReturnsPie()
    {
        // Arrange
        var code = @"pie
    title Pets
    ""Dogs"" : 386";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Pie, result);
    }

    [Fact]
    public void DetectDiagramType_Timeline_ReturnsTimeline()
    {
        // Arrange
        var code = @"timeline
    title History";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Timeline, result);
    }

    [Fact]
    public void DetectDiagramType_GitGraph_ReturnsGitGraph()
    {
        // Arrange
        var code = @"gitGraph
    commit";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.GitGraph, result);
    }

    [Fact]
    public void DetectDiagramType_Journey_ReturnsJourney()
    {
        // Arrange
        var code = @"journey
    title My journey";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Journey, result);
    }

    [Fact]
    public void DetectDiagramType_WithFrontmatter_SkipsFrontmatter()
    {
        // Arrange
        var code = @"---
layout: elk
---
flowchart LR
    A --> B";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Flowchart, result);
    }

    [Fact]
    public void DetectDiagramType_CaseInsensitive_ReturnsFlowchart()
    {
        // Arrange
        var code = @"GRAPH TD
    A --> B";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Flowchart, result);
    }

    [Fact]
    public void DetectDiagramType_NoMatchingKeyword_ReturnsUnknown()
    {
        // Arrange
        var code = @"invalidDiagram
    A --> B";

        // Act
        var result = _detector.DetectDiagramType(code);

        // Assert
        Assert.Equal(MermaidDiagramType.Unknown, result);
    }

    #endregion
}
