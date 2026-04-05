param(
    [Parameter(Mandatory = $true)]
    [string]$AppVersion,

    [Parameter(Mandatory = $true)]
    [string]$Runtime,

    [string]$PublishDir,

    [string]$OutputDir = 'dist'
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = "publish/$Runtime"
}

if (-not (Test-Path -Path $PublishDir -PathType Container)) {
    Write-Error "Publish directory not found: '$PublishDir'"
    exit 1
}

$resolvedPublishDir = (Resolve-Path $PublishDir).Path
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$resolvedOutputDir = (Resolve-Path $OutputDir).Path

$installerFileName = "applanch-$AppVersion-$Runtime-installer"
$architecturesAllowed = if ($Runtime -eq 'win-x64') { 'x64compatible' } else { 'x86compatible' }
$architecturesInstallIn64BitMode = if ($Runtime -eq 'win-x64') { 'x64compatible' } else { '' }

$iscc = Get-Command 'iscc.exe' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
if ([string]::IsNullOrWhiteSpace($iscc)) {
    $knownPaths = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )
    $iscc = $knownPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($iscc)) {
    Write-Error 'Inno Setup compiler (ISCC.exe) was not found. Install Inno Setup 6 first.'
    exit 1
}

$scriptTemplate = @'
[Setup]
AppId={{D102DAA4-8B3D-44BA-987E-7C03B9D23F8D}
AppName=applanch
AppVersion=__APP_VERSION__
AppPublisher=ChanyaVRC
DefaultDirName={localappdata}\Programs\applanch
DefaultGroupName=applanch
DisableProgramGroupPage=yes
OutputDir=__OUTPUT_DIR__
OutputBaseFilename=__OUTPUT_BASE_FILENAME__
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=__ARCH_ALLOWED__
ArchitecturesInstallIn64BitMode=__ARCH_INSTALL64__
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
UninstallDisplayIcon={app}\applanch.exe

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "__PUBLISH_DIR__\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\applanch"; Filename: "{app}\applanch.exe"
Name: "{autodesktop}\applanch"; Filename: "{app}\applanch.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\applanch.exe"; Description: "Launch applanch"; Flags: nowait postinstall skipifsilent
'@

$installerScript = $scriptTemplate
$installerScript = $installerScript.Replace('__APP_VERSION__', $AppVersion)
$installerScript = $installerScript.Replace('__OUTPUT_DIR__', $resolvedOutputDir)
$installerScript = $installerScript.Replace('__OUTPUT_BASE_FILENAME__', $installerFileName)
$installerScript = $installerScript.Replace('__ARCH_ALLOWED__', $architecturesAllowed)
$installerScript = $installerScript.Replace('__ARCH_INSTALL64__', $architecturesInstallIn64BitMode)
$installerScript = $installerScript.Replace('__PUBLISH_DIR__', $resolvedPublishDir)

$issPath = Join-Path ([System.IO.Path]::GetTempPath()) ("applanch-installer-" + [Guid]::NewGuid().ToString('N') + '.iss')
try {
    Set-Content -Path $issPath -Value $installerScript -Encoding UTF8

    & $iscc '/Qp' $issPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup build failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }

    $installerPath = Join-Path $resolvedOutputDir ($installerFileName + '.exe')
    if (-not (Test-Path -Path $installerPath -PathType Leaf)) {
        Write-Error "Installer output was not found: '$installerPath'"
        exit 1
    }

    $checksumPath = "$installerPath.sha256"
    $hash = (Get-FileHash $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([System.IO.Path]::GetFileName($installerPath))" | Set-Content -NoNewline -Path $checksumPath

    Write-Host "Installer built: $installerPath"
}
finally {
    if (Test-Path $issPath) {
        Remove-Item $issPath -Force -ErrorAction SilentlyContinue
    }
}