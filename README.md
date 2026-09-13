![3D Print Log logo](docs/assets/3d-print-log-logo_8b178eb1339b.svg)

# Slic3r Post-Processing Uploader for 3D Print Log

A Slic3r/PrusaSlicer/OrcaSlicer/Bambu Studio Post-Processing script for uploading print details to https://www.3dprintlog.com.

[3D Print Log](https://www.3dprintlog.com) is a simple tool for tracking 3D prints. Easily record details of your 3D printers, filament usage, print status, print duration and more. Keeps track the filament remaining on your spools, and we have Cura/Octoprint/Moonraker, and now Slic3r integrations.

This Slic3r Uploader will parse your gcode file after export and open up https://www.3dprintlog.com with the print details automatically filled out.

Create a free account at https://www.3dprintlog.com today and enjoy all the features.

## Currently Supported Slicers:

This is a new project, currently a work in progress. Feel free to help out by submitting requests, bug reports, or helping to make Pull Requests.

- OrcaSlicer
- PrusaSlicer (2.x and 3.0 alpha, ASCII `.gcode` and binary `.bgcode`)
- Bambu Studio
- FLSun Slicer
- Anycubic Slicer Next

#### PrusaSlicer binary G-code (.bgcode)

Printers that require binary G-code (MK4/MK4S, MINI+, XL, CORE One) are supported: the uploader reads the metadata, print summary, settings and PNG thumbnail straight from the `.bgcode` container, so nothing changes in your slicer setup. The G-code toolpaths themselves are never decoded. If a file uses a compression scheme this tool does not understand, it says so and suggests re-exporting as ASCII.

#### PrusaSlicer 3.0

PrusaSlicer 3.0 still has the `Post-processing scripts` print setting, so this uploader works there unchanged. The 3.0 Lua plugin system is a separate thing: it can only add objects to the plate and cannot see exported G-code or reach the network, so a post-processing script remains the way to log prints from PrusaSlicer. Verified against 3.0.0-alpha11; the format is still experimental, so report anything that stops parsing.

#### Partial Support for: (templates with full settings up next)

- Super Slicer

## Usage:

Download the [latest release for your operating system](https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases), and save the file to a location on your computer.

| Operating system | Download |
| --- | --- |
| Windows (x64) | `Slic3rPostProcessingUploader-win-x64.exe` |
| macOS (Apple Silicon) | `Slic3rPostProcessingUploader-osx-arm64` |
| macOS (Intel) | `Slic3rPostProcessingUploader-osx-x64` |
| Linux (x64) | `Slic3rPostProcessingUploader-linux-x64` |

On macOS and Linux, mark the file executable after downloading (`chmod +x Slic3rPostProcessingUploader-osx-arm64`).

In the Slicer's 'Post-Processing Scripts' section, add the path to this file. Full/Absolute paths are recommended:

### Examples:

Windows:

`C:\\uploader\\Slic3rPostProcessingUploader.exe [options]`

Mac/Linux:

`~/uploader/Slic3rPostProcessingUploader [options]`

#### Example of OrcaSlicer on Windows:

![Screenshot of OrcaSlicer with the Other Tab visible. In the Post-Processing Scripts section, we see the example of the absolute path to a windows .exe](docs/assets/OrcaSlicerExample.png)

## Options:

`--help`, `-h`: Display this help message. No settings will be uploaded if help is displayed.

`--version`, `-v`: Display the version number and exit.

`--local-dev`: Use the local development environment

`--debug <path>`: Save debug information to the specified path. Note that debug mode reads and logs the entire G-code file, so it is slower on large files than a normal run, which only reads the start and end of the file where the slicer writes its settings. For binary G-code the logged file contents are the decoded metadata, not the raw binary.

`--dry-run`: Parse the G-code and print the rendered note, the key parsed fields (slicer, version, print time, filament usage, whether a thumbnail was found), and the DTO JSON to the console, then exit with code 0. Nothing is uploaded to 3dprintlog.com and no browser is opened, so this is the quickest way to check a custom `--template` before using it for real. When combined with `--debug <path>`, the DTO JSON is written to `<path>/slic3r-dto.json` instead of the console. Startup/parse telemetry is still sent (tagged `DryRun=true`) unless `--opt-out-telemetry` is also given; no upload event is sent.

Example: `Slic3rPostProcessingUploader --dry-run --template C:\templates\my.txt C:\prints\benchy.gcode`

`--opt-out-telemetry`: Disable telemetry tracking. To help improve the plugin, we track slicer and plugin versions, as well as log errors that are thrown. No personal data is collected.

### Update notifications

Each run also asks GitHub for the [latest release](https://github.com/HoffmanEngineering/Slic3rPostProcessingUploader/releases/latest). When it is newer than the version you are running, a note is printed at the end of the output with a link to the release so you can upgrade to get the latest features. The check runs alongside the upload, is silent when GitHub cannot be reached, and is skipped for development builds (`0.0.0-dev`).

### Note Template Options:

`--default`: Use the default note template, which contains a curated list of general settings. Preferred by most users. The Default template is used if no other note template option is given.

`--full`: Use the full note template, which lists most of the settings available in the slicers

`--template <path>`: Use a custom note template. Absolute paths work better. See README for more details on syntax

Any other `--flag` or `-x` that isn't one of the options above is reported as an error rather than silently ignored.

## Console Output & Troubleshooting

A normal run prints a short progress summary and then opens your browser:

```
3D Print Log Uploader v1.1.2
  ✓ Detected OrcaSlicer 2.3.0
  ✓ Parsed benchy.gcode (default template, 12 ms)
  ✓ Uploaded print settings to 3dprintlog.com
  → Opening https://www.3dprintlog.com/prints/new/cura?...
```

If something goes wrong, the uploader prints what happened and what to do about it, keeps the console window open for 30 seconds (or until a key is pressed) so the message can be read, and exits with code `1` so the slicer can report the failure:

```
  ✗ Could not reach 3dprintlog.com.
    Check your internet connection and try again.
```

Stack traces and raw API responses are only shown on screen when running with `--debug <path>`; they are always written to `slic3r-debug.txt` in the debug folder.

## Example

In your Slicer's Post Processing text box, input:

`Slic3rPostProcessingUploader --default`

Once you slice your object and export the `.gcode` file, this plugin will run and open your default web browser to https://www.3dprintlog.com with all of the print details and settings filled out.

### Thumbnails

If your slicer embeds thumbnails in the G-code, the highest resolution one up to 720p (1280x720) is used as the print image. PNG and JPG thumbnails are supported; QOI thumbnails are ignored. To get a sharper image, add a larger size (e.g. `300x300`) to your printer's thumbnail settings in the slicer.

### Multi-Color Prints

For multi-filament prints (OrcaSlicer, Bambu Studio, Anycubic Slicer Next, PrusaSlicer MMU/XL), each filament usage entry is sent with a note describing the slicer slot it came from, its basic color, and its material, for example:

- `Slot 1 · Red (#E72F1D) · PLA`
- `Slot 2 · Black (#080A0D) · PLA`
- `Slot 4 · Blue (#0078BF) · PETG`

The color name is derived from the slicer's `filament_colour` setting, so you can match each entry to the right spool on 3D Print Log even when the printer has filaments loaded in a different order than the slicer. Slots with no usage are omitted.

## Note Templates

If the provided `default` or `full` templates are not to your liking, you can create custom note templates by passing in the path to a text file containing the template. These can look for specific settings in the gcode file, and pull out the data.

The built-in templates are plain text files using the same syntax, so they make good starting points for a custom template (copy one, edit it, and pass it with `--template`). Improvements to them are welcome as pull requests:

| Slicer | Default | Full |
| --- | --- | --- |
| OrcaSlicer | [default.txt](Slic3rPostProcessingUploader/Templates/OrcaSlicer/default.txt) | [full.txt](Slic3rPostProcessingUploader/Templates/OrcaSlicer/full.txt) |
| PrusaSlicer | [default.txt](Slic3rPostProcessingUploader/Templates/PrusaSlicer/default.txt) | [full.txt](Slic3rPostProcessingUploader/Templates/PrusaSlicer/full.txt) |
| Bambu Studio | [default.txt](Slic3rPostProcessingUploader/Templates/BambuStudio/default.txt) | [full.txt](Slic3rPostProcessingUploader/Templates/BambuStudio/full.txt) |
| FLSun Slicer | [default.txt](Slic3rPostProcessingUploader/Templates/FLSunSlicer/default.txt) | [full.txt](Slic3rPostProcessingUploader/Templates/FLSunSlicer/full.txt) |
| Anycubic Slicer Next | [default.txt](Slic3rPostProcessingUploader/Templates/AnycubicSlicerNext/default.txt) | [full.txt](Slic3rPostProcessingUploader/Templates/AnycubicSlicerNext/full.txt) |

Example:
`Slic3rPostProcessingUploader --template "C:\tmp\my-custom-template.txt"`

### Template Syntax

Any plain text will be displayed as-is in the note.

You can use `{{setting_name}}` to identify settings from the gcode file to use. This program will then look for `; setting_name = some value`, and replace `{{setting_name}}` with `some value`.

For example, OrcaSlicer's gcode files have a section at the bottom containing all the configuration like:

```
; CONFIG_BLOCK_START
; accel_to_decel_enable = 1
; accel_to_decel_factor = 50%
; activate_air_filtration = 0
; activate_chamber_temp_control = 0
; adaptive_bed_mesh_margin = 0
; adaptive_pressure_advance = 0
; adaptive_pressure_advance_bridges = 0
; adaptive_pressure_advance_model = "0,0,0\n0,0,0"
; adaptive_pressure_advance_overhangs = 0
...
```

If `my-custom-template.txt` contained the template:

```
This is my template.

I used these settings:
- Activate Air Filtration: {{activate_air_filtration}}
- Use Adaptive Pressure Advance: {{adaptive_pressure_advance}}
- Acceleration to Deceleration: {{accel_to_decel_factor}}
```

Then 3D Print Log will receive a note that is rendered like:

```
This is my template.

I used these settings:
- Activate Air Filtration: 0
- Use Adaptive Pressure Advance: 0
- Acceleration to Deceleration: 50%
```

## Releasing

Releases are cut by pushing a tag. There is no version to bump in the csproj and no changelog to edit:

```bash
git tag v1.2.0
git push origin v1.2.0
```

The [Release workflow](.github/workflows/release.yml) builds and tests on every platform, publishes the four binaries above with the version taken from the tag (so `--version` prints `1.2.0`), and creates the GitHub Release with notes generated from the merged pull requests since the previous tag. PRs are grouped by label per [`.github/release.yml`](.github/release.yml), so label PRs `enhancement`, `bug`, or `documentation` to land them in the right section.

Tags are protected by a repository ruleset: they can only be created, moved, or deleted by a repository admin.

## Questions & Discussions

Usage questions are best asked in [**3D Print Log Discussions**](https://github.com/HoffmanEngineering/3d-print-log-ui/discussions/categories/q-a). That board is the front door for the whole project — the web app, the API, and both slicer plugins — so you do not have to work out which repository your question belongs to, and answers stay searchable for whoever asks next.

If a slicer is reporting a value incorrectly, that is almost always this uploader rather than the API. Attaching the G-code header (or the whole file) to an issue is the single most useful thing you can include.

Bug reports and feature requests belong in this repository's issues.
