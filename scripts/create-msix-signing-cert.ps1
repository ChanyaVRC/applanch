#Requires -Version 5.1
<#
.SYNOPSIS
    Creates an ephemeral self-signed certificate for MSIX signing in CI.
.DESCRIPTION
    Generates a self-signed code-signing certificate, exports it as PFX, converts
    it to Base64, then writes both cert and password to GITHUB_ENV-compatible
    environment variables for downstream steps.
.PARAMETER Subject
    Certificate subject. Must match the MSIX manifest publisher.
.PARAMETER FriendlyName
    Friendly certificate name.
.PARAMETER ValidityYears
    Certificate lifetime in years.
.PARAMETER OutputEnvPath
    Destination path for environment variable exports (defaults to GITHUB_ENV).
.PARAMETER CertPasswordEnvName
    Environment variable name to store generated PFX password.
.PARAMETER CertBase64EnvName
    Environment variable name to store generated PFX Base64 data.
#>
param(
    [string]$Subject = 'CN=applanch',
    [string]$FriendlyName = 'applanch CI self-signed certificate',
    [int]$ValidityYears = 3,
    [string]$OutputEnvPath = $env:GITHUB_ENV,
    [string]$CertPasswordEnvName = 'MSIX_SIGNING_CERT_PASSWORD',
    [string]$CertBase64EnvName = 'MSIX_SIGNING_CERT_BASE64'
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputEnvPath)) {
    Write-Error 'OutputEnvPath is required (or set GITHUB_ENV).'
    exit 1
}

$password = [Guid]::NewGuid().ToString('N')
$securePassword = ConvertTo-SecureString $password -AsPlainText -Force
$pfxPath = Join-Path $env:RUNNER_TEMP 'applanch-self-signed.pfx'
$certificate = $null

try {
    $certificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $Subject `
        -FriendlyName $FriendlyName `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -NotAfter (Get-Date).AddYears($ValidityYears) `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')

    Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
    $base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))

    Write-Host "::add-mask::$password"
    Write-Host "::add-mask::$base64"
    "$CertPasswordEnvName=$password" | Out-File -FilePath $OutputEnvPath -Encoding utf8 -Append
    "$CertBase64EnvName=$base64" | Out-File -FilePath $OutputEnvPath -Encoding utf8 -Append
}
finally {
    if (Test-Path $pfxPath) {
        Remove-Item $pfxPath -Force -ErrorAction SilentlyContinue
    }

    if ($certificate -and -not [string]::IsNullOrWhiteSpace($certificate.Thumbprint)) {
        Remove-Item -Path "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }
}
