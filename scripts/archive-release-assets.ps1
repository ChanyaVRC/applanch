param(
    [Parameter(Mandatory = $true)]
    [string]$AppVersion,

    [Parameter(Mandatory = $true)]
    [string]$Runtime
)

$ErrorActionPreference = 'Stop'

$publishDir = "artifacts/publish/$Runtime"
$distDir = 'dist'
$archiveName = "applanch-$AppVersion-$Runtime.zip"
$archivePath = Join-Path $distDir $archiveName
$checksumPath = "$archivePath.sha256"

if (-not (Test-Path -Path $publishDir -PathType Container)) {
    Write-Error "Publish directory not found: '$publishDir'"
    exit 1
}

New-Item -ItemType Directory -Force -Path $distDir | Out-Null
Compress-Archive -Path "$publishDir/*" -DestinationPath $archivePath
$hash = (Get-FileHash $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $archiveName" | Set-Content -NoNewline -Path $checksumPath
