# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.1] - 2026-03-30

### Fixed
- 🐛 Fixed critical browser crash issue during parallel Mermaid diagram rendering
  - Added automatic retry mechanism with exponential backoff (up to 3 attempts)
  - Implemented browser health checks and automatic recovery
  - Added comprehensive error handling for browser connection losses
  - Parallel rendering now continues even if individual diagrams fail
  - Browser automatically reinitializes after crashes
  - Improved logging with detailed failure reporting

### Added
- ✅ Page break support using `---pagebreak` syntax in Markdown
  - Creates actual page breaks in Word documents
  - Case-insensitive (works with `---pagebreak`, `---PAGEBREAK`, etc.)
  - Accepts both `---pagebreak` and `--- pagebreak` formats
  - Distinguishes from horizontal rules (`---`)
- ✅ New `RenderDiagramWithRetryAsync` method in `IMermaidRenderer` for resilient diagram rendering

## [1.0.0] - 2025-12-20

### 🎉 Initial Release

#### Added
- ✅ Complete Markdown to Word (DOCX) conversion
- ✅ Support for 11 Mermaid diagram types:
  - Flowchart
  - Sequence Diagram
  - Gantt Chart
  - Class Diagram
  - State Diagram
  - Entity Relationship Diagram
  - Mindmap
  - Timeline
  - Git Graph
  - User Journey
  - Pie Chart
- ✅ Dagre and ELK layout engine support for diagrams
- ✅ Dynamic placeholder substitution system
- ✅ Custom Word template support
- ✅ Markdown formatting preservation:
  - Headings (H1-H4)
  - Bold, Italic, Strikethrough
  - Code blocks and inline code
  - Lists (numbered and bulleted)
  - Tables with headers
  - Blockquotes
  - Horizontal rules (`---`)
  - Page breaks (`---pagebreak`)
  - Hyperlinks
- ✅ CLI with comprehensive validation
- ✅ High-quality diagram rendering via Playwright
- ✅ Structured logging with Microsoft.Extensions.Logging
- ✅ Cross-platform support (Windows, Linux, macOS)

#### Technical Details
- Built with .NET 10.0 (LTS)
- Zero warnings build configuration
- Service-based architecture with dependency injection
- Interface-driven design for testability
- Comprehensive error handling

#### Documentation
- Complete README with examples
- Project history documentation
- Contributing guidelines
- MIT License

### Known Limitations
- Hyperlinks rendered as styled text (not clickable)
- External images not yet supported
- No automatic Table of Contents generation

---

## Future Releases

See the [Roadmap](README.md#-roadmap) section in the README for planned features.

---

[1.0.0]: https://github.com/yourusername/MarkdownToWord/releases/tag/v1.0.0
