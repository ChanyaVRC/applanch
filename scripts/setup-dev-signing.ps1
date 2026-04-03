#Requires -Version 5.1
<#
.SYNOPSIS
    Sets up a local code-signing certificate for signing the sparse MSIX in development.
.DESCRIPTION
    Creates a self-signed code-signing certificate with subject CN=applanch,
    installs the public certificate to LocalMachine\TrustedPeople, and outputs
    the thumbprint for use by build-sparse-package.ps1 when signing the MSIX.

    Run this script once (as Administrator) on a new development machine.
    After this, build-sparse-package.ps1 automatically signs the MSIX with
    the dev certificate, allowing sparse package registration to work on
    Windows 11 without enabling Developer Mode.
.EXAMPLE
    # Run from an elevated PowerShell prompt:
    .\scripts\setup-dev-signing.ps1
#>

$ErrorActionPreference = 'Stop'

$subject = 'CN=applanch'

# Create the certificate in CurrentUser\My if it doesn't already exist.
$cert = Get-ChildItem 'Cert:\CurrentUser\My' |
    Where-Object { $_.Subject -eq $subject -and $_.NotAfter -gt (Get-Date) -and $_.HasPrivateKey } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($cert) {
    Write-Host "Existing dev certificate found: $($cert.Thumbprint)"
}
else {
    Write-Host "Creating self-signed code-signing certificate with subject '$subject'..."
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $subject `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -KeyExportPolicy Exportable `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(10)
    Write-Host "Created certificate: $($cert.Thumbprint)"
}

# Install the public certificate to LocalMachine\TrustedPeople so that Windows
# considers sparse MSIXs signed with this cert as trusted (requires elevation).
$trusted = Get-ChildItem 'Cert:\LocalMachine\TrustedPeople' |
    Where-Object { $_.Thumbprint -eq $cert.Thumbprint } |
    Select-Object -First 1

if ($trusted) {
    Write-Host "Certificate is already trusted in LocalMachine\TrustedPeople."
}
else {
    Write-Host "Installing public certificate to LocalMachine\TrustedPeople (requires admin)..."

    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
        [System.Security.Cryptography.X509Certificates.StoreName]::TrustedPeople,
        [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
    try {
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $store.Add($cert)
    }
    finally {
        $store.Close()
    }

    Write-Host "Certificate installed to LocalMachine\TrustedPeople."
}

Write-Host ""
Write-Host "Dev signing is ready."
Write-Host "  Thumbprint : $($cert.Thumbprint)"
Write-Host "  Subject    : $($cert.Subject)"
Write-Host "  Expires    : $($cert.NotAfter.ToString('yyyy-MM-dd'))"
Write-Host ""
Write-Host "Run .\scripts\build-sparse-package.ps1 to build and sign the MSIX."
Write-Host "The MSIX will be automatically signed whenever this cert is present in CurrentUser\My."
