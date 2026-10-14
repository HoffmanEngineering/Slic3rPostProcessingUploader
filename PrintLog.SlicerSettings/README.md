# PrintLog.SlicerSettings

Reads the settings that OrcaSlicer, PrusaSlicer, Bambu Studio, FLSun Slicer and Anycubic Slicer Next write into
G-code, and renders the [3D Print Log](https://www.3dprintlog.com) note templates from them. It is the parsing code of
the [Slic3r post-processing uploader](https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader), packaged so
a server or another connector gets the same results without porting the parsers. It has no console, browser,
network or telemetry dependencies and is trim- and AOT-safe.

## Parsing a settings block

```csharp
using PrintLog.SlicerSettings;

SlicerConfigResult result = SlicerConfigParser.Parse(configBlock, header);

result.Slicer;        // "OrcaSlicer"
result.Version;       // "2.4.0" ("Unknown" without a header)
result.IsRecognized;  // true when the header's slicer marker identified it
result.Settings["print_settings_id"];  // every "; key = value" setting, case-insensitive keys
result.Note;          // the slicer's default note template, rendered
result.Dto;           // the full parse: print time, filament usage, note, thumbnail
```

`configBlock` is the settings text the slicer writes, without toolpaths:

| Slicer | Settings block |
| --- | --- |
| OrcaSlicer, Anycubic Slicer Next | the comment lines after the last toolpath command, from `; EXECUTABLE_BLOCK_END` to the end of the file |
| PrusaSlicer | the comment lines after the last toolpath command: the `; filament used` totals through `; prusaslicer_config = end` |
| Bambu Studio | `; CONFIG_BLOCK_START` to `; CONFIG_BLOCK_END`, which Bambu Studio writes near the top of the file |

`header` is optional: the start of the file through `; HEADER_BLOCK_END` (PrusaSlicer: its first line). Send it when
you can. It carries the slicer name and version, and for Bambu Studio the print time and filament totals. Without it
the version is `Unknown` and the slicer is guessed from the settings, which cannot tell OrcaSlicer forks such as
Anycubic Slicer Next apart from OrcaSlicer itself.

`Parse` never reads the file system or the network and never throws on malformed or empty text. Note sections computed
from more than settings degrade:

- `{{filament_profiles}}` is complete.
- `{{modified_settings}}` lists every setting that differs from the system profile, but cannot split saved from
  unsaved changes, because that needs the user's preset files on the slicing machine.
- `{{models}}` is always empty: object sizes are measured from the toolpaths.

## Whole files

`SlicerDetector.Detect(gcode)` picks the slicer for a whole file (or any part of it) and creates a parser for any note
template. `GcodeWindow.ReadFromFile` reads only the head and tail of a large file, and `BinaryGcode` decodes
PrusaSlicer `.bgcode` metadata into the equivalent ASCII header.
