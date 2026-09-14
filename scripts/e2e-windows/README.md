# Windows end-to-end check for the setup wizard

Runs the real, published NativeAOT `Slic3rPostProcessingUploader.exe` (`install` / `uninstall`) against a real
OrcaSlicer on a throw-away Windows, inside [Windows Sandbox](https://learn.microsoft.com/windows/security/application-security/application-isolation/windows-sandbox/).
Every launch is a pristine Windows; the sandbox gets the host GPU through vGPU, so the slicer renders normally.

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM   # once, elevated, then reboot
.\scripts\e2e-windows\run.ps1                                                       # OrcaSlicer 2.4.2 portable by default
.\scripts\e2e-windows\run.ps1 -KeepOpen                                             # leave the sandbox up afterwards
```

It proves the same four things as the Linux check (`../e2e-linux`), see `e2e.ps1`:

1. OrcaSlicer's first-run wizard writes `%APPDATA%\OrcaSlicer\{system,user}` — the layout the installer expects.
2. `install` creates one ` - 3DPrintLog` override per selectable process profile, with the exe path quoted
   (it is installed under `C:\Program Files\3D Print Log`), and a second run is a no-op.
3. OrcaSlicer lists the overrides as user presets; selecting one, slicing a cube and exporting G-code makes the
   slicer call the installed script with the chosen flags, the temporary G-code path and `SLIC3R_PP_OUTPUT_NAME`.
   (The binary is swapped for a logging stub compiled in the sandbox first, so nothing is uploaded.)
4. `uninstall` removes every override again.

What is Windows-specific, compared with the Linux run: the real Windows config root, a path with spaces in the
`post_process` entry, and the shipped NativeAOT binary rather than a self-contained build.

Artifacts land in `scripts/e2e-windows/out/`: numbered screenshots of each step, `e2e.log` (a PowerShell
transcript), the wizard's stdin/stdout, the exported G-code, `pp.log` from the stub, `result.txt` (`PASS` or the
failure) and copies of the config tree after the wizard and after `install`.

Notes

- `run.ps1` maps this folder read-only into the sandbox as `C:\e2e` (the OrcaSlicer zip and the published exe
  live in `cache\`) and `out\` writable as `C:\out`; the sandbox runs `e2e.ps1` as its logon command. There is
  no way to reach into a running sandbox from outside, so all steering happens in that script and the host only
  polls for `result.txt`.
- The guest shuts itself down when done (killing the host client can leave the VM orphaned; if that happens,
  `hcsdiag list` / `hcsdiag kill <id>` from an elevated prompt clears it). Only one sandbox can run at a time.
- Click coordinates are relative to the OrcaSlicer window's client area and assume the 2.4.x layout at 100%
  DPI; `run.ps1` sizes the sandbox window so the guest desktop is large enough. When a step fails the script
  saves a `-fail.png` screenshot — compare it with the numbered ones to see which click drifted.
- Windows PowerShell 5.1 inside the sandbox prepends a UTF-8 BOM when piping strings to a native process, which
  made the wizard read `﻿y` instead of `y`; `e2e.ps1` therefore feeds answers from a file via `cmd /c … <`.
- The first run downloads the ~150 MB portable zip into `scripts/e2e-windows/cache/`; it is reused afterwards.
