# OrcaSlicer fixtures

## orcaslicer-2.5.0-alpha-cube-20mm.gcode

A 20 mm cube sliced by the OrcaSlicer 2.5.0-alpha CLI
(`OrcaSlicer_Linux_AppImage_Ubuntu2404_V2.5.0-alpha.AppImage`) on 2026-10-17, in the Docker image from the
3d-print-log-orca-plugin end-to-end harness (`e2e/`, branch `test/e2e`). Presets: Generic Klipper Printer
(`MyKlipper 0.4 nozzle`), `0.20mm Standard @MyKlipper`, `Generic PLA @System`, which is what that run's
scenario a uses. Orca's preview shows 4.00 g and 1.34 m for this cube.

```bash
orca-slicer --datadir <datadir> --slice 0 --arrange 1 \
  --load-settings "Custom/machine/MyKlipper 0.4 nozzle.json;Custom/process/0.20mm Standard @MyKlipper.json" \
  --load-filaments "OrcaFilamentLibrary/filament/Generic PLA @System.json" \
  --outputdir out cube.stl
```

The 2.5 CLI resolves a system preset only from its source file next to the vendor manifest, so the profile
paths point into `resources/profiles/` of an OrcaSlicer checkout (main at fd28f6c).

What it pins:

- `outer_wall_line_width = 100%` and `line_width = 110%`: Orca 2.5's system profiles give line widths as a
  percentage of the nozzle diameter, which `{{models}}` has to resolve to get 20.0 × 20.0 × 20.0 mm.
- `; filament used [g] = 4.00`: the per-slot grams that become the filamentUsage entry's `EstimatedAmountMg`.

Trimmed like the others: the header, the first two layers, the last two layers (for `{{models}}`), the end
G-code, the configuration block and the print summary are kept; the layers in between are replaced with
`; TOOLPATH OMITTED FROM TEST FIXTURE`. The CLI does not render a thumbnail (that needs the GUI's OpenGL
view), so this fixture has none.
