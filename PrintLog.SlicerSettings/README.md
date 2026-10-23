# PrintLog.SlicerSettings

Reads the settings that OrcaSlicer, PrusaSlicer, Bambu Studio, FLSun Slicer and Anycubic Slicer Next write into
G-code, and renders the [3D Print Log](https://www.3dprintlog.com) note templates from them. It is the parsing code of
the [Slic3r post-processing uploader](https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader), packaged so
a server or another connector gets the same results without porting the parsers. It has no console, browser,
network or telemetry dependencies and is trim- and AOT-safe.

**Versions 0.x are unstable:** the API may change between minor versions until 1.0.0.

## Parsing a settings block

```csharp
using PrintLog.SlicerSettings;

SlicerConfigResult result = SlicerConfigParser.Parse(configBlock, header);

result.Slicer;                     // "OrcaSlicer"
result.Version;                    // "2.4.0" ("Unknown" without a header)
result.IsRecognized;               // true when the header's slicer marker identified it
result.Settings["print_settings_id"]; // every "; key = value" setting, case-insensitive keys
result.Note;                       // the slicer's default note template, rendered
result.EstimatedPrintTimeSeconds;  // null when the text has no estimate
result.TotalFilamentMg;            // null when absent
result.TotalFilamentLengthMm;      // null when absent
result.Filaments;                  // per slot: SlotIndex (from 0), LengthMm, Colour, Type, Description
```

The result is immutable. Totals that are absent, zero, negative or out of range are null, never 0.

`configBlock` is the settings text the slicer writes, without toolpaths:

| Slicer | Settings block |
| --- | --- |
| OrcaSlicer, Anycubic Slicer Next | after the toolpaths: from `; EXECUTABLE_BLOCK_END` to the end of the file |
| PrusaSlicer | after the toolpaths: the `; filament used` totals through `; prusaslicer_config = end` |
| Bambu Studio, FLSun Slicer | near the top: after `; HEADER_BLOCK_END` through `; CONFIG_BLOCK_END` (FLSun writes its time and filament totals just before `; CONFIG_BLOCK_START`) |

`header` is optional: the start of the file through `; HEADER_BLOCK_END` (PrusaSlicer: its first line). Send it when
you can. It carries the slicer name and version, and for Bambu Studio the print time and filament totals. Without it
the version is `Unknown` and the slicer is guessed from the settings, which cannot tell OrcaSlicer forks such as
Anycubic Slicer Next apart from OrcaSlicer itself.

## Untrusted input

`Parse` is meant to run on text a connector sends:

- It never reads the file system or the network.
- It never throws, whatever the text. Malformed values are left out. If parsing fails outright, the result has slicer
  and version `Unknown`, no settings and an empty note, and the reason goes to the optional `debugLog` callback.
- Its work is linear in the length of the text.
- **Cap what you accept.** A settings block is tens of kilobytes. Text over 4,000,000 characters (header and block
  together) is windowed like a large G-code file: only the first and last 2,000,000 characters are read.
- Thumbnails are not extracted.

## What a settings block cannot give you

- `{{filament_profiles}}` is complete.
- `{{modified_settings}}` lists every setting that differs from the system profile, but cannot split saved from
  unsaved changes, because that needs the user's preset files on the slicing machine.
- `{{models}}` is always empty: object sizes are measured from the toolpaths.

## Parsing an uploaded G-code file

A slicer plugin that has the whole file can send it (or its first and last 2,000,000 characters), plus what only the
slicing machine has, and get back exactly the `settings` JSON the uploader would post for that file:

```csharp
var options = new SlicerGcodeOptions(
    Template: "default",                          // or "full"; anything else throws ArgumentException
    Models: [new ModelBounds("3DBenchy.stl", 100.1, 159.6, 95.2, 125.7, 48.0)],
    UserPresets: [new UserPresetFile("process", "0.20mm Standard @MyPrinter - 3DPrintLog", presetJson)])
{
    OutputName = @"C:\Prints\benchy_final.gcode", // optional: sets file_name and print_name
    DebugLog = message => logger.LogDebug(message),
};

SlicerGcodeResult result = SlicerGcodeParser.Parse(gcode, options);

result.Slicer;        // "OrcaSlicer"
result.Version;       // "2.4.0"
result.IsRecognized;  // true when the header's slicer marker identified it
result.SettingsJson;  // {"note": ..., "print_name": ..., "estimated_print_time_seconds": ..., "material_used_mg": ...,
                      //  "snapshot": ..., "file_name": ..., "filamentUsage": [...]}
```

- `Models` are the raw extrusion centerline boxes per object instance (what the uploader's toolpath scan measures);
  the note adds the outer-wall line width. Empty means no models section. At most 1,000 are used, and names are cut
  to 256 characters.
- `UserPresets` are the user's own preset files (`user/<account>/<kind>/<name>.json`). They split
  `{{modified_settings}}` into saved and unsaved changes exactly as the uploader does with the files on disk; when a
  user preset the G-code names has no file, or two, the section is the flat list.
- `OutputName` gives `file_name` (the last path segment, `/` or `\`) and `print_name` (`Benchy Final`); without it
  both are null.
- The thumbnail is extracted. Binary G-code is not decoded: send text.
- It never reads the file system or the network, never throws for any text or list contents (a parse that fails
  outright gives slicer and version `Unknown` and an empty note), and is linear in its input. Cap what you accept.

## Known gaps

- **Snapmaker Orca** has no test fixture yet, so it has no detection of its own. Its files are parsed as OrcaSlicer, and
  the version is `Unknown`.
- Without the header, Anycubic Slicer Next is also reported as OrcaSlicer (see above).
