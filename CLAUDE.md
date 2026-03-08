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

- **Program.cs**: Entry point with DI setup and Application Insights telemetry
- **ArgumentParser.cs**: CLI argument handling (`--default`, `--full`, `--template`, `--debug`, etc.)
- **ParserFactory.cs**: Detects slicer type from G-code markers, falls back to template match scoring
- **Services/Parsers/{SlicerName}/**: Each slicer has its own directory with:
  - Parser class implementing `IGcodeParser`
  - Default and Full note templates
- **CuraSettingDto.cs**: Main DTO sent to the API

### Supported Slicers

OrcaSlicer, PrusaSlicer, Bambu Studio, FLSun Slicer, Anycubic Slicer Next

### Installer / Wizard

- **Services/AppMode.cs**: Enum (PostProcess, Wizard, Install, Uninstall) — detected from CLI args in ArgumentParser
- **Services/Installer/ISlicerProfileInstaller.cs**: Interface for each slicer family's installer
- **Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs**: Abstract base with OS-aware config root resolution and all JSON profile logic
- **Services/Installer/OrcaFamily/**: Concrete subclasses — OrcaSlicerInstaller, SnapmakerOrcaInstaller, AnycubicSlicerNextInstaller (one property each)
- **Services/Installer/SlicerInstallerRegistry.cs**: Registry of all supported installers — add new slicers here
- **Services/Installer/WizardService.cs**: Interactive terminal wizard driving the install/uninstall flow

### Adding a New OrcaSlicer Fork

1. Create a subclass of `OrcaFamilyProfileInstaller` in `Services/Installer/OrcaFamily/`
2. Set `SlicerName` (display name) and `SlicerDirectoryName` (config folder name on disk, e.g. `"Snapmaker_Orca"`)
3. Register it in `SlicerInstallerRegistry.All`

### Template System

Templates use `{{setting_name}}` placeholders that get replaced with values from G-code comments like `; setting_name = value`.

## Testing

Uses MSTest with Snapshooter for snapshot testing. Snapshots hash the `settings.Snapshot` field to avoid cross-platform line-ending issues:

```csharp
Snapshot.Match(result, matchOptions => matchOptions.HashField("settings.Snapshot"));
```

## Adding a New Slicer

1. Create `Services/Parsers/{SlicerName}/` directory
2. Implement parser class with `IGcodeParser` interface and static `Is{SlicerName}(string gcode)` detection method
3. Create `{SlicerName}DefaultNoteTemplate` and `{SlicerName}FullNoteTemplate` classes
4. Register in `ParserFactory.GetParser()` and add `Build{SlicerName}Parser()` method
5. Add tests with snapshot verification

## Debug Mode

```bash
Slic3rPostProcessingUploader.exe --debug C:\path\to\debug\
```

Outputs: environment variables, raw G-code, parsed DTO, and API response to the specified directory.

## Documentation

When making user-facing changes (new CLI flags, new features, behavior changes), update the README.md to document them. The Options section in the README should match the available CLI arguments in ArgumentParser.cs.
