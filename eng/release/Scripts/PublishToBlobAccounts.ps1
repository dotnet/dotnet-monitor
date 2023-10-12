[CmdletBinding(SupportsShouldProcess)]
Param(
    [Parameter(Mandatory=$true)][string]$AzCopyPath,
    [Parameter(Mandatory=$true)][string]$BuildNumber,
    [Parameter(Mandatory=$true)][string]$ReleaseVersion,
    [Parameter(Mandatory=$true)][string]$DotnetStageAccountKey,
    [Parameter(Mandatory=$true)][string]$DestinationAccountName,
    [Parameter(Mandatory=$true)][string]$DestinationSasTokenBase64,
    [Parameter(Mandatory=$true)][string]$ChecksumsAccountName,
    [Parameter(Mandatory=$true)][string]$ChecksumsSasTokenBase64
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$sourceAccountName = 'dotnetstage'
$sourceContainerName = 'dotnet-monitor'
$destinationContainerName = 'dotnet'

$destinationSasToken = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($DestinationSasTokenBase64))
$checksumsSasToken = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($ChecksumsSasTokenBase64))

function Generate-Source-Uri{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)][string]$AssetType
    )

    return "https://$sourceAccountName.blob.core.windows.net/$sourceContainerName/$BuildNumber/${AssetType}Assets/*"
}

function Generate-Destination-Uri{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)][string]$AccountName
    )

    return "https://$AccountName.blob.core.windows.net/$destinationContainerName/diagnostics/monitor/$ReleaseVersion"
}

function Generate-Sas-Token{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)][string]$StorageAccountName,
        [Parameter(Mandatory=$true)][string]$ContainerName,
        [Parameter(Mandatory=$true)][string]$AccountKey,
        [Parameter(Mandatory=$true)][string]$Permissions
    )

    $context = New-AzStorageContext `
        -StorageAccountName $StorageAccountName `
        -StorageAccountKey $AccountKey

    return New-AzStorageContainerSASToken `
        -Container $ContainerName `
        -Context $context `
        -Permission $Permissions `
        -StartTime (Get-Date).AddMinutes(-15.0) `
        -ExpiryTime (Get-Date).AddHours(1.0)
}

function Transfer-File{
    [CmdletBinding(SupportsShouldProcess)]
    Param(
        [Parameter(Mandatory=$true)][string]$From,
        [Parameter(Mandatory=$true)][string]$To,
        [Parameter(Mandatory=$true)][string]$FromToken,
        [Parameter(Mandatory=$true)][string]$ToToken
    )

    Write-Host "Copy $From -> $To"

    if ($From -eq $to) {
        Write-Host 'Skipping copy because source and destination are the same.'
    } else {
        [array]$azCopyArgs = "$From$FromToken"
        $azCopyArgs += "$To$ToToken"
        $azCopyArgs += "--s2s-preserve-properties"
        $azCopyArgs += "--s2s-preserve-access-tier=false"
        if ($WhatIfPreference) {
            $azCopyArgs += "--dry-run"
        }
        & $AzCopyPath cp @azCopyArgs
    }
}

# Create source URI and SAS token
$sourceUri = Generate-Source-Uri `
    -AssetType 'Blob'
$soureSasToken = Generate-Sas-Token `
    -StorageAccountName $sourceAccountName `
    -ContainerName $sourceContainerName `
    -AccountKey $DotnetStageAccountKey `
    -Permissions 'rl'

# Create destination URI
$destinationUri = Generate-Destination-Uri `
    -AccountName $DestinationAccountName

# Copy files to destination account
Transfer-File `
    -From $sourceUri `
    -FromToken $soureSasToken `
    -To $destinationUri `
    -ToToken $destinationSasToken `
    -WhatIf:$WhatIfPreference

# Create source checksums URI
$checksumsSourceUri = Generate-Source-Uri `
    -AssetType 'Checksum'

# Create checksums destination URI
$checksumsDestinationUri = Generate-Destination-Uri `
    -AccountName $ChecksumsAccountName

# Copy checksums to checksum account
Transfer-File `
    -From $checksumsSourceUri `
    -FromToken $soureSasToken `
    -To $checksumsDestinationUri `
    -ToToken $checksumsSasToken `
    -WhatIf:$WhatIfPreference