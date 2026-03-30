using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MarkdownToWord.Models;

namespace MarkdownToWord.Services;

/// <summary>
/// Service responsible for rendering Mermaid diagrams to PNG images using Playwright.
/// </summary>
public sealed class MermaidRenderer : IMermaidRenderer
{
    private const string MermaidVersion = "11.12.0";
    private const int DefaultTimeoutMs = 30000;
    private const int DefaultViewportWidth = 3840;
    private const int DefaultViewportHeight = 2160;
    private const int DeviceScale = 3;
    private const int MaxRetryAttempts = 3;
    private const int InitialRetryDelayMs = 1000;

    private readonly ILogger<MermaidRenderer> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _initialized;

    public MermaidRenderer(ILogger<MermaidRenderer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the shared browser instance (idempotent and thread-safe).
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            _playwright = await Playwright.CreateAsync();
            _browser = await LaunchBrowserWithFallbackAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<IBrowser> LaunchBrowserWithFallbackAsync(CancellationToken cancellationToken)
    {
        var channels = new[] { "msedge", "chrome" };

        foreach (var channel in channels)
        {
            try
            {
                var browser = await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Channel = channel,
                    Headless = true
                });

                _logger.LogInformation("Browser initialized successfully: {Channel}", channel);
                return browser;
            }
            catch (PlaywrightException ex)
            {
                _logger.LogWarning(ex, "Browser {Channel} not available", channel);
            }
        }

