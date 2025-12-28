using MarkdownToWord.Models;

namespace MarkdownToWord.Tests.Models;

public class ConversionOptionsTests
{
    [Fact]
    public void ConversionOptions_RequiredProperties_CanBeSet()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx"
        };

        // Assert
        Assert.Equal(@"C:\test\input.md", options.InputPath);
        Assert.Equal(@"C:\test\output.docx", options.OutputPath);
    }

    [Fact]
    public void TemplatePath_CanBeNull()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx",
            TemplatePath = null
        };

        // Assert
        Assert.Null(options.TemplatePath);
    }

    [Fact]
    public void TemplatePath_CanBeSet()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx",
            TemplatePath = @"C:\test\template.docx"
        };

        // Assert
        Assert.Equal(@"C:\test\template.docx", options.TemplatePath);
    }

    [Fact]
    public void MermaidTimeoutSeconds_DefaultValue_Is30()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx"
        };

        // Assert
        Assert.Equal(30, options.MermaidTimeoutSeconds);
    }

    [Fact]
    public void MermaidTimeoutSeconds_CanBeCustomized()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx",
            MermaidTimeoutSeconds = 60
        };

        // Assert
        Assert.Equal(60, options.MermaidTimeoutSeconds);
    }

    [Fact]
    public void DiagramDpiScale_DefaultValue_Is2()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx"
        };

        // Assert
        Assert.Equal(2, options.DiagramDpiScale);
    }

    [Fact]
    public void DiagramDpiScale_CanBeCustomized()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx",
            DiagramDpiScale = 3
        };

        // Assert
        Assert.Equal(3, options.DiagramDpiScale);
    }

    [Fact]
    public void ConversionOptions_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var options = new ConversionOptions
        {
            InputPath = @"C:\test\input.md",
            OutputPath = @"C:\test\output.docx",
            TemplatePath = @"C:\test\template.docx",
            MermaidTimeoutSeconds = 45,
            DiagramDpiScale = 3
        };

        // Assert
        Assert.Equal(@"C:\test\input.md", options.InputPath);
        Assert.Equal(@"C:\test\output.docx", options.OutputPath);
        Assert.Equal(@"C:\test\template.docx", options.TemplatePath);
        Assert.Equal(45, options.MermaidTimeoutSeconds);
        Assert.Equal(3, options.DiagramDpiScale);
    }
}
