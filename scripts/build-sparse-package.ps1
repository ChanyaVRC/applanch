#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the sparse MSIX package for the applanch Windows 11 context menu extension.
.DESCRIPTION
    Stages the Package.appxmanifest and icon assets then calls makeappx.exe to create
    applanch.msix. Run this script whenever Package.appxmanifest or the icon assets change.
    The resulting applanch.msix is committed to the repository as a static asset.
.PARAMETER SourceManifest
    Path to the source Package.appxmanifest (default: src\applanch\Assets\Package.appxmanifest).
.PARAMETER OutputMsix
    Output path for applanch.msix (default: src\applanch\Assets\applanch.msix).
.EXAMPLE
    .\scripts\build-sparse-package.ps1
#>
param(
    [string]$SourceManifest = "$PSScriptRoot\..\src\applanch\Assets\Package.appxmanifest",
    [string]$OutputMsix = "$PSScriptRoot\..\src\applanch\Assets\applanch.msix"
)

$ErrorActionPreference = 'Stop'

# Locate makeappx.exe from the Windows SDK.
$makeappx = $null
$root = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -ErrorAction SilentlyContinue).KitsRoot10
if ($root) {
    $makeappx = Get-ChildItem (Join-Path $root 'bin') -Recurse -Filter 'makeappx.exe' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like '*x64*' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $makeappx -or -not (Test-Path $makeappx)) {
    Write-Error "makeappx.exe not found. Install the Windows SDK 10.0.19041.0 or later."
    exit 1
}

# Prepare staging directory.
$staging = Join-Path ([System.IO.Path]::GetTempPath()) 'applanch-sparse'
$assetsDir = Join-Path $staging 'Assets'
New-Item -ItemType Directory -Force $assetsDir | Out-Null

# Copy manifest and icon assets.
Copy-Item $SourceManifest (Join-Path $staging 'AppxManifest.xml') -Force
$assetSourceDir = Split-Path $SourceManifest
Copy-Item (Join-Path $assetSourceDir 'applanch44.png')  $assetsDir -Force
Copy-Item (Join-Path $assetSourceDir 'applanch150.png') $assetsDir -Force

# Build the sparse MSIX.
& $makeappx pack /nv /o /d $staging /p $OutputMsix
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Sparse MSIX built: $OutputMsix ($([System.IO.FileInfo]$OutputMsix).Length bytes)"
