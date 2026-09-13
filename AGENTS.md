# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Publish single-file executables for all platforms
dotnet publish --configuration Release
```

## Architecture

This is a .NET 10 console application that acts as a post-processing script for 3D printer slicers. After a slicer exports G-code, this tool parses the file, extracts print settings, and opens https://www.3dprintlog.com with pre-filled print details.

### Data Flow

```
G-code file → ArgumentParser → ParserFactory → Slicer-specific Parser → CuraSettingDto → API POST → Browser opens
```

### Key Components

- **Program.cs**: Entry point; wiring only (argument parsing, telemetry setup, debug logging, browser launch)
- **Services/UploadService.cs**: Posts the DTO to the API over an injected `HttpClient`; every failure becomes a `UserFacingException`
- **Services/PrintMetadata.cs**: Pure helpers for `file_name`/`print_name` (honours `SLIC3R_PP_OUTPUT_NAME`) and the new-print URL
- **ArgumentParser.cs**: CLI argument handling (`--default`, `--full`, `--template`, `--debug`, etc.)
- **ParserFactory.cs**: A `SlicerRegistration` registry (one entry per slicer). Detection is the first registration whose `Detect` matches; otherwise every registration's full template is scored against the G-code and the best match wins (ties go to the earlier entry, so Orca is the ultimate default)
- **Services/Parsers/{SlicerName}/**: Each slicer has its own directory with:
  - Parser class implementing `IGcodeParser`
  - Default and Full note templates
- **CuraSettingDto.cs**: Main DTO sent to the API

### Supported Slicers

OrcaSlicer, PrusaSlicer, Bambu Studio, FLSun Slicer, Anycubic Slicer Next

### Template System

Templates use `{{setting_name}}` placeholders that get replaced with values from G-code comments like `; setting_name = value`.

## Testing

Uses MSTest with Snapshooter for snapshot testing. Real G-code fixtures live under `Slic3rPostProcessingUploaderUnitTests/TestData/{SlicerName}/` and are copied to the test output directory. Use `TestData.Load(relativePath)` for a named fixture and `TestData.EnumerateFixtures(slicerFolder)` for data-driven coverage of every version in a slicer's folder.

Each fixture test must verify that `ParserFactory` selects the expected parser without emitting its unrecognized-slicer warning, then parse and snapshot the result. Use the fixture filename as the snapshot name and hash the `settings.Snapshot` field to avoid cross-platform line-ending issues:

```csharp
Snapshot.Match(
    result,
    Path.GetFileNameWithoutExtension(fixturePath),
    matchOptions => matchOptions.HashField("settings.Snapshot"));
```

Keep fixtures compact by replacing unused toolpath bodies with a short omission marker while preserving the header, thumbnails, print summary, and trailing configuration. Retain one untrimmed fixture for `GcodeWindowTests`.

## Adding a New Slicer

1. Create `Services/Parsers/{SlicerName}/` directory
2. Implement parser class deriving from `GcodeParserBase` with a static `Is{SlicerName}(string gcode)` detection method
3. Create `{SlicerName}DefaultNoteTemplate` and `{SlicerName}FullNoteTemplate` classes
4. Add one `SlicerRegistration` line to the `Slicers` array in `ParserFactory.cs` (name, detection method, both template factories, parser factory). The name is used for the `{Name}PercentMatch` telemetry event
5. Add a real G-code fixture under `Slic3rPostProcessingUploaderUnitTests/TestData/{SlicerName}/`
6. Add data-driven parser-factory, parsing, and snapshot coverage for every fixture in that folder

## Debug Mode

```bash
Slic3rPostProcessingUploader.exe --debug C:\path\to\debug\
```

Outputs: environment variables, raw G-code, parsed DTO, and API response to the specified directory.

## Documentation

When making user-facing changes (new CLI flags, new features, behavior changes), update the README.md to document them. The Options section in the README should match the available CLI arguments in ArgumentParser.cs.
