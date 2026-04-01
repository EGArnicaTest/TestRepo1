$pwPemPath = Join-Path $resolvedDir "$certName`_pw.pem"
$pemBuilder = [System.Text.StringBuilder]::new()

foreach ($cert in $pfxCollection) {
    $certBase64 = [Convert]::ToBase64String($cert.RawData, [Base64FormattingOptions]::InsertLineBreaks)
    [void]$pemBuilder.AppendLine("-----BEGIN CERTIFICATE-----")
    [void]$pemBuilder.AppendLine($certBase64)
    [void]$pemBuilder.AppendLine("-----END CERTIFICATE-----")

    if ($cert.HasPrivateKey) {
        $rsaKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
        if ($rsaKey) {
            $passwordBytes = [System.Text.Encoding]::UTF8.GetBytes($CertPassword)
            $pbeParams = New-Object System.Security.Cryptography.PbeParameters(
                [System.Security.Cryptography.PbeEncryptionAlgorithm]::TripleDes3KeyPkcs12,
                [System.Security.Cryptography.HashAlgorithmName]::SHA1,
                2000
            )

            $keyBytes = $rsaKey.ExportEncryptedPkcs8PrivateKey($passwordBytes, $pbeParams)
            $keyBase64 = [Convert]::ToBase64String($keyBytes, [Base64FormattingOptions]::InsertLineBreaks)

            [void]$pemBuilder.AppendLine("-----***** ********* ******* ***-----*)
            [****]***********.**********(**********)
            [****]***********.**********(*-----*** ********* ******* ***-----")
        }
    }
}

$pemBuilder.ToString() | Out-File -FilePath $pwPemPath -Encoding ascii -NoNewline
Write-Host "Password-protected PEM saved to $pwPemPath"
