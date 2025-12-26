using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;
using Microsoft.Extensions.Logging;
using MarkdownToWord.Models;
using MarkdigDocument = Markdig.Syntax.MarkdownDocument;
using System.Text;
using System.Text.RegularExpressions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for generating Word documents from Markdown.
/// </summary>
public class WordGenerator : IWordGenerator
{
    private readonly ILogger<WordGenerator> _logger;
    private const int DefaultPageWidthTwips = 11906; // A4: 8.27 inches * 1440
    private const int DefaultMarginTwips = 1440; // 1 inch
    private const int SmallDiagramMaxWidthPx = 300; // Diagrams up to 300px width
    private const int SmallDiagramMaxHeightPx = 300; // Diagrams up to 300px height
    private const int MaxDisplayWidthPx = 619; // Maximum display width (page - margins)
    private int _currentDiagramIndex = 0;
    private IReadOnlyList<MermaidDiagram>? _currentDiagrams;
    private static readonly Regex NumberedHeadingRegex = new("^\\d+\\.\\s", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public WordGenerator(ILogger<WordGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the usable page width in pixels from a Word template.
    /// </summary>
    public async Task<int> GetUsablePageWidthAsync(string templatePath)
    {
        await Task.CompletedTask; // Async signature for future enhancements

        if (!File.Exists(templatePath))
        {
            // Return default usable width for A4 (595pt - 144pt margins = 451pt ≈ 601px at 96 DPI)
            _logger.LogWarning("Template not found, using default page width");
            return 601;
        }

        try
        {
            using var document = WordprocessingDocument.Open(templatePath, false);
            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                return 601;
            }

            // Get section properties
            var sectionProps = mainPart.Document.Body.Descendants<SectionProperties>().FirstOrDefault();
            if (sectionProps == null)
            {
                return 601;
            }

            // Get page size and margins
            var pageSize = sectionProps.GetFirstChild<PageSize>();
            var pageMargin = sectionProps.GetFirstChild<PageMargin>();

            if (pageSize?.Width == null)
            {
                return 601;
            }

            var pageWidthTwips = (int)pageSize.Width.Value;
            var leftMarginTwips = pageMargin?.Left?.Value ?? DefaultMarginTwips;
            var rightMarginTwips = pageMargin?.Right?.Value ?? DefaultMarginTwips;

            // Calculate usable width in twips, then convert to pixels (96 DPI)
            var usableWidthTwips = pageWidthTwips - leftMarginTwips - rightMarginTwips;
            var usableWidthPixels = (int)(usableWidthTwips / 1440.0 * 96);

            _logger.LogDebug("Template page width: {Width}px", usableWidthPixels);
            return usableWidthPixels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read template dimensions");
            return 601;
        }
    }

    /// <summary>
    /// Generates a Word document from parsed Markdown and rendered diagrams.
    /// </summary>
    public async Task GenerateAsync(
        ConversionOptions options,
        Models.MarkdownDocument markdownDocument,
        PlaceholderDictionary placeholders,
        IReadOnlyDictionary<int, byte[]> renderedDiagrams,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Word document: {Output}", options.OutputPath);

        // Copy template to output or create new document
        if (!string.IsNullOrEmpty(options.TemplatePath) && File.Exists(options.TemplatePath))
        {
            File.Copy(options.TemplatePath, options.OutputPath, overwrite: true);
            _logger.LogInformation("Copied template to output: {Template}", options.TemplatePath);
        }
        else
        {
            CreateEmptyDocument(options.OutputPath);
            _logger.LogInformation("Created new document at: {Output}", options.OutputPath);
        }

        // Open document for editing
        using var document = WordprocessingDocument.Open(options.OutputPath, true);
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("Document has no main part");
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Document has no body");

        // Replace placeholders if any
        if (placeholders.IsLoaded)
        {
            await ReplacePlaceholdersAsync(document, placeholders);
        }

        // Reset diagram index and store diagram list
        _currentDiagramIndex = 0;
        _currentDiagrams = markdownDocument.MermaidDiagrams;

        // Convert Markdown to OpenXml and append to body
        var parsedDoc = markdownDocument.ParsedDocument as MarkdigDocument;
        if (parsedDoc != null)
        {
            foreach (var block in parsedDoc)
            {
                var openXmlElements = ConvertMarkdownBlockToOpenXml(
                    block,
                    mainPart,
                    renderedDiagrams);

                if (openXmlElements != null && openXmlElements.Count > 0)
                {
                    var shouldStartNewPage = ShouldStartNewPage(block);

                    if (shouldStartNewPage && body.ChildElements.Count > 0)
                    {
                        body.AppendChild(CreatePageBreakParagraph());
                    }

                    foreach (var element in openXmlElements)
                    {
                        body.AppendChild(element);
                    }
                }
            }
        }

        mainPart.Document.Save();
        _logger.LogInformation("Word document generated successfully");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates an empty Word document.
    /// </summary>
    private void CreateEmptyDocument(string path)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
    }

    /// <summary>
    /// Replaces placeholders in the document.
    /// </summary>
    private async Task ReplacePlaceholdersAsync(
        WordprocessingDocument document,
        PlaceholderDictionary placeholders)
    {
        await Task.CompletedTask;

        _logger.LogInformation("Replacing {Count} placeholder(s)", placeholders.Replacements.Count);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        // Replace in main body paragraphs
        ReplacePlaceholdersInElement(body, placeholders);

        // Replace in headers
        if (document.MainDocumentPart != null)
        {
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                ReplacePlaceholdersInElement(headerPart.Header, placeholders);
            }

            // Replace in footers
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                ReplacePlaceholdersInElement(footerPart.Footer, placeholders);
            }
        }
    }

    /// <summary>
    /// Replaces placeholders in an OpenXml element by processing paragraphs.
    /// Handles cases where placeholders are split across multiple Text nodes.
    /// Uses case-insensitive matching.
    /// </summary>
    private void ReplacePlaceholdersInElement(OpenXmlElement element, PlaceholderDictionary placeholders)
    {
        foreach (var paragraph in element.Descendants<Paragraph>())
        {
            var fullText = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));

            // Check if any placeholder exists in this paragraph (case-insensitive)
            var hasPlaceholder = placeholders.Replacements.Keys.Any(key =>
                fullText.Contains(key, StringComparison.OrdinalIgnoreCase));

            if (!hasPlaceholder)
                continue;

            // Replace all placeholders in the full text (case-insensitive)
            var replacedText = fullText;
            foreach (var (placeholder, value) in placeholders.Replacements)
            {
                // Use Regex for case-insensitive replacement
                var pattern = System.Text.RegularExpressions.Regex.Escape(placeholder);
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (regex.IsMatch(replacedText))
                {
                    replacedText = regex.Replace(replacedText, value);
                    _logger.LogDebug("Replaced placeholder: {Placeholder} -> {Value}", placeholder, value);
                }
            }

            // If text changed, rebuild the paragraph's text nodes
            if (replacedText != fullText)
            {
                var textNodes = paragraph.Descendants<Text>().ToList();
                if (textNodes.Count > 0)
                {
                    // Put all replaced text in the first Text node
                    textNodes[0].Text = replacedText;

                    // Remove remaining Text nodes
                    for (int i = 1; i < textNodes.Count; i++)
                    {
                        textNodes[i].Remove();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Converts a Markdown block to OpenXml elements.
    /// </summary>
    private List<OpenXmlElement> ConvertMarkdownBlockToOpenXml(
        Block block,
        MainDocumentPart mainPart,
        IReadOnlyDictionary<int, byte[]> renderedDiagrams)
    {
        return block switch
        {
            HeadingBlock heading => new List<OpenXmlElement> { CreateHeading(heading) },
            ParagraphBlock paragraph when IsPageBreakParagraph(paragraph) =>
                new List<OpenXmlElement> { CreatePageBreakParagraph() },
            ParagraphBlock paragraph => new List<OpenXmlElement> { CreateParagraph(paragraph, mainPart, renderedDiagrams) },
            FencedCodeBlock codeBlock when codeBlock.Info?.ToLowerInvariant() == "mermaid" =>
                CreateMermaidDiagramParagraph(codeBlock, mainPart, renderedDiagrams) is Paragraph p
                    ? new List<OpenXmlElement> { p }
                    : new List<OpenXmlElement>(),
            FencedCodeBlock codeBlock => CreateCodeBlock(codeBlock),
            QuoteBlock quote => CreateQuoteBlock(quote, mainPart, renderedDiagrams),
            ThematicBreakBlock => new List<OpenXmlElement> { CreateHorizontalRule() },
            Markdig.Extensions.Tables.Table table => CreateTable(table),
            ListBlock list => CreateList(list, mainPart, renderedDiagrams),
            _ => new List<OpenXmlElement>()
        };
    }

    /// <summary>
    /// Creates a heading paragraph.
    /// </summary>
    private Paragraph CreateHeading(HeadingBlock heading)
    {
        var styleId = heading.Level switch
        {
            1 => "Heading1",
            2 => "Heading2",
            3 => "Heading3",
            _ => "Heading4"
        };

        var paragraph = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = styleId }));

        // Extract text from heading
        var inline = heading.Inline;
        if (inline != null)
        {
            var runs = inline
                .Select(CreateRunFromInline)
                .Where(run => run is not null);

            foreach (var run in runs)
            {
                paragraph.AppendChild(run);
            }
        }

        return paragraph;
    }

    /// <summary>
    /// Creates a normal paragraph.
    /// </summary>
    private Paragraph CreateParagraph(
        ParagraphBlock paragraphBlock,
        MainDocumentPart mainPart,
        IReadOnlyDictionary<int, byte[]> renderedDiagrams)
    {
        var paragraph = new Paragraph();

        var inline = paragraphBlock.Inline;
        if (inline != null)
        {
            var runs = inline
                .Select(CreateRunFromInline)
                .Where(run => run != null);

            foreach (var run in runs)
            {
                paragraph.AppendChild(run);
            }
        }

        return paragraph;
    }

    /// <summary>
    /// Creates a code block with language label, background, and border.
    /// </summary>
    private List<OpenXmlElement> CreateCodeBlock(FencedCodeBlock codeBlock)
    {
        var elements = new List<OpenXmlElement>();
        var language = codeBlock.Info?.Trim();

        // Add language label if specified
        if (!string.IsNullOrEmpty(language))
        {
            var labelParagraph = new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "120", After = "0" },
                    new Indentation { Left = "0" }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" },
                        new FontSize { Val = "18" }, // 9pt
                        new Color { Val = "666666" }
                    ),
                    new Text(language.ToUpperInvariant()) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            elements.Add(labelParagraph);
        }

        // Create code block with background and border
        var code = codeBlock.Lines.ToString();
        var codeLines = code.Split('\n');

        foreach (var line in codeLines)
        {
            var codeParagraph = new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = "Normal" },
                    new SpacingBetweenLines
                    {
                        Before = "0",
                        After = "0",
                        Line = "240",
                        LineRule = LineSpacingRuleValues.Auto
                    },
                    new Indentation { Left = "240" }, // 0.17 inch indentation
                    new ParagraphBorders(
                        new LeftBorder
                        {
                            Val = BorderValues.Single,
                            Color = "D0D0D0",
                            Size = 4,
                            Space = 1
                        },
                        new RightBorder
                        {
                            Val = BorderValues.Single,
                            Color = "D0D0D0",
                            Size = 4,
                            Space = 1
                        },
                        new TopBorder
                        {
                            Val = BorderValues.Single,
                            Color = "D0D0D0",
                            Size = 4,
                            Space = 1
                        },
                        new BottomBorder
                        {
                            Val = BorderValues.Single,
                            Color = "D0D0D0",
                            Size = 4,
                            Space = 1
                        }
                    ),
                    new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Color = "auto",
                        Fill = "F5F5F5" // Very light gray background
                    }
                )
            );

            var run = new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" },
                    new FontSize { Val = "20" }, // 10pt
                    new Color { Val = "333333" }
                ),
                new Text(string.IsNullOrEmpty(line) ? " " : line) { Space = SpaceProcessingModeValues.Preserve }
            );

            codeParagraph.AppendChild(run);
            elements.Add(codeParagraph);
        }

        // Add spacing after code block
        var spacingParagraph = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "120" }
            )
        );
        elements.Add(spacingParagraph);

        return elements;
    }

    /// <summary>
    /// Creates a paragraph with embedded Mermaid diagram image.
    /// </summary>
    private Paragraph? CreateMermaidDiagramParagraph(
        FencedCodeBlock codeBlock,
        MainDocumentPart mainPart,
        IReadOnlyDictionary<int, byte[]> renderedDiagrams)
    {
        // Get the diagram for the current index
        if (!renderedDiagrams.TryGetValue(_currentDiagramIndex, out var imageData))
        {
            _logger.LogWarning("No rendered diagram found for index {Index}", _currentDiagramIndex);
            _currentDiagramIndex++;
            return null;
        }

        try
        {
            var paragraph = new Paragraph();
            var run = paragraph.AppendChild(new Run());

            // Add image part
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var stream = new MemoryStream(imageData))
            {
                imagePart.FeedData(stream);
            }

            var imagePartId = mainPart.GetIdOfPart(imagePart);

            // Calculate image dimensions from PNG header
            var (width, height) = GetPngDimensions(imageData);

            // Scale down to maintain original document size while keeping high resolution
            // DeviceScaleFactor=3 means images are 3x larger, so divide by 3
            var displayWidth = width / 3;
            var displayHeight = height / 3;

            // SINGLE RULE: Small diagrams (≤300px width AND ≤300px height) take up 100% of the width
            if (displayWidth <= SmallDiagramMaxWidthPx && displayHeight <= SmallDiagramMaxHeightPx)
            {
                var scaleFactor = (double)MaxDisplayWidthPx / displayWidth;
                displayWidth = MaxDisplayWidthPx;
                displayHeight = (int)(displayHeight * scaleFactor);

                _logger.LogDebug(
                    "Small diagram {Index} scaled to full width by {Factor:F2}x: {Width}x{Height}px",
                    _currentDiagramIndex,
                    scaleFactor,
                    displayWidth,
                    displayHeight);
            }
            else
            {
                _logger.LogDebug(
                    "Diagram {Index} rendered at original size: {Width}x{Height}px",
                    _currentDiagramIndex,
                    displayWidth,
                    displayHeight);
            }

            var widthEmus = (long)(displayWidth * 9525); // pixels to EMUs
            var heightEmus = (long)(displayHeight * 9525);

            // Create the image element
            var element = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmus, Cy = heightEmus },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = (uint)_currentDiagramIndex + 1, Name = $"Diagram {_currentDiagramIndex}" },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = $"Diagram{_currentDiagramIndex}.png" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = imagePartId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                                    new A.PresetGeometry(
                                        new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }))
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

            run.AppendChild(element);

            _logger.LogInformation("Inserted diagram {Index} into document", _currentDiagramIndex);
            _currentDiagramIndex++;

            return paragraph;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert diagram {Index}", _currentDiagramIndex);
            _currentDiagramIndex++;
            return null;
        }
    }

    /// <summary>
    /// Creates a list (bullet or numbered).
    /// </summary>
    private List<OpenXmlElement> CreateList(ListBlock list, MainDocumentPart mainPart, IReadOnlyDictionary<int, byte[]> renderedDiagrams)
    {
        var elements = new List<OpenXmlElement>();

        foreach (var listItem in list.OfType<ListItemBlock>())
        {
            var paragraph = new Paragraph();

            // Add numbering/bullet formatting
            var paragraphProperties = new ParagraphProperties();
            var numberingProperties = new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = list.IsOrdered ? 1 : 2 }
            );
            paragraphProperties.AppendChild(numberingProperties);
            paragraph.AppendChild(paragraphProperties);

            // Process all blocks in the list item
            foreach (var paraBlock in listItem.OfType<ParagraphBlock>())
            {
                var inline = paraBlock.Inline;
                if (inline == null)
                {
                    continue;
                }

                var runs = inline
                    .Select(CreateRunFromInline)
                    .OfType<Run>();

                foreach (var run in runs)
                {
                    paragraph.AppendChild(run);
                }
            }

            elements.Add(paragraph);
        }

        return elements;
    }

    private static bool ShouldStartNewPage(Block block)
    {
        // Match headings or paragraphs starting with an integer and a dot (e.g., "1. Objetivo")
        var text = ExtractBlockText(block);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return NumberedHeadingRegex.IsMatch(text);
    }

    private static string ExtractBlockText(Block block)
    {
        switch (block)
        {
            case HeadingBlock heading when heading.Inline != null:
                return string.Concat(heading.Inline.Select(i => (i as LiteralInline)?.Content.ToString() ?? string.Empty)).Trim();
            case ParagraphBlock paragraph when paragraph.Inline != null:
                return string.Concat(paragraph.Inline.Select(i => (i as LiteralInline)?.Content.ToString() ?? string.Empty)).Trim();
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a ParagraphBlock represents a page break.
    /// </summary>
    private bool IsPageBreakParagraph(ParagraphBlock paragraph)
    {
        if (paragraph.Inline == null)
        {
            return false;
        }

        // Extract text from the paragraph
        var textBuilder = new StringBuilder();
        foreach (var literal in paragraph.Inline.OfType<LiteralInline>())
        {
            textBuilder.Append(literal.Content);
        }

        var text = textBuilder.ToString().Trim();

        // Check if it matches pagebreak pattern
        // Valid formats: ---pagebreak, --- pagebreak, ---PAGEBREAK, etc.
        if (text.StartsWith("---"))
        {
            var afterDashes = text.Substring(3).Trim();
            return afterDashes.Equals("pagebreak", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static Paragraph CreatePageBreakParagraph()
    {
        return new Paragraph(new Run(new Break { Type = BreakValues.Page }));
    }

    /// <summary>
    /// Extracts PNG dimensions from PNG byte array.
    /// </summary>
    private (int width, int height) GetPngDimensions(byte[] pngData)
    {
        // PNG signature: 8 bytes
        // IHDR chunk starts at byte 8
        // Width: bytes 16-19 (big-endian)
        // Height: bytes 20-23 (big-endian)

        if (pngData.Length < 24)
        {
            _logger.LogWarning("Invalid PNG data - too short");
            return (800, 600); // Default fallback
        }

        int width = (pngData[16] << 24) | (pngData[17] << 16) | (pngData[18] << 8) | pngData[19];
        int height = (pngData[20] << 24) | (pngData[21] << 16) | (pngData[22] << 8) | pngData[23];

        return (width, height);
    }

    /// <summary>
    /// Creates a Run from a Markdown inline element.
    /// </summary>
    private Run? CreateRunFromInline(Inline inline)
    {
        return inline switch
        {
            LiteralInline literal => new Run(new Text(literal.Content.ToString())
            { Space = SpaceProcessingModeValues.Preserve }),

            EmphasisInline emphasis => CreateEmphasisRun(emphasis),

            CodeInline code => new Run(
                new RunProperties(new RunStyle { Val = "CodeChar" }),
                new Text(code.Content) { Space = SpaceProcessingModeValues.Preserve }),

            LinkInline link => CreateLinkRun(link),

            LineBreakInline => new Run(new Break()),

            _ => null
        };
    }

    /// <summary>
    /// Creates a run with emphasis (bold/italic/strikethrough).
    /// </summary>
    private Run CreateEmphasisRun(EmphasisInline emphasis)
    {
        var runProps = new RunProperties();

        if (emphasis.DelimiterChar == '*' || emphasis.DelimiterChar == '_')
        {
            if (emphasis.DelimiterCount == 2)
            {
                runProps.AppendChild(new Bold());
            }
            else
            {
                runProps.AppendChild(new Italic());
            }
        }
        else if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2)
        {
            // Strikethrough: ~~texto~~
            runProps.AppendChild(new Strike());
        }

        var text = ExtractTextFromInline(emphasis);
        return new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    /// <summary>
    /// Creates a hyperlink run from a Markdown link.
    /// </summary>
    private Run CreateLinkRun(LinkInline link)
    {
        var runProps = new RunProperties(
            new Underline { Val = UnderlineValues.Single },
            new Color { Val = "0563C1" } // Word hyperlink blue
        );

        var text = ExtractTextFromInline(link);
        var run = new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve });


        //Note: Actual hyperlink creation in Word requires more complex handling with Hyperlink elements.
        // For simplicity, we return just the run here.
        // The URL will be visible only if the user edits the document.
        return run;
    }

    /// <summary>
    /// Creates a horizontal rule (thematic break).
    /// </summary>
    private Paragraph CreateHorizontalRule()
    {
        return new Paragraph(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Color = "CCCCCC",
                        Size = 6,
                        Space = 1
                    }
                ),
                new SpacingBetweenLines { Before = "120", After = "120" }
            )
        );
    }

    /// <summary>
    /// Extracts text content from an inline element.
    /// </summary>
    private string ExtractTextFromInline(Inline inline)
    {
        if (inline is LiteralInline literal)
        {
            return literal.Content.ToString();
        }

        if (inline is ContainerInline container)
        {
            var textBuilder = new StringBuilder();
            foreach (var child in container)
            {
                textBuilder.Append(ExtractTextFromInline(child));
            }
            return textBuilder.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Creates a Word table from a Markdown table.
    /// </summary>
    private List<OpenXmlElement> CreateTable(Markdig.Extensions.Tables.Table markdownTable)
    {
        var elements = new List<OpenXmlElement>();
        var wordTable = new DocumentFormat.OpenXml.Wordprocessing.Table();

        // Set table properties
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Color = "000000", Size = 4, Space = 0 },
                new BottomBorder { Val = BorderValues.Single, Color = "000000", Size = 4, Space = 0 },
                new LeftBorder { Val = BorderValues.Single, Color = "000000", Size = 4, Space = 0 },
                new RightBorder { Val = BorderValues.Single, Color = "000000", Size = 4, Space = 0 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Color = "000000", Size = 4, Space = 0 },
                new InsideVerticalBorder { Val = BorderValues.Single, Color = "000000", Size = 4, Space = 0 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }, // 100% da largura
            new TableLayout { Type = TableLayoutValues.Autofit }
        );
        wordTable.AppendChild(tableProperties);

        // Process table rows
        foreach (var tableRow in markdownTable.OfType<Markdig.Extensions.Tables.TableRow>())
        {
            var wordRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();

            // Process cells
            foreach (var tableCell in tableRow.OfType<Markdig.Extensions.Tables.TableCell>())
            {
                var wordCell = new DocumentFormat.OpenXml.Wordprocessing.TableCell();

                // Set cell properties
                var cellProperties = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Auto },
                    new TableCellMargin(
                        new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                        new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                        new LeftMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                        new RightMargin { Width = "100", Type = TableWidthUnitValues.Dxa }
                    )
                );

                // If it's a header row, add light gray background
                if (tableRow.IsHeader)
                {
                    cellProperties.AppendChild(new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Color = "auto",
                        Fill = "F5F5F5" // Very light gray
                    });
                }

                wordCell.AppendChild(cellProperties);

                // Add content to cell
                // TableCell contains blocks (usually ParagraphBlock)
                foreach (var paragraphBlock in tableCell.OfType<ParagraphBlock>())
                {
                    var paragraph = new Paragraph();

                    if (paragraphBlock.Inline != null)
                    {
                        foreach (var inline in paragraphBlock.Inline)
                        {
                            // If it's a header, add bold
                            if (tableRow.IsHeader)
                            {
                                var run = CreateRunFromInline(inline);
                                if (run != null)
                                {
                                    // Add bold to the run
                                    if (run.RunProperties == null)
                                    {
                                        run.RunProperties = new RunProperties();
                                    }
                                    run.RunProperties.AppendChild(new Bold());
                                    paragraph.AppendChild(run);
                                }
                            }
                            else
                            {
                                var run = CreateRunFromInline(inline);
                                if (run != null)
                                {
                                    paragraph.AppendChild(run);
                                }
                            }
                        }
                    }

                    wordCell.AppendChild(paragraph);
                }

                // If the cell is empty, add an empty paragraph
                if (!wordCell.Elements<Paragraph>().Any())
                {
                    wordCell.AppendChild(new Paragraph());
                }

                wordRow.AppendChild(wordCell);
            }

            wordTable.AppendChild(wordRow);
        }

        elements.Add(wordTable);

        // Add spacing after table
        var spacingParagraph = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "120" }
            )
        );
        elements.Add(spacingParagraph);

        return elements;
    }

    /// <summary>
    /// Creates a blockquote (quote block) with special formatting.
    /// </summary>
    private List<OpenXmlElement> CreateQuoteBlock(
        QuoteBlock quoteBlock,
        MainDocumentPart mainPart,
        IReadOnlyDictionary<int, byte[]> renderedDiagrams)
    {
        var elements = new List<OpenXmlElement>();

        // Process each block inside the quote
        foreach (var block in quoteBlock)
        {
            if (block is ParagraphBlock paragraphBlock)
            {
                var paragraph = new Paragraph(
                    new ParagraphProperties(
                        new ParagraphStyleId { Val = "Normal" },
                        new SpacingBetweenLines { Before = "80", After = "80" },
                        new Indentation { Left = "720" }, // 0.5 inch indentation
                        new ParagraphBorders(
                            new LeftBorder
                            {
                                Val = BorderValues.Single,
                                Color = "CCCCCC",
                                Size = 12,
                                Space = 4
                            }
                        ),
                        new Shading
                        {
                            Val = ShadingPatternValues.Clear,
                            Color = "auto",
                            Fill = "F9F9F9" // Very light gray background
                        }
                    )
                );

                // Add paragraph content
                var inline = paragraphBlock.Inline;
                if (inline != null)
                {
                    var runs = inline
                        .Select(CreateRunFromInline)
                        .OfType<Run>();

                    foreach (var run in runs)
                    {
                        // Add italic for quote emphasis
                        if (run.RunProperties == null)
                        {
                            run.RunProperties = new RunProperties();
                        }
                        run.RunProperties.AppendChild(new Italic());
                        run.RunProperties.AppendChild(new Color { Val = "666666" });
                        paragraph.AppendChild(run);
                    }
                }

                elements.Add(paragraph);
            }
            else
            {
                // For other types of blocks inside the quote, process recursively
                var nestedElements = ConvertMarkdownBlockToOpenXml(block, mainPart, renderedDiagrams);
                elements.AddRange(nestedElements);
            }
        }

        return elements;
    }
}
