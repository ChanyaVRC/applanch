#Requires -Version 5.1
<#
.SYNOPSIS
    Signs and verifies an MSIX package.
.DESCRIPTION
    Uses signtool.exe from the Windows SDK to sign the specified MSIX package
    with a Base64-encoded PFX certificate, then verifies the signature.
.PARAMETER MsixPath
    Path to the MSIX file to sign.
.PARAMETER CertificateBase64
    Base64-encoded PFX certificate content. Defaults to env:MSIX_SIGNING_CERT_BASE64.
.PARAMETER CertificatePassword
    PFX password. Defaults to env:MSIX_SIGNING_CERT_PASSWORD.
.PARAMETER TimestampUrl
    RFC3161 timestamp URL.
.PARAMETER TrustSelfSignedCertificateForVerification
    When true, allows verification to continue for self-signed certificates if
    the only failure is an untrusted-root chain.
.EXAMPLE
    ./scripts/sign-msix.ps1 -MsixPath publish/win-x64/applanch.msix
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$MsixPath,

    [string]$CertificateBase64 = $env:MSIX_SIGNING_CERT_BASE64,

    [string]$CertificatePassword = $env:MSIX_SIGNING_CERT_PASSWORD,

    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [bool]$TrustSelfSignedCertificateForVerification = $true
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $MsixPath -PathType Leaf)) {
    Write-Error "MSIX file not found: '$MsixPath'"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($CertificateBase64)) {
    Write-Error "CertificateBase64 is required (parameter or MSIX_SIGNING_CERT_BASE64 env var)."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    Write-Error "CertificatePassword is required (parameter or MSIX_SIGNING_CERT_PASSWORD env var)."
    exit 1
}

$kitsRoot = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots' -ErrorAction SilentlyContinue).KitsRoot10
if ([string]::IsNullOrWhiteSpace($kitsRoot)) {
    Write-Error "Windows SDK root not found in registry."
    exit 1
}

$signtool = Get-ChildItem (Join-Path $kitsRoot 'bin') -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like '*x64*' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if ([string]::IsNullOrWhiteSpace($signtool) -or -not (Test-Path $signtool)) {
    Write-Error "signtool.exe not found. Install the Windows SDK."
    exit 1
}

$pfxPath = Join-Path ([System.IO.Path]::GetTempPath()) ("applanch-signing-" + [Guid]::NewGuid().ToString("N") + ".pfx")
$certificate = $null
try {
    [System.IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($CertificateBase64))
    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($pfxPath, $CertificatePassword)
    $isSelfSignedCertificate = $certificate.Subject -eq $certificate.Issuer

    & $signtool sign /fd SHA256 /f $pfxPath /p $CertificatePassword /tr $TimestampUrl /td SHA256 /v $MsixPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "signtool sign failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }

    $verifyOutput = & $signtool verify /pa /v $MsixPath 2>&1
    $verifyOutput | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        $verifyOutputText = $verifyOutput | Out-String
        $isUntrustedRootChainFailure =
            ($verifyOutputText -match 'terminated in a root') -and
            ($verifyOutputText -match 'not trusted by the trust provider')

        if ($TrustSelfSignedCertificateForVerification -and $isSelfSignedCertificate -and $isUntrustedRootChainFailure) {
            Write-Warning "signtool verify reported untrusted root for self-signed certificate; continuing."
            $global:LASTEXITCODE = 0
        }
        else {
            Write-Error "signtool verify failed with exit code $LASTEXITCODE."
            exit $LASTEXITCODE
        }
    }

    Write-Host "MSIX signed and verified: $MsixPath"
}
finally {
    if ($certificate) {
        $certificate.Dispose()
    }

    if (Test-Path $pfxPath) {
        Remove-Item $pfxPath -Force -ErrorAction SilentlyContinue
    }
}
