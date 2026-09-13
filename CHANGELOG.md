# Unreleased
- `--help` no longer blocks or throws when stdin/stdout is redirected, and `--help`/`--version` no longer trigger telemetry initialization
- Unknown CLI flags (e.g. a misspelled `--fulll`) now cause an error instead of being silently ignored; argument errors (bad `--template`/`--debug` usage) are now reported as plain, actionable messages instead of a generic crash report
- Fixed estimated print times over 24 hours being logged too short: the `d` (days) token in slicer time strings like `1d 6h 12m 5s` was being silently dropped
- Cleaner console output: a short ✓ progress summary on success, plain-English errors with a suggested fix on failure. Raw HTTP responses and stack traces now only appear on screen with `--debug` (they are still written to `slic3r-debug.txt`)
- On error the console window now stays open for 30 seconds (or until a key is pressed) and the process exits with code 1 so slicers can report the failure
- If the browser can't be opened, the 3D Print Log URL is printed so it can be opened manually
- PrusaSlicer 3.0 support: verified against 3.0.0-alpha11 G-code, which turns `support_material` into an enum and drops `support_material_auto`
- PrusaSlicer note templates no longer render blank lines for settings that PrusaSlicer stopped writing to G-code (`support_material_auto`, `wipe_tower_x`, `wipe_tower_y`, `wipe_tower_rotation_angle`); the Full template's "Top Contact Z Distance" now reads `support_material_contact_distance`
- Added PrusaSlicer 2.9.2 and 3.0.0-alpha11 parser test fixtures
- Multi-filament usage entries now include a note with the slicer slot, basic color name (from `filament_colour`), and material, e.g. `Slot 1 · Red (#E72F1D) · PLA`, making it easier to match entries to the right spool on 3D Print Log

# v1.1.0
- New Slicer Support:
  - Bambu Studio
  - FLSun Slicer
  - Anycubic Slicer Next
- Add more robust Multi Material Support

# v1.0.1
- Fixed issue with Total Estimated Times not parsing correctly
- Gracefully handle missing titles

# v1.0.0
- Initial release