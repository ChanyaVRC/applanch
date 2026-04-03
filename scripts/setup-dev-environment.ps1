#Requires -Version 5.1
<#
.SYNOPSIS
    Bootstraps a local development environment for applanch.
.DESCRIPTION
    Validates required tools, performs an initial Debug build, optionally sets up
    a local dev signing certificate, and optionally builds the sparse MSIX package.

    This script does not install external software automatically. If prerequisites
    are missing, it reports what to install.
.PARAMETER SkipBuild
    Skip the initial dotnet Debug build.
.PARAMETER SetupDevSigning
    Run setup-dev-signing.ps1. If not elevated, this script prompts for UAC.
.PARAMETER BuildSparseMsix
    Build sparse MSIX via build-sparse-package.ps1 after checks/build.
.EXAMPLE
    .\scripts\setup-dev-environment.ps1
.EXAMPLE
    # Full bootstrap including dev signing and sparse MSIX build:
    .\scripts\setup-dev-environment.ps1 -SetupDevSigning -BuildSparseMsix
#>
param(
    [switch]$SkipBuild,
    [switch]$SetupDevSigning,
    [switch]$BuildSparseMsix
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message"
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-SetupDevSigning {
    $signingScriptPath = Join-Path $PSScriptRoot 'setup-dev-signing.ps1'

    if (Test-IsAdministrator) {
        & $signingScriptPath
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        return
    }

    $shellExecutable = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
    if ([string]::IsNullOrWhiteSpace($shellExecutable)) {
        $shellExecutable = (Get-Command powershell -ErrorAction SilentlyContinue).Source
    }

    if ([string]::IsNullOrWhiteSpace($shellExecutable)) {
        Write-Error 'Unable to find PowerShell executable for elevation.'
    }

    Write-Host 'Current shell is not elevated. Requesting UAC approval for setup-dev-signing.ps1...'
    $elevatedProcess = Start-Process -FilePath $shellExecutable `
        -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $signingScriptPath `
        -Verb RunAs `
        -Wait `
        -PassThru

    if ($elevatedProcess.ExitCode -ne 0) {
        Write-Error "setup-dev-signing.ps1 failed in elevated process (exit code: $($elevatedProcess.ExitCode))."
    }
}

function Get-WindowsSdkToolPath([string]$toolFileName) {
    $kitsRoot = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -ErrorAction SilentlyContinue).KitsRoot10
    if ([string]::IsNullOrWhiteSpace($kitsRoot)) {
        return $null
    }

    return Get-ChildItem (Join-Path $kitsRoot 'bin') -Recurse -Filter $toolFileName -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like '*x64*' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

function Assert-Prerequisites {
    $missing = @()

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        $missing += '.NET SDK 10 (dotnet command not found)'
    }
    else {
        $sdks = & dotnet --list-sdks
        if ($LASTEXITCODE -ne 0 -or -not ($sdks | Where-Object { $_ -match '^10\.' })) {
            $missing += '.NET SDK 10'
        }
    }

    $makeappx = Get-WindowsSdkToolPath 'makeappx.exe'
    if (-not $makeappx -or -not (Test-Path $makeappx)) {
        $missing += 'Windows SDK (makeappx.exe)'
    }

    $signtool = Get-WindowsSdkToolPath 'signtool.exe'
    if (-not $signtool -or -not (Test-Path $signtool)) {
        $missing += 'Windows SDK (signtool.exe)'
    }

    if ($missing.Count -gt 0) {
        Write-Error (
            "Missing prerequisites:`n - " + ($missing -join "`n - ") +
            "`nInstall required components, then run this script again."
        )
    }

    Write-Host 'Prerequisites: OK'
    Write-Host "  dotnet   : $($dotnet.Source)"
    Write-Host "  makeappx : $makeappx"
    Write-Host "  signtool : $signtool"
}

$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    Write-Step 'Checking prerequisites'
    Assert-Prerequisites

    if (-not $SkipBuild) {
        Write-Step 'Running initial Debug build'
        & dotnet build .\applanch.slnx -c Debug
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Step 'Skipping initial Debug build'
    }

    if ($SetupDevSigning) {
        Write-Step 'Setting up local dev signing certificate'
        Invoke-SetupDevSigning
    }
    else {
        Write-Step 'Skipping dev signing setup (pass -SetupDevSigning to enable)'
    }

    if ($BuildSparseMsix) {
        Write-Step 'Building sparse MSIX package'
        & "$PSScriptRoot\build-sparse-package.ps1"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Step 'Skipping sparse MSIX build (pass -BuildSparseMsix to enable)'
    }

    Write-Host ''
    Write-Host 'Environment setup complete.'
    Write-Host 'Suggested next steps:'
    Write-Host '  1) dotnet test'
    Write-Host '  2) .\scripts\build-sparse-package.ps1'
}
finally {
    Pop-Location
}
