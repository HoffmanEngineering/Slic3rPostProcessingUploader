# End-to-end check of the setup wizard against a real OrcaSlicer on Windows, run inside Windows Sandbox.
# Same four steps as ../e2e-linux/e2e.sh:
#
#   1. Unpack the portable OrcaSlicer and complete its first-run wizard (Klipper + Afinia printers).
#   2. Run `Slic3rPostProcessingUploader.exe install` (the real NativeAOT binary, installed under
#      "C:\Program Files\3D Print Log" so the quoted-path case is exercised) against the config it wrote.
#   3. Swap the binary for a stub that logs its arguments, relaunch OrcaSlicer, load a cube, slice,
#      export G-code, and assert that OrcaSlicer invoked the stub with the installed flags.
#   4. `uninstall` removes every override again.
#
# Inputs (mapped read-only):  C:\e2e\cache\OrcaSlicer_Windows_*_portable.zip, C:\e2e\cache\publish\*.exe
# Outputs (mapped writable):  C:\out  screenshots, logs, the exported G-code, config trees, result.txt
#
# Click coordinates are relative to the OrcaSlicer window's client area (which includes its self-drawn title bar) and assume the 2.4.x layout at
# 100% DPI; when a step fails the script saves a "-fail.png" screenshot next to the numbered ones.
$ErrorActionPreference = "Stop"
$OUT = "C:\out"
Start-Transcript -Path (Join-Path $OUT "e2e.log") | Out-Null
. "$PSScriptRoot\ui.ps1"

$CONFIG = Join-Path $env:APPDATA "OrcaSlicer"
$INSTALL_DIR = "C:\Program Files\3D Print Log"
$UPLOADER = Join-Path $INSTALL_DIR "Slic3rPostProcessingUploader.exe"

function Finish([string]$result) {
    Stop-Orca
    $result | Set-Content (Join-Path $OUT "result.txt")
    Stop-Transcript | Out-Null
    # Shutting the guest down is the only clean way to end a sandbox: killing the host client can leave the
    # VM orphaned. Skip it when the host asked to keep the sandbox open for inspection.
    if (-not (Test-Path "C:\e2e\cache\keep-open")) { shutdown /s /t 5 }
    exit ($(if ($result -eq "PASS") { 0 } else { 1 }))
}

function Fail([string]$msg) {
    Log "FAIL: $msg"
    try { Save-Screenshot "fail" } catch {}
    Finish "FAIL: $msg"
}

