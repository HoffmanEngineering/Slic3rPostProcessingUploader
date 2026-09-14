# Slicer Profile Installer — Design Doc

**Date:** 2026-03-08
**Status:** Approved

## Problem

Users must manually add the uploader executable path to every process profile in every slicer they use. OrcaSlicer-family slicers have no global post-process script setting — it must be set per profile. A user with 3 slicers and 50+ profiles each faces hundreds of manual edits.

## Goal

Add an install/uninstall wizard to the existing binary that automatically injects (or removes) the uploader's path into all process profiles across all detected OrcaSlicer-family slicers. Designed to be extended to PrusaSlicer, Bambu Studio, and FLSun Slicer in the future.

## Scope (v1)

**In scope:**
- OrcaSlicer, Snapmaker_Orca, AnycubicSlicerNext (share identical config structure)
- Windows, macOS, Linux
- Interactive terminal wizard
- Dry-run mode
- Uninstall

**Out of scope (future):**
- PrusaSlicer, Bambu Studio, FLSun Slicer (different profile formats — separate design needed)
- VM/real-slicer integration testing (separate design)

---

## CLI Design

Mode is determined by the first argument:

| Invocation | Mode |
|---|---|
| `(no args)` | Wizard (interactive install, double-click experience) |
| `install` | Install wizard (same as no-args) |
| `install --dry-run` | Preview install — no files written |
| `uninstall` | Uninstall wizard |
| `uninstall --dry-run` | Preview uninstall — no files written |
| `<gcode-file> [flags]` | Existing post-process behavior (unchanged) |

`WizardMode` and `InstallMode` are functionally identical. `--dry-run` is only valid alongside `install` or `uninstall`.

---

## Architecture

### ISlicerProfileInstaller

```csharp
interface ISlicerProfileInstaller
{
    string SlicerName { get; }
    bool IsDetected();
    SlicerInstallStatus GetInstallStatus(string executablePath);
    InstallResult Install(string executablePath, string flags, bool dryRun);
    InstallResult Uninstall(string executablePath, bool dryRun);
}
```

### OrcaFamily Base + Subclasses

```csharp
abstract class OrcaFamilyProfileInstaller : ISlicerProfileInstaller
{
    protected abstract string SlicerDirectoryName { get; }
    // All shared JSON discovery + injection logic
}

class OrcaSlicerInstaller : OrcaFamilyProfileInstaller
    => SlicerDirectoryName = "OrcaSlicer"

class SnapmakerOrcaInstaller : OrcaFamilyProfileInstaller
    => SlicerDirectoryName = "Snapmaker_Orca"

class AnycubicSlicerNextInstaller : OrcaFamilyProfileInstaller
    => SlicerDirectoryName = "AnycubicSlicerNext"
```

Adding a new OrcaSlicer fork = one new subclass with one string property.
Adding PrusaSlicer = a new base class implementing `ISlicerProfileInstaller` with `.ini` logic.

### SlicerInstallerRegistry

Holds all registered `ISlicerProfileInstaller` instances. The only file to update when adding new slicer support. Mirrors the role of `ParserFactory` in the existing codebase.

---

## Wizard UX Flow

```
3D Print Log Uploader - Setup Wizard
=====================================
Scanning for supported slicers...

Found: OrcaSlicer         C:\Users\...\AppData\Roaming\OrcaSlicer\
  Status: Not installed | 47 process profiles found

Found: Snapmaker_Orca     C:\Users\...\AppData\Roaming\Snapmaker_Orca\
  Status: Already installed | 83 profiles | flags: --full

Found: AnycubicSlicerNext  (not detected - skipped)

--- OrcaSlicer ---
Install to OrcaSlicer? [Y/n]: y

  Note template:
    1) Default (recommended)
    2) Full
  Choice [1]: 2

  Opt out of telemetry? [y/N]: n
  Additional flags (leave blank for none): --local-dev

  Note: 3 profiles already have other post-process scripts. Ours will be appended.
  Will create/update 47 process profiles. Proceed? [Y/n]: y
  Done: 32 created, 15 updated.

--- Snapmaker_Orca ---
Already installed (flags: --full). Reinstall with new flags? [Y/n]: n

Setup complete!
```

