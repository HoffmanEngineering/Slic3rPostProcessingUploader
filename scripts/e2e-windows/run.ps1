<#
.SYNOPSIS
    Runs the Windows end-to-end installer check (see e2e.ps1) in Windows Sandbox.

.DESCRIPTION
    Publishes the real win-x64 NativeAOT build of the uploader, downloads the portable OrcaSlicer build, and
    boots a throw-away Windows Sandbox that runs the wizard against it. Screenshots, logs, the exported
    G-code and the captured config trees land in -OutDir. Needs the "Windows Sandbox" optional feature
    (Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM). The sandbox gets the
    host GPU through vGPU, so OrcaSlicer renders normally.

.EXAMPLE
    .\scripts\e2e-windows\run.ps1
    .\scripts\e2e-windows\run.ps1 -KeepOpen     # leave the sandbox running afterwards for a look around
#>
param(
    [string]$PortableZipUrl = "https://github.com/OrcaSlicer/OrcaSlicer/releases/download/v2.4.2/OrcaSlicer_Windows_V2.4.2_x64_portable.zip",
    [string]$OutDir = (Join-Path $PSScriptRoot "out"),
    [string]$CacheDir = (Join-Path $PSScriptRoot "cache"),
    [int]$TimeoutMinutes = 25,
    [switch]$KeepOpen
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$publishDir = Join-Path $CacheDir "publish"
New-Item -ItemType Directory -Force $OutDir, $CacheDir | Out-Null

$zip = Join-Path $CacheDir (Split-Path $PortableZipUrl -Leaf)
if (-not (Test-Path $zip)) {
    Write-Host "Downloading $PortableZipUrl"
    Invoke-WebRequest -Uri $PortableZipUrl -OutFile $zip
    Unblock-File $zip
}

Write-Host "Publishing win-x64 NativeAOT build"
dotnet publish (Join-Path $repo "Slic3rPostProcessingUploader") -c Release -r win-x64 -o $publishDir | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Get-ChildItem $OutDir | Remove-Item -Recurse -Force

# The sandbox sees this folder (with cache\) read-only as C:\e2e and the output folder writable as C:\out.
$wsb = Join-Path $CacheDir "sandbox.wsb"
@"
<Configuration>
  <vGPU>Enable</vGPU>
  <Networking>Disable</Networking>
  <MemoryInMB>8192</MemoryInMB>
  <MappedFolders>
    <MappedFolder><HostFolder>$PSScriptRoot</HostFolder><SandboxFolder>C:\e2e</SandboxFolder><ReadOnly>true</ReadOnly></MappedFolder>
    <MappedFolder><HostFolder>$OutDir</HostFolder><SandboxFolder>C:\out</SandboxFolder><ReadOnly>false</ReadOnly></MappedFolder>
  </MappedFolders>
  <LogonCommand><Command>powershell -ExecutionPolicy Bypass -File C:\e2e\e2e.ps1</Command></LogonCommand>
</Configuration>
"@ | Set-Content $wsb -Encoding Ascii

Add-Type @"
using System; using System.Runtime.InteropServices;
public static class HostWin32 {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int h2, bool repaint);
}
"@

# e2e.ps1 shuts the guest down when it is done unless this marker exists (see Finish there).
$keepMarker = Join-Path $CacheDir "keep-open"
if ($KeepOpen) { New-Item -ItemType File -Force $keepMarker | Out-Null } else { Remove-Item $keepMarker -ErrorAction SilentlyContinue }

if (Get-Process -Name "vmmemWindowsSandbox" -ErrorAction SilentlyContinue) {
    throw "a Windows Sandbox is already running (only one is allowed); close it first"
}
Write-Host "Starting Windows Sandbox (this takes a few minutes)"
Start-Process WindowsSandbox.exe -ArgumentList "`"$wsb`""

# The guest desktop is the size of the Sandbox window: make it big enough for the click coordinates.
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
$sized = $false
$result = Join-Path $OutDir "result.txt"
while ((Get-Date) -lt $deadline) {
    if (-not $sized) {
        $h = [HostWin32]::FindWindow($null, "Windows Sandbox")
        if ($h -ne [IntPtr]::Zero) { [void][HostWin32]::MoveWindow($h, 0, 0, 1700, 1150, $true); $sized = $true }
    }
    if (Test-Path $result) { break }
    Start-Sleep -Seconds 5
}

if (-not $KeepOpen) {
    # The guest shuts itself down; wait for the VM to disappear before reporting.
    $end = (Get-Date).AddMinutes(2)
    while ((Get-Date) -lt $end -and (Get-Process -Name "vmmemWindowsSandbox" -ErrorAction SilentlyContinue)) { Start-Sleep -Seconds 3 }
}
if (-not (Test-Path $result)) { throw "end-to-end check timed out after $TimeoutMinutes minutes; see $OutDir" }
$text = (Get-Content $result -Raw).Trim()
if ($text -ne "PASS") { throw "end-to-end check FAILED: $text; see $OutDir for screenshots and logs" }
Write-Host "PASS; artifacts in $OutDir"
