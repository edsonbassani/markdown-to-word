using System.CommandLine;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MarkdownToWord.Models;
using MarkdownToWord.Services;

namespace MarkdownToWord;

/// <summary>
/// Main entry point for the Markdown to Word converter CLI application.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole();
        });

        var logger = loggerFactory.CreateLogger<Program>();

        // Initialize services
        var layoutDetectorLogger = loggerFactory.CreateLogger<MermaidLayoutDetector>();
        var layoutDetector = new MermaidLayoutDetector(layoutDetectorLogger);

        var markdownParserLogger = loggerFactory.CreateLogger<MarkdownParser>();
        var markdownParser = new MarkdownParser(layoutDetector, markdownParserLogger);

        var mermaidRendererLogger = loggerFactory.CreateLogger<MermaidRenderer>();
        var mermaidRenderer = new MermaidRenderer(mermaidRendererLogger);

        var placeholderServiceLogger = loggerFactory.CreateLogger<PlaceholderService>();
        var placeholderService = new PlaceholderService(placeholderServiceLogger);

        var wordGeneratorLogger = loggerFactory.CreateLogger<WordGenerator>();
        var wordGenerator = new WordGenerator(wordGeneratorLogger);

        // Define CLI options
        var inputOption = new Option<string>(
            aliases: new[] { "--input", "-i" },
            description: "Full path to the input markdown file")
        {
            IsRequired = true
        };

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Full path where the .docx file will be saved")
        {
            IsRequired = true
        };

        var templateOption = new Option<string?>(
            aliases: new[] { "--template", "-t" },
            description: "Full path to the Word template (.docx)")
        {
            IsRequired = false
        };

        // Create root command
        var rootCommand = new RootCommand("Markdown to Word Converter - Converts Markdown files with Mermaid diagrams to professional Word documents.")
        {
            inputOption,
            outputOption,
            templateOption
        };

        // Set command handler
        rootCommand.SetHandler(async (input, output, template) =>
        {
            try
            {
                logger.LogInformation("Starting Markdown to Word conversion");
                logger.LogInformation("Input: {Input}", input);
                logger.LogInformation("Output: {Output}", output);

                if (!string.IsNullOrEmpty(template))
                {
                    logger.LogInformation("Template: {Template}", template);
                }

                // Validate input file exists
                if (!File.Exists(input))
                {
                    logger.LogError("Input file not found: {Input}", input);
                    Environment.ExitCode = 1;
                    return;
                }

                // Validate template exists if provided
                if (!string.IsNullOrEmpty(template) && !File.Exists(template))
                {
                    logger.LogError("Template not found: {Template}", template);
                    Environment.ExitCode = 1;
                    return;
                }

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    logger.LogInformation("Output directory created: {OutputDir}", outputDir);
                }

                // Initialize shared browser (Edge/Chrome)
                logger.LogInformation("Initializing browser for Mermaid rendering...");
                await mermaidRenderer.InitializeAsync();

                // Create conversion options
                var options = new ConversionOptions
                {
                    InputPath = input,
                    OutputPath = output,
                    TemplatePath = template
                };

                // Load placeholders
                logger.LogInformation("Loading placeholders...");
                var placeholders = await placeholderService.LoadPlaceholdersAsync(input);
                if (placeholders.IsLoaded)
                {
                    placeholderService.ValidatePlaceholders(placeholders);
                }

                // Parse Markdown
                logger.LogInformation("Parsing Markdown file...");
                var markdownDoc = await markdownParser.ParseAsync(input);

                // Render Mermaid diagrams
                var renderedDiagrams = new ConcurrentDictionary<int, byte[]>();
                var failedDiagrams = new ConcurrentBag<int>();

                if (markdownDoc.MermaidDiagrams.Count > 0)
                {
                    logger.LogInformation("Rendering {Count} Mermaid diagram(s)...", markdownDoc.MermaidDiagrams.Count);

                    // Get page width for proportional diagrams
                    int? pageWidth = null;
                    if (!string.IsNullOrEmpty(template) && markdownDoc.MermaidDiagrams.Any(d => d.RequiresProportionalWidth))
                    {
                        pageWidth = await wordGenerator.GetUsablePageWidthAsync(template);
                    }

                    var total = markdownDoc.MermaidDiagrams.Count;
                    var completed = 0;
                    var maxDegree = Math.Max(1, Math.Min(Environment.ProcessorCount, 4));

                    await Parallel.ForEachAsync(
                        markdownDoc.MermaidDiagrams,
                        new ParallelOptions { MaxDegreeOfParallelism = maxDegree },
                        async (diagram, ct) =>
                        {
                            try
                            {
                                var imageData = await mermaidRenderer.RenderDiagramWithRetryAsync(
                                    diagram,
                                    diagram.RequiresProportionalWidth ? pageWidth : null,
                                    ct);

                                renderedDiagrams[diagram.Position] = imageData;

                                var done = Interlocked.Increment(ref completed);
                                logger.LogInformation("Progress: {Done}/{Total} diagrams rendered", done, total);
                            }
                            catch (Exception ex)
                            {
                                failedDiagrams.Add(diagram.Position);
                                logger.LogError(
                                    ex,
                                    "Failed to render diagram at position {Position} (Type: {Type}, Layout: {Layout})",
                                    diagram.Position,
                                    diagram.DiagramType,
                                    diagram.LayoutType);
                                
                                // Continue processing other diagrams
                                var done = Interlocked.Increment(ref completed);
                                logger.LogInformation("Progress: {Done}/{Total} diagrams processed (with failures)", done, total);
                            }
                        });

                    // Report rendering results
                    var successCount = renderedDiagrams.Count;
                    var failureCount = failedDiagrams.Count;
                    
                    if (failureCount > 0)
                    {
                        logger.LogWarning(
                            "Diagram rendering completed with failures: {Success} succeeded, {Failed} failed. Failed positions: [{Positions}]",
                            successCount,
                            failureCount,
                            string.Join(", ", failedDiagrams.OrderBy(x => x)));
                        
                        if (successCount == 0)
                        {
                            logger.LogError("All diagrams failed to render. Document will be generated without diagram images.");
                        }
                    }
                    else
                    {
                        logger.LogInformation("All {Count} diagrams rendered successfully!", successCount);
                    }
                }

                // Generate Word document
                logger.LogInformation("Generating Word document...");
                await wordGenerator.GenerateAsync(
                    options,
                    markdownDoc,
                    placeholders,
                    renderedDiagrams.ToDictionary(kv => kv.Key, kv => kv.Value));

                logger.LogInformation("✅ Conversion completed successfully!");
                logger.LogInformation("Document generated: {Output}", output);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error during conversion");
                Environment.ExitCode = 1;
            }
            finally
            {
                await mermaidRenderer.DisposeAsync();
            }
        }, inputOption, outputOption, templateOption);

        return await rootCommand.InvokeAsync(args);
    }
}
