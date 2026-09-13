<#
.SYNOPSIS
    Runs the Linux end-to-end installer check (see e2e.sh) in Docker.

.DESCRIPTION
    Publishes a linux-x64 build of the uploader, builds the test-bench image, and runs the wizard against a
    real OrcaSlicer AppImage inside the container. Screenshots, logs, the exported G-code and the captured
    config trees land in -OutDir. Needs Docker Desktop with the Linux engine; no GPU is required.

.EXAMPLE
    .\scripts\e2e-linux\run.ps1
    .\scripts\e2e-linux\run.ps1 -AppImageUrl https://github.com/OrcaSlicer/OrcaSlicer/releases/download/v2.4.2/OrcaSlicer_Linux_AppImage_Ubuntu2404_V2.4.2.AppImage
#>
param(
    [string]$AppImageUrl = "https://github.com/OrcaSlicer/OrcaSlicer/releases/download/v2.4.2/OrcaSlicer_Linux_AppImage_Ubuntu2404_V2.4.2.AppImage",
    [string]$OutDir = (Join-Path $PSScriptRoot "out"),
    [string]$CacheDir = (Join-Path $PSScriptRoot "cache")
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$publishDir = Join-Path $CacheDir "publish"
New-Item -ItemType Directory -Force $OutDir, $CacheDir | Out-Null

$appImage = Join-Path $CacheDir (Split-Path $AppImageUrl -Leaf)
if (-not (Test-Path $appImage)) {
    Write-Host "Downloading $AppImageUrl"
    Invoke-WebRequest -Uri $AppImageUrl -OutFile $appImage
}

Write-Host "Publishing linux-x64 build"
# Native AOT cannot cross-compile from Windows to Linux; a self-contained single file is enough to exercise the wizard.
dotnet publish (Join-Path $repo "Slic3rPostProcessingUploader") -c Release -r linux-x64 --self-contained `
    -p:PublishAot=false -p:PublishSingleFile=true -o $publishDir | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "Building test-bench image"
docker build -q -t slic3r-uploader-e2e $PSScriptRoot | Out-Null
if ($LASTEXITCODE -ne 0) { throw "docker build failed" }

Get-ChildItem $OutDir | Remove-Item -Recurse -Force
Write-Host "Running end-to-end check (this takes a few minutes)"
# Inputs and outputs go through `docker cp` rather than bind mounts: Docker Desktop's host-folder sharing has
# a habit of hanging a container in "Created" until a consent dialog is found, and this never needs it.
$name = "slic3r-uploader-e2e-run"
docker rm -f $name 2>$null | Out-Null
docker create --name $name slic3r-uploader-e2e | Out-Null
docker cp $appImage "${name}:/appimage"
docker cp (Join-Path $publishDir 'Slic3rPostProcessingUploader') "${name}:/uploader"
docker start -a $name
$exit = $LASTEXITCODE
docker cp "${name}:/out/." $OutDir
docker rm -f $name | Out-Null
if ($exit -ne 0) { throw "end-to-end check FAILED; see $OutDir for screenshots and logs" }
Write-Host "PASS; artifacts in $OutDir"
