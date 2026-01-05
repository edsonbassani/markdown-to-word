# Version 1.1.0 Release Summary

## Issue Fixed
The markdown to Word converter was ignoring list items starting with `-` (dash) instead of converting them to bullet points in the Word document.

## Root Cause
The `WordGenerator.CreateList` method was creating list items with NumberingId references (Val = 1 for ordered lists, Val = 2 for unordered lists), but the document did not have the required numbering definitions XML part. Without the `NumberingDefinitionsPart`, Word ignores all list formatting.

## Solution Implemented

### 1. Added Numbering Definitions Support
- Created new method `EnsureNumberingDefinitions()` in [WordGenerator.cs](src/Services/WordGenerator.cs)
- This method automatically creates numbering definitions for both:
  - Ordered lists (numbered: 1, 2, 3...)
  - Unordered lists (bullets: ●)
- Definitions include proper indentation and formatting
- Uses Calibri font for bullet character (●) to ensure consistent rendering across platforms
- Changed from Symbol font to avoid empty rectangle rendering issue

### 2. Integration
- Modified `GenerateAsync()` method to call `EnsureNumberingDefinitions()` before processing markdown content
- This ensures all documents (even those without templates) have proper list support

### 3. Version Updates
Updated version from 1.0.0 to 1.1.0 across all relevant files:
- [src/md2word.csproj](src/md2word.csproj) - Updated Version, PackageReleaseNotes, and Copyright year
- [CHANGELOG.md](CHANGELOG.md) - Added version 1.1.0 entry with detailed changes
- [README.md](README.md) - Updated example version references

### 4. NuGet Package
- Created new package: `md2word.1.1.0.nupkg` in the `nupkg` directory
- Package metadata updated with deprecation notice for version 1.0.0
- Ready for publishing to NuGet.org

## Files Changed
1. **src/Services/WordGenerator.cs**
   - Added `EnsureNumberingDefinitions()` method (72 lines)
   - Modified `GenerateAsync()` to initialize numbering definitions
   
2. **src/md2word.csproj**
   - Version: 1.0.0 → 1.1.0
   - PackageReleaseNotes updated
   - Copyright year: 2025 → 2026

3. **CHANGELOG.md**
   - Added [1.1.0] section with bug fix details
   - Added deprecation notice for v1.0.0

4. **README.md**
   - Updated version references in examples

## Testing
- ✅ All 82 existing unit tests pass
- ✅ Build completes successfully with zero errors and zero warnings
- ✅ Manual test with [test-lists.md](test-lists.md) confirmed bullet points now render correctly

## Publishing Instructions

To publish the new version to NuGet.org:

```bash
dotnet nuget push nupkg/md2word.1.1.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

To mark version 1.0.0 as deprecated on NuGet.org:
- Log in to NuGet.org
- Navigate to the md2word package
- Select version 1.0.0
- Check "List in search results" and add deprecation message

## Backward Compatibility
This is a bug fix release with no breaking changes. All existing functionality remains intact while adding proper list rendering support.