        throw new InvalidOperationException("No compatible browser found. Please install Microsoft Edge or Google Chrome.");
    }

    /// <summary>
    /// Checks if the browser is healthy and ready to use.
    /// </summary>
    private async Task<bool> IsBrowserHealthyAsync()
    {
        if (_browser is null || !_initialized)
        {
            return false;
        }

        try
        {
            // Try to check if browser is still connected
            return _browser.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resets the browser state to allow reinitialization.
    /// </summary>
    private void ResetBrowserState()
    {
        _browser = null;
        _initialized = false;
        _logger.LogWarning("Browser state reset - will reinitialize on next attempt");
    }

    /// <summary>
    /// Renders a diagram with retry logic and browser recovery.
    /// </summary>
    public async Task<byte[]> RenderDiagramWithRetryAsync(
        MermaidDiagram diagram,
        int? pageWidthInPixels = null,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetryAttempts)
        {
            attempt++;
            try
            {
                // Check browser health before attempting render
                if (!await IsBrowserHealthyAsync())
                {
                    _logger.LogWarning("Browser unhealthy, reinitializing before attempt {Attempt}", attempt);
                    ResetBrowserState();
                    await InitializeAsync(cancellationToken);
                }

                var result = await RenderDiagramAsync(diagram, pageWidthInPixels, cancellationToken);
                
                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "Diagram {Position} rendered successfully on attempt {Attempt}/{Max}",
                        diagram.Position, attempt, MaxRetryAttempts);
                }
                
                return result;
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                lastException = ex;
                var delay = InitialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                
                _logger.LogWarning(
                    ex,
                    "Diagram {Position} failed on attempt {Attempt}/{Max}. Retrying in {Delay}ms...",
                    diagram.Position, attempt, MaxRetryAttempts, delay);

                // Reset browser state on any exception
                ResetBrowserState();
                
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError(
            lastException,
            "Diagram {Position} failed after {Attempts} attempts",
            diagram.Position, MaxRetryAttempts);
        
        throw new InvalidOperationException(
            $"Failed to render diagram {diagram.Position} after {MaxRetryAttempts} attempts. See inner exception for details.",
            lastException);
    }

    public async Task<byte[]> RenderDiagramAsync(
        MermaidDiagram diagram,
        int? pageWidthInPixels = null,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        if (_browser is null)
        {
            throw new InvalidOperationException("Browser not initialized");
        }

        _logger.LogInformation(
            "Rendering Mermaid diagram: Type={Type}, Layout={Layout}, Position={Position}",
            diagram.DiagramType,
            diagram.LayoutType,
            diagram.Position);

        var viewportWidth = diagram.RequiresProportionalWidth && pageWidthInPixels.HasValue
            ? pageWidthInPixels.Value
            : DefaultViewportWidth;

        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = viewportWidth,
                    Height = DefaultViewportHeight
                },
                DeviceScaleFactor = DeviceScale
            });

            page = await context.NewPageAsync();

            var html = GenerateHtmlTemplate(diagram);
            await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await page.WaitForFunctionAsync(
                "() => window.renderComplete === true",
                new PageWaitForFunctionOptions { Timeout = DefaultTimeoutMs });

            var renderError = await page.EvaluateAsync<string>("() => window.renderError ?? ''");
            if (!string.IsNullOrEmpty(renderError))
            {
                throw new InvalidOperationException($"Mermaid rendering failed: {renderError}");
            }

            var svgHandle = await page.QuerySelectorAsync("#container svg")
                ?? throw new InvalidOperationException("Could not find rendered SVG element");

            var boundingBox = await svgHandle.BoundingBoxAsync();
            if (boundingBox == null)
            {
                throw new InvalidOperationException("Could not get dimensions of rendered SVG");
            }

            _logger.LogDebug(
                "Diagram rendered: {Width}x{Height}px",
                boundingBox.Width,
                boundingBox.Height);

            var imageBytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                Clip = new Clip
                {
                    X = boundingBox.X,
                    Y = boundingBox.Y,
                    Width = boundingBox.Width,
                    Height = boundingBox.Height
                }
            });

            _logger.LogInformation(
                "Successfully rendered diagram {Position}: {Size} bytes",
                diagram.Position,
                imageBytes.Length);

            return imageBytes;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Timeout waiting for Mermaid diagram to render");
            throw new TimeoutException($"Mermaid diagram rendering timeout after {DefaultTimeoutMs}ms", ex);
        }
        catch (PlaywrightException ex) when (
            ex.Message.Contains("Target page", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("context", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("browser has been closed", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Protocol error", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                ex,
                "Browser connection lost for diagram {Position}: {Message}",
                diagram.Position,
                ex.Message);
            
            // Reset browser state so it can be reinitialized
            ResetBrowserState();
            throw;
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(
                ex,
                "Playwright error rendering diagram {Position}: {Message}",
                diagram.Position,
                ex.Message);
            throw;
        }
        finally
        {
            if (page is not null)
            {
                await page.CloseAsync();
            }

            if (context is not null)
            {
                await context.CloseAsync();
            }
        }
    }

    public async Task<IReadOnlyList<byte[]>> RenderDiagramsBatchAsync(
        IEnumerable<MermaidDiagram> diagrams,
        int? pageWidthInPixels = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<byte[]>();

        foreach (var diagram in diagrams)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var image = await RenderDiagramAsync(diagram, pageWidthInPixels, cancellationToken);
            results.Add(image);
        }

        return results;
    }

    private string GenerateHtmlTemplate(MermaidDiagram diagram)
    {
        var escapedCode = diagram.Code
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("${", "\\${");

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ 
            margin: 0; 
            padding: 20px; 
            background: white; 
            font-family: Arial, sans-serif;
        }}
        #container {{ 
            display: inline-block; 
        }}
    </style>
</head>
<body>
    <div id=""container""></div>
    <script type=""module"">
        import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@{MermaidVersion}/dist/mermaid.esm.min.mjs';
        import elkLayouts from 'https://cdn.jsdelivr.net/npm/@mermaid-js/layout-elk@0.1.4/dist/mermaid-layout-elk.esm.min.mjs';
    
        mermaid.registerLayoutLoaders(elkLayouts);
    
        mermaid.initialize({{
            startOnLoad: false,
            theme: 'default',
            securityLevel: 'loose',
            flowchart: {{ 
                htmlLabels: true,
                curve: 'basis'
            }}
        }});
    
        const diagramCode = `{escapedCode}`;
    
        try {{
            const {{ svg }} = await mermaid.render('diagram', diagramCode);
            document.getElementById('container').innerHTML = svg;
            window.renderComplete = true;
            window.renderError = null;
        }} catch (error) {{
            console.error('Mermaid render error:', error);
            window.renderComplete = true;
            window.renderError = error.message;
        }}
    </script>
</body>
</html>";
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        _initLock.Dispose();
        _initialized = false;

        _logger.LogInformation("MermaidRenderer disposed");
    }
}