**Key behaviors:**
- Slicers not detected on disk are skipped entirely (no prompt)
- Existing install status is shown before asking about reinstall
- Guided flag selection (note template, telemetry opt-out) followed by optional free-text for advanced flags (`--local-dev`, `--debug <path>`, etc.)
- If any profiles have other post-process scripts, warn once at the slicer level before confirming
- Dry-run replaces "Done: 32 created, 15 updated" with `[DRY RUN] Would create/update 47 profiles`
- Uninstall summary reports: "Removed from 45 profiles. 2 profiles had other scripts — left those intact."

---

## Profile Discovery & Injection (OrcaFamily)

### Config Root Resolution (OS-aware)

```csharp
string GetConfigRoot(string slicerName) => OS switch {
    Windows => Path.Combine(Environment.SpecialFolder.ApplicationData, slicerName),
    macOS   => Path.Combine(HOME, "Library", "Application Support", slicerName),
    Linux   => Path.Combine(XDG_CONFIG_HOME ?? "~/.config", slicerName)
}
```

### Discovery

1. Scan `{configRoot}/system/**/process/*.json`
2. Keep only files where `"instantiation": "true"` (skip abstract base profiles)
3. Enumerate all subdirectories under `{configRoot}/user/` (handles `default/` and numeric cloud account IDs like `56914/`)

### Injection Per Profile

For each selectable system profile × each user account dir:

| Condition | Action |
|---|---|
| User override exists, already contains our script | Skip |
| User override exists, missing our script | Load JSON, append to `post_process` array, save |
| User override does not exist | Create minimal override JSON |

**Our script detection:** Check if any entry in `post_process` contains `Slic3rPostProcessingUploader` (partial, path-agnostic match — survives path changes between installs).

**Executable path quoting:** Wrap in quotes if the path contains spaces.
`"C:\My Path\uploader.exe" --full`

**Minimal override JSON structure:**
```json
{
    "from": "User",
    "inherits": "<system profile name>",
    "name": "<system profile name>",
    "post_process": ["\"<exe path>\" <flags>"],
    "print_settings_id": "<system profile name>",
    "version": "<version from system profile>"
}
```

### Uninstall

- Find all user profile JSON files containing `Slic3rPostProcessingUploader` in `post_process`
- Remove only our entry, leave all other scripts intact
- If the file becomes an empty override (no user customizations beyond required structural fields), delete it
- If the file has other user settings (e.g. `wall_loops`, `sparse_infill_density`), keep the file — only remove our `post_process` entry

---

## Testing Strategy

### Unit Tests — JSON Logic
- Inject into profile with no existing `post_process` field
- Inject into profile that already has other scripts (append, don't replace)
- Skip profile that already has our script (idempotent)
- Uninstall removes only our entry, leaves other scripts
- Uninstall deletes file when it becomes an empty override
- Uninstall keeps file when other user customizations exist

### Unit Tests — Discovery
- Filters out `instantiation: false` abstract base profiles
- Enumerates all `user/` subdirs including numeric account IDs
- OS-aware config root resolution

### Integration Tests — Temp Directories
- Full install against realistic fixture directory (mirrors real profile structure from research doc)
- Full uninstall round-trip
- Dry-run produces zero file writes

Follows existing MSTest conventions. JSON structure validated directly (no snapshot testing needed).

---

## Future Extensibility

| Goal | What to do |
|---|---|
| Add new OrcaSlicer fork | Subclass `OrcaFamilyProfileInstaller`, set `SlicerDirectoryName`, register in `SlicerInstallerRegistry` |
| Add PrusaSlicer | New base class implementing `ISlicerProfileInstaller` with `.ini` injection logic |
| Add Bambu Studio | New base class implementing `ISlicerProfileInstaller` with Bambu-specific logic |
| VM/real-slicer integration tests | Separate design doc |