function Start-Orca([string]$LogName, [string]$ModelFile) {
    $orcaArgs = @(); if ($ModelFile) { $orcaArgs += "`"$ModelFile`"" }
    if ($orcaArgs.Count -gt 0) { Start-Process -FilePath "C:\orca\orca-slicer.exe" -ArgumentList $orcaArgs }
    else { Start-Process -FilePath "C:\orca\orca-slicer.exe" }
    [void](Wait-Window ' - OrcaSlicer$' 120)   # "Untitled - OrcaSlicer" or "<model> - OrcaSlicer"
    Start-Sleep -Seconds 6
    Set-MainWindowPlacement 0 0 1200 800
}

function Stop-Orca {
    Get-Process -Name "orca-slicer" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 3
}

function Invoke-Uploader([string]$Verb, [string[]]$Answers, [string]$LogName) {
    $log = Join-Path $OUT "$LogName.log"
    $probe = ($Answers | findstr /n "^") -join " | "
    Log "stdin for '$Verb' as PowerShell pipes it ($($OutputEncoding.EncodingName)): $probe"
    # Feed the answers from a file with known bytes rather than through PowerShell's native-command piping.
    $answersFile = Join-Path $env:TEMP "answers.txt"
    [System.IO.File]::WriteAllText($answersFile, (($Answers -join "`r`n") + "`r`n"), [System.Text.Encoding]::ASCII)
    cmd /c "`"$UPLOADER`" $Verb < `"$answersFile`" 2>&1" | Tee-Object -FilePath $log
}

function Write-CubeStl([string]$Path) {
    $s = 20
    $tri = { param($a, $b, $c) " facet normal 0 0 0`n  outer loop`n   vertex $a`n   vertex $b`n   vertex $c`n  endloop`n endfacet" }
    @(
        "solid cube"
        (& $tri "0 0 0" "$s 0 0" "$s $s 0");   (& $tri "0 0 0" "$s $s 0" "0 $s 0")
        (& $tri "0 0 $s" "$s $s $s" "$s 0 $s"); (& $tri "0 0 $s" "0 $s $s" "$s $s $s")
        (& $tri "0 0 0" "0 $s 0" "0 $s $s");   (& $tri "0 0 0" "0 $s $s" "0 0 $s")
        (& $tri "$s 0 0" "$s $s $s" "$s $s 0"); (& $tri "$s 0 0" "$s 0 $s" "$s $s $s")
        (& $tri "0 0 0" "0 0 $s" "$s 0 $s");   (& $tri "0 0 0" "$s 0 $s" "$s 0 0")
        (& $tri "0 $s 0" "$s $s $s" "0 $s $s"); (& $tri "0 $s 0" "$s $s 0" "$s $s $s")
        "endsolid cube"
    ) -join "`n" | Set-Content -Path $Path -Encoding Ascii
}

try {
# ---- 0. Inputs -------------------------------------------------------------------------------------
$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
Log "sandbox desktop $($bounds.Width)x$($bounds.Height), user $env:USERNAME, APPDATA $env:APPDATA"
if ($bounds.Width -lt 1300 -or $bounds.Height -lt 1000) { Fail "desktop is too small for the click coordinates; enlarge the Sandbox window" }

$zip = Get-ChildItem "C:\e2e\cache\OrcaSlicer_Windows_*_portable.zip" | Select-Object -First 1
if (-not $zip) { Fail "no portable OrcaSlicer zip in C:\e2e\cache" }
$exe = "C:\e2e\cache\publish\Slic3rPostProcessingUploader.exe"
if (-not (Test-Path $exe)) { Fail "no published uploader at $exe" }

New-Item -ItemType Directory -Force $INSTALL_DIR | Out-Null
Copy-Item $exe $UPLOADER
Log "uploader installed at $UPLOADER"

Expand-Archive -Path $zip.FullName -DestinationPath "C:\orca-zip" -Force
$orcaExe = Get-ChildItem "C:\orca-zip" -Recurse -Filter "orca-slicer.exe" | Select-Object -First 1
if (-not $orcaExe) { Fail "orca-slicer.exe not found in the portable zip" }
Move-Item $orcaExe.DirectoryName "C:\orca"
Log "unpacked $($zip.Name)"

# ---- 1. First-run wizard ----------------------------------------------------------------------------
Start-Orca "first-run"
$W = 'Setup Wizard'
[void](Wait-Window $W 30)
Save-Screenshot "welcome"
Click-Window $W 417 422;    Save-Screenshot "region"            # Get Started
Click-Window $W 414 414 1;  Click-Window $W 742 596             # Region: North America -> Next
Save-Screenshot "printer-selection"
Click-Window $W 114 170 1;  Save-Screenshot "klipper"           # Custom Printer -> Klipper
Click-Window $W 769 496 1;  Save-Screenshot "afinia"            # Afinia (whole vendor)
Click-Window $W 742 596 8;  Save-Screenshot "filaments"         # Next
Click-Window $W 742 596 6;  Save-Screenshot "stealth"           # Next
Click-Window $W 129 309 1;  Save-Screenshot "stealth-enabled"   # Enable stealth mode
Click-Window $W 742 583 3;  Save-Screenshot "plugins"           # Next
Click-Window $W 742 583 15                           # Finish
if (-not ((Test-Path "$CONFIG\system\Custom") -and (Test-Path "$CONFIG\system\Afinia"))) { Fail "wizard did not write system profiles" }
if (-not (Test-Path "$CONFIG\user\default\process")) { Fail "wizard did not create user\default\process" }
Save-Screenshot "wizard-done"
Stop-Orca
Log "wizard complete: $((Get-ChildItem "$CONFIG\system" -Directory).Name -join ' ')"
Copy-Item $CONFIG (Join-Path $OUT "config-after-wizard") -Recurse

# ---- 2. Install with the real uploader ----------------------------------------------------------------
# Answers: install=Y, note template 2 (full), opt out of telemetry=y, no extra flags.
Invoke-Uploader install @("y", "2", "y", "") "install"
Copy-Item $CONFIG (Join-Path $OUT "config-after-install") -Recurse
$processDir = "$CONFIG\user\default\process"
$created = @(Get-ChildItem $processDir -Filter "* - 3DPrintLog.json").Count
if ($created -eq 0) { Fail "install created no overrides" }
# As it appears in the JSON: the path is quoted (it has spaces) and backslashes are escaped.
$expectedEntry = '\"' + $UPLOADER.Replace('\', '\\') + '\" --full --opt-out-telemetry'
$sample = Get-Content "$processDir\0.20mm Standard @MyKlipper - 3DPrintLog.json" -Raw
if (-not $sample.Contains($expectedEntry)) { Fail "override does not carry the expected post_process entry ($expectedEntry)" }
Log "install created $created overrides"
$again = Invoke-Uploader install @("y", "2", "y", "") "install-again"
if (-not ($again -match "$created skipped")) { Fail "second install was not a no-op" }

# ---- 3. Prove OrcaSlicer runs it --------------------------------------------------------------------
# Same path, different program: a stub .exe that records how the slicer called it (nothing is uploaded).
Move-Item $UPLOADER "$UPLOADER.real"
Add-Type -OutputAssembly $UPLOADER -OutputType ConsoleApplication -TypeDefinition @"
using System; using System.Collections.Generic; using System.IO; using System.Linq;
static class Stub {
    static void Main(string[] a) {
        var lines = new List<string> { "args: " + string.Join(" ", a),
            "SLIC3R_PP_OUTPUT_NAME=" + Environment.GetEnvironmentVariable("SLIC3R_PP_OUTPUT_NAME") };
        if (a.Length > 0 && File.Exists(a[a.Length - 1])) lines.AddRange(File.ReadLines(a[a.Length - 1]).Take(2));
        File.AppendAllLines(@"$OUT\pp.log", lines);
    }
}
"@

Write-CubeStl "C:\cube.stl"
Start-Orca "with-overrides" "C:\cube.stl"
Click-Window ' - OrcaSlicer$' 107 52 1 4     # Prepare tab
Click-Window ' - OrcaSlicer$' 165 334 1 3    # process preset dropdown
Save-Screenshot "preset-dropdown"
Click-Screen 173 510 4                       # "0.20mm Standard @MyKlipper - 3DPrintLog" (popup is a separate window)
Save-Screenshot "cube-ready"

# Documentation shot: the preset's Others tab with the post-processing script filled in (Advanced mode only).
Set-MainWindowPlacement 0 0 1200 980
Click-Window ' - OrcaSlicer$' 283 295 1 3    # Simple/Advanced toggle -> Advanced
Click-Window ' - OrcaSlicer$' 360 369 1 3    # Others tab
[void][Win32]::SetCursorPos(200, 650); Scroll-Wheel -40; Start-Sleep 1; Scroll-Wheel 3; Start-Sleep 2
Save-Screenshot "others-post-processing"
Click-Window ' - OrcaSlicer$' 41 369 1 2     # back to Quality tab
Set-MainWindowPlacement 0 0 1200 800
Click-Window ' - OrcaSlicer$' 888 52 1 25    # Slice plate
Save-Screenshot "sliced"
Click-Window ' - OrcaSlicer$' 1069 52 1 6    # Export G-code file
[void](Wait-Window 'Save G-code file as' 30)
Send-Keys "^a"; Send-Keys "C:\out\cube-test.gcode"; Send-Keys "{ENTER}"
Wait-WindowGone 'Save G-code file as' 60
Start-Sleep -Seconds 10
Save-Screenshot "exported"
Stop-Orca

if (-not (Test-Path "$OUT\cube-test.gcode")) { Fail "no G-code exported" }
if (-not (Test-Path "$OUT\pp.log")) { Fail "OrcaSlicer never invoked the post-process script" }
$pp = Get-Content "$OUT\pp.log"
if (-not ($pp[0] -match '^args: --full --opt-out-telemetry ')) { Fail "script was called with unexpected arguments: $($pp[0])" }
if (-not ($pp -contains "SLIC3R_PP_OUTPUT_NAME=C:\out\cube-test.gcode")) { Fail "SLIC3R_PP_OUTPUT_NAME not passed" }
if (-not ($pp -match 'generated by OrcaSlicer')) { Fail "script did not receive the G-code path" }

# ---- 4. Uninstall round-trips ----------------------------------------------------------------------
Remove-Item $UPLOADER; Move-Item "$UPLOADER.real" $UPLOADER
Invoke-Uploader uninstall @("y") "uninstall"
$left = @(Get-ChildItem $processDir -Filter "* - 3DPrintLog.json").Count
if ($left -ne 0) { Fail "uninstall left $left overrides behind" }

Log "PASS: OrcaSlicer ran the installed uploader:"
$pp | ForEach-Object { Log $_ }
Finish "PASS"
} catch {
    # Anything unexpected (a P/Invoke conversion, a missing file) must still leave result.txt and shut down.
    Fail "unexpected error: $($_.Exception.Message) $($_.InvocationInfo.PositionMessage)"
}
