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
- **Services/Parsers/{SlicerName}/**: Each slicer has its own directory containing its parser class (implementing `IGcodeParser`)
- **Templates/{SlicerName}/default.txt** and **full.txt**: The built-in note templates, compiled in as embedded resources and loaded through `Services/EmbeddedNoteTemplate.cs`. They are LF-only (enforced by `.gitattributes`) and the loader strips the trailing newline so rendered notes are byte-identical across platforms
- **CuraSettingDto.cs**: Main DTO sent to the API

### Supported Slicers

OrcaSlicer, PrusaSlicer, Bambu Studio, FLSun Slicer, Anycubic Slicer Next

### Installer / Wizard

- **Services/AppMode.cs**: Enum (PostProcess, Wizard, Install, Uninstall) — detected from CLI args in ArgumentParser
- **Services/Installer/ISlicerProfileInstaller.cs**: Interface for each slicer family's installer
- **Services/Installer/OrcaFamily/OrcaFamilyProfileInstaller.cs**: Abstract base with all JSON profile logic
- **Services/Installer/SlicerConfigRoot.cs**: Pure, testable resolver for the per-OS config root (`%APPDATA%`, `~/Library/Application Support`, `$XDG_CONFIG_HOME`/`~/.config`). Use `SpecialFolder.UserProfile` for home, never `Personal` (that is `~/Documents` on Unix)
- **Services/Installer/OrcaFamily/**: Concrete subclasses — OrcaSlicerInstaller, SnapmakerOrcaInstaller, AnycubicSlicerNextInstaller (one property each)
- **Services/Installer/SlicerInstallerRegistry.cs**: Registry of all supported installers — add new slicers here
- **Services/Installer/WizardService.cs**: Interactive terminal wizard driving the install/uninstall flow (`install`/`uninstall` verbs, `--dry-run`)

Rules the installer must keep, because users' own profiles live next to ours:

- The installer only ever creates, edits or deletes files it owns: `<profile> - 3DPrintLog.json` overrides (`IsInstallerOwned` = file name suffix). A hand-made profile that already runs the uploader is left alone (only a stale executable path is refreshed, keeping the user's flags), its parent is skipped, and `uninstall` reports it rather than touching it
- Our `post_process` entry is recognised by executable path (`IsOurEntry`), not just by file name, so a renamed or moved binary is still found
- System profiles are read with `JsonDocument` (real vendor files contain duplicate keys, which `JsonNode` rejects); unreadable files are counted, never fatal
- Profiles are written indented with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` so quoted Windows paths and non-ASCII stay legible

### Adding a New OrcaSlicer Fork

1. Create a subclass of `OrcaFamilyProfileInstaller` in `Services/Installer/OrcaFamily/`
2. Set `SlicerName` (display name) and `SlicerDirectoryName` (config folder name on disk, e.g. `"Snapmaker_Orca"`)
3. Register it in `SlicerInstallerRegistry.All`
4. Add a pruned copy of a real config tree under `Slic3rPostProcessingUploaderUnitTests/TestData/Installer/{SlicerDirectoryName}/` (system vendor index + a few process profiles, one user account) and list it in `InstallerFixture.All` so the data-driven installer tests cover it

### Template System

Templates use `{{setting_name}}` placeholders that get replaced with values from G-code comments like `; setting_name = value`. Built-in templates are `.txt` files under `Slic3rPostProcessingUploader/Templates/`; custom ones come from `--template <path>` via `NoteTemplateFromFile`. Both implement `INoteTemplate`.

A placeholder that is alone on its line renders as a block: each line of the value inherits the indentation, CRLF templates keep CRLF, and an empty value removes the line (the heading above stays). Inline placeholders are substituted verbatim.

### Computed placeholders

`Services/Parsers/Computed/` holds placeholders whose value comes from code (`ComputedPlaceholder(Key, Render)`), currently `{{filament_profiles}}` (distinct filament profiles with counts), `{{models}}` (per-object size from a streaming scan of the whole file, `ObjectBoundsScanner`) and `{{modified_settings}}`. The last two render a whole section — heading included, trailing newline — so that on a line of their own they vanish entirely when empty. Rules:

- A parser lists what it supports in `ComputedPlaceholders`: `OrcaParser`, `BambuStudioParser` and `PrusaParser` do today. A placeholder runs only when the active template references its key, so templates without `{{models}}` never pay for the full-file scan
- `Render` must never throw: catch, `context.DebugLog(...)`, and return empty or a degraded value. The uploader must not fail a print log because of a note section
- Everything a placeholder may touch comes from `ParseOptions` (stream opener, debug log, slicer config roots). `ParseGcode(string)` uses `ParseOptions.InMemory`, which has no file or config access, so unit tests and fixture snapshots are hermetic; `Program.cs` passes the real file and `SlicerInstallerRegistry.PresetConfigRoots` (Orca family + Bambu Studio + PrusaSlicer; each locator only recognises its own folder layout)
- Computed keys are excluded from the parser-factory heuristic score and from `TemplatePlaceholderCoverageTests`
- `{{modified_settings}}` has two implementations. `ModifiedSettingsPlaceholder` (Orca, Bambu) reads `different_settings_to_system` and splits it into saved/unsaved via the user's JSON presets (`UserPresetLocator`); Bambu writes no `inherits_group`, so it always gets the flat list. `PrusaModifiedSettingsPlaceholder` has no list to read: it diffs the config block against the preset the file was sliced with, rebuilt by `PrusaPresetLocator` from the user's full-dump `.ini` (preferred) or the vendor bundle section resolved through its `inherits` chain (`vendor/*.ini`; the section must exist in exactly one bundle), and reports every difference as unsaved, skipping any preset it cannot pin down. Value comparison tolerates the slicer's serialisation quirks: a single saved per-extruder value expanded per slot in the G-code, a single G-code string repeated per slot in the presets, and percents written bare in a bundle. `TestData/PrusaSlicer/config/vendor/PrusaResearch.ini` is a pruned real bundle (only the sections the fixture presets inherit) used by the real-export tests
- `ObjectBoundsScanner` understands every slicer's marker dialect (`; printing object … id:n copy m`, Bambu's `; start printing object, unique label id: n` + `; Z_HEIGHT:` + `; FEATURE:`, PrusaSlicer's `M486 S<n>`/`M486 A<name>`, Klipper's `EXCLUDE_OBJECT_START NAME=`). Orca writes comment markers *and* `M486` for the same instance, so once a comment marker is seen the firmware commands are ignored. Bambu files carry no object names (`Object 67`); `.bgcode` toolpaths are not decoded, so `{{models}}` is empty for them

## Testing

Uses MSTest with Snapshooter for snapshot testing. Real G-code fixtures live under `Slic3rPostProcessingUploaderUnitTests/TestData/{SlicerName}/` and are copied to the test output directory. Use `TestData.Load(relativePath)` for a named fixture and `TestData.EnumerateFixtures(slicerFolder)` for data-driven coverage of every version in a slicer's folder.

Each fixture test must verify that `ParserFactory` selects the expected parser without emitting its unrecognized-slicer warning, then parse and snapshot the result. Use the fixture filename as the snapshot name and hash the `settings.Snapshot` field to avoid cross-platform line-ending issues:

```csharp
Snapshot.Match(
    result,
    Path.GetFileNameWithoutExtension(fixturePath),
    matchOptions => matchOptions.HashField("settings.Snapshot"));
```

Keep fixtures compact by replacing unused toolpath bodies with a short omission marker while preserving the header, thumbnails, print summary, and trailing configuration. Retain one untrimmed fixture for `GcodeWindowTests`. A fixture that should exercise `{{models}}` needs whole layers kept (the object markers repeat per layer), including the last layer for the height; `orcaslicer-2.4.0-benchy-x16.gcode` keeps four and `bambustudio-01.10.01.50-calibration-cube-two-filament.gcode` keeps layers 1-3 and 123-128. `prusaslicer-2.9.2-mk4s-mmu3-shape-box-cylinder.gcode` (firmware `M486` labelling) keeps layers 1, 2, 124 and 125.

### Installer tests

Installer unit tests run against pruned copies of real slicer config trees under `Slic3rPostProcessingUploaderUnitTests/TestData/Installer/{slicer}/` (see `InstallerFixture`). Two manual end-to-end checks drive the real wizard against a real OrcaSlicer — `scripts/e2e-linux/run.ps1` (Docker, headless) and `scripts/e2e-windows/run.ps1` (Windows Sandbox, real `%APPDATA%` and a quoted `Program Files` path). They are pre-release checks, not CI (each takes ~8 minutes and needs Docker Desktop or the Windows Sandbox feature); run one after changing `WizardService`, the profile-writing code or the config-root resolution. Each README explains what it proves and how to recalibrate click coordinates when the slicer's layout changes. The `docs/images/` screenshots come from the Linux run.

## Adding a New Slicer

1. Create `Services/Parsers/{SlicerName}/` directory
2. Implement parser class deriving from `GcodeParserBase` with a static `Is{SlicerName}(string gcode)` detection method
3. Create `Templates/{SlicerName}/default.txt` and `full.txt` and add both as `<EmbeddedResource>` entries in `Slic3rPostProcessingUploader.csproj` (the `LogicalName` must be `Templates/{SlicerName}/{kind}.txt`). The parser's `CreateDefaultTemplate()` returns `EmbeddedNoteTemplate.Default("{SlicerName}")`
4. Add one `SlicerRegistration` line to the `Slicers` array in `ParserFactory.cs` (name, detection method, both template factories, parser factory). The name is used for the `{Name}PercentMatch` telemetry event
5. Add a real G-code fixture under `Slic3rPostProcessingUploaderUnitTests/TestData/{SlicerName}/`
6. Add data-driven parser-factory, parsing, and snapshot coverage for every fixture in that folder
7. Add the slicer to `EmbeddedNoteTemplateTests`, `TemplatePlaceholderCoverageTests`, and the template table in the README

## Debug Mode

```bash
Slic3rPostProcessingUploader.exe --debug C:\path\to\debug\
```

Outputs: environment variables, raw G-code, parsed DTO, and API response to the specified directory.

## Documentation

When making user-facing changes (new CLI flags, new features, behavior changes), update the README.md to document them. The Options section in the README should match the available CLI arguments in ArgumentParser.cs.
