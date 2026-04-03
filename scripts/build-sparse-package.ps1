#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the sparse MSIX package for the applanch Windows 11 context menu extension.
.DESCRIPTION
    Stages the Package.appxmanifest and icon assets then calls makeappx.exe to create
    applanch.msix. Run this script whenever Package.appxmanifest or the icon assets change.
    The generated package is treated as a build artifact (not source-controlled).
.PARAMETER SourceManifest
    Path to the source Package.appxmanifest (default: src\applanch\Assets\Package.appxmanifest).
.PARAMETER OutputMsix
    Output path for applanch.msix (default: artifacts\sparse-package\applanch.msix).
.EXAMPLE
    .\scripts\build-sparse-package.ps1
#>
param(
    [string]$SourceManifest = "$PSScriptRoot\..\src\applanch\Assets\Package.appxmanifest",
    [string]$OutputMsix = "$PSScriptRoot\..\artifacts\sparse-package\applanch.msix"
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

$outputDirectory = Split-Path -Path $OutputMsix -Parent
if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    Write-Error "OutputMsix must include a directory path."
    exit 1
}

New-Item -ItemType Directory -Force $outputDirectory | Out-Null

# Copy manifest and icon assets.
Copy-Item $SourceManifest (Join-Path $staging 'AppxManifest.xml') -Force
$assetSourceDir = Split-Path $SourceManifest
Copy-Item (Join-Path $assetSourceDir 'applanch44.png')  $assetsDir -Force
Copy-Item (Join-Path $assetSourceDir 'applanch150.png') $assetsDir -Force

# Build the sparse MSIX.
& $makeappx pack /nv /o /d $staging /p $OutputMsix
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Sign the MSIX with the local dev certificate if one is available.
# Run .\scripts\setup-dev-signing.ps1 (once, as Admin) to create it.
$devCert = Get-ChildItem 'Cert:\CurrentUser\My' |
    Where-Object { $_.Subject -eq 'CN=applanch' -and $_.NotAfter -gt (Get-Date) -and $_.HasPrivateKey } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($devCert) {
    $kitsRoot = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -ErrorAction SilentlyContinue).KitsRoot10
    $signtool = if ($kitsRoot) {
        Get-ChildItem (Join-Path $kitsRoot 'bin') -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like '*x64*' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }

    if ($signtool -and (Test-Path $signtool)) {
        Write-Host "Signing MSIX with dev certificate ($($devCert.Thumbprint))..."
        & $signtool sign /fd SHA256 /sha1 $devCert.Thumbprint /tr http://timestamp.digicert.com /td SHA256 /v $OutputMsix
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "signtool signing failed (exit $LASTEXITCODE). The MSIX remains unsigned."
        }
        else {
            Write-Host "MSIX signed successfully."
        }
    }
    else {
        Write-Warning "signtool.exe not found; MSIX will remain unsigned. Install the Windows SDK."
    }
}
else {
    Write-Host "No dev signing certificate found. MSIX is unsigned."
    Write-Host "Run .\scripts\setup-dev-signing.ps1 (as Admin) to enable signing without Developer Mode."
}

Write-Host "Sparse MSIX built: $OutputMsix ($([System.IO.FileInfo]$OutputMsix).Length bytes)"
