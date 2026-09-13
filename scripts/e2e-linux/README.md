# Linux end-to-end check for the setup wizard

Runs the real uploader's `install` / `uninstall` against a real OrcaSlicer on Ubuntu 24.04, inside Docker, with
no GPU: the slicer renders through Mesa's software rasteriser on an Xvfb display and is driven with `xdotool`.

```powershell
.\scripts\e2e-linux\run.ps1            # OrcaSlicer 2.4.2 by default
.\scripts\e2e-linux\run.ps1 -AppImageUrl <other OrcaSlicer-family AppImage>
```

What it proves, in order (see `e2e.sh`):

1. OrcaSlicer's first-run wizard completes and writes `~/.config/OrcaSlicer/{system,user}` — the same layout the
   installer expects on Windows.
2. `install` creates one ` - 3DPrintLog` override per selectable process profile, and a second run is a no-op.
3. OrcaSlicer lists the overrides as user presets; selecting one, slicing a cube and exporting G-code makes the
   slicer call the installed script with the chosen flags, the temporary G-code path and `SLIC3R_PP_OUTPUT_NAME`.
   (The binary is swapped for a logging stub at the same path first, so nothing is uploaded.)
4. `uninstall` removes every override again.

Artifacts land in `scripts/e2e-linux/out/`: numbered screenshots of each step, the slicer's log, the exported
G-code, and copies of the config tree after the wizard and after `install`. The "after wizard" tree is what
`TestData/Installer/OrcaSlicer-linux-fresh` was captured from.

Notes

- Click coordinates assume a 1600×1000 screen and the OrcaSlicer 2.4.x wizard layout. When a step fails the
  script saves a `-fail.png` screenshot — compare it with the numbered ones to see which click drifted.
- Native AOT cannot cross-compile from Windows, so `run.ps1` publishes a self-contained (non-AOT) linux-x64
  build. The wizard logic is identical; only the packaging differs.
- The first run downloads the ~140 MB AppImage into `scripts/e2e-linux/cache/` and builds the image (~1 GB of
  apt packages). Both are reused afterwards.
- Files go in and out with `docker cp` rather than bind mounts, so no Docker Desktop file-sharing consent is
  needed (bind mounts were seen to hang the container in `Created` on Windows).
