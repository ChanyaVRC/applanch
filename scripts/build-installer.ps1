param(
    [Parameter(Mandatory = $true)]
    [string]$AppVersion,

    [Parameter(Mandatory = $true)]
    [string]$Runtime,

    [string]$PublishDir,

    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = "publish/$Runtime"
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Join-Path 'artifacts' 'installers') $Runtime
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

$assetsDirectory = Join-Path (Join-Path $PSScriptRoot '..') 'src\applanch\Assets'
$setupIconPath = Join-Path $assetsDirectory 'applanch.ico'
$largeLogoPath = Join-Path $assetsDirectory 'applanch150.png'
$smallLogoPath = Join-Path $assetsDirectory 'applanch44.png'

if (-not (Test-Path -Path $setupIconPath -PathType Leaf)) {
    Write-Error "Installer icon not found: '$setupIconPath'"
    exit 1
}

if (-not (Test-Path -Path $largeLogoPath -PathType Leaf)) {
    Write-Error "Installer large logo image not found: '$largeLogoPath'"
    exit 1
}

if (-not (Test-Path -Path $smallLogoPath -PathType Leaf)) {
    Write-Error "Installer small logo image not found: '$smallLogoPath'"
    exit 1
}

Add-Type -AssemblyName System.Drawing

function New-BrandedWizardBitmap {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,

        [Parameter(Mandatory = $true)]
        [int]$Width,

        [Parameter(Mandatory = $true)]
        [int]$Height,

        [Parameter(Mandatory = $true)]
        [string]$LogoPath,

        [Parameter(Mandatory = $true)]
        [bool]$IsLargeImage
    )

    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $logo = [System.Drawing.Image]::FromFile($LogoPath)
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 247, 250))
    $accentBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(43, 72, 117))
    $fontFamily = New-Object System.Drawing.FontFamily 'Segoe UI'
    $fontSize = if ($IsLargeImage) { 19.0 } else { 10.0 }
    $titleFont = New-Object System.Drawing.Font($fontFamily, [single]$fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Point)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.Clear([System.Drawing.Color]::FromArgb(24, 42, 74))

        $logoSize = if ($IsLargeImage) { [math]::Min(118, $Width - 28) } else { [math]::Min(36, $Width - 12) }
        $logoX = [int](($Width - $logoSize) / 2)
        $logoY = if ($IsLargeImage) { 42 } else { 8 }
        $graphics.FillRectangle($brush, 0, 0, $Width, $Height)
        $graphics.FillRectangle($accentBrush, 0, 0, $Width, $(if ($IsLargeImage) { 94 } else { 22 }))
        $graphics.DrawImage($logo, $logoX, $logoY, $logoSize, $logoSize)

        if ($IsLargeImage) {
            $graphics.DrawString('applanch', $titleFont, [System.Drawing.Brushes]::White, 23, 8)
        }
    }
    finally {
        $titleFont.Dispose()
        $fontFamily.Dispose()
        $accentBrush.Dispose()
        $brush.Dispose()
        $logo.Dispose()
        $graphics.Dispose()
        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
        $bitmap.Dispose()
    }
}

$wizardImagePath = Join-Path ([System.IO.Path]::GetTempPath()) ("applanch-wizard-large-" + [Guid]::NewGuid().ToString('N') + '.bmp')
$wizardSmallImagePath = Join-Path ([System.IO.Path]::GetTempPath()) ("applanch-wizard-small-" + [Guid]::NewGuid().ToString('N') + '.bmp')
New-BrandedWizardBitmap -OutputPath $wizardImagePath -Width 164 -Height 314 -LogoPath $largeLogoPath -IsLargeImage $true
New-BrandedWizardBitmap -OutputPath $wizardSmallImagePath -Width 55 -Height 55 -LogoPath $smallLogoPath -IsLargeImage $false

$scriptTemplate = @'
[Setup]
AppId={{D102DAA4-8B3D-44BA-987E-7C03B9D23F8D}
AppName=applanch
AppVerName=applanch __APP_VERSION__
AppVersion=__APP_VERSION__
AppPublisher=ChanyaVRC
AppPublisherURL=https://github.com/ChanyaVRC/applanch
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
SetupIconFile=__SETUP_ICON_FILE__
WizardImageFile=__WIZARD_IMAGE_FILE__
WizardSmallImageFile=__WIZARD_SMALL_IMAGE_FILE__
VersionInfoCompany=ChanyaVRC
VersionInfoDescription=applanch Installer
VersionInfoProductName=applanch Installer
VersionInfoVersion=__APP_VERSION__
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
$installerScript = $installerScript.Replace('__SETUP_ICON_FILE__', '"' + ((Resolve-Path $setupIconPath).Path) + '"')
$installerScript = $installerScript.Replace('__WIZARD_IMAGE_FILE__', '"' + $wizardImagePath + '"')
$installerScript = $installerScript.Replace('__WIZARD_SMALL_IMAGE_FILE__', '"' + $wizardSmallImagePath + '"')

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
    if (Test-Path $wizardImagePath) {
        Remove-Item $wizardImagePath -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $wizardSmallImagePath) {
        Remove-Item $wizardSmallImagePath -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $issPath) {
        Remove-Item $issPath -Force -ErrorAction SilentlyContinue
    }
}