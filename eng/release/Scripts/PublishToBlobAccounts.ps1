[CmdletBinding(SupportsShouldProcess)]
Param(
    [Parameter(Mandatory=$true)][string]$AzCopyPath,
    [Parameter(Mandatory=$true)][string]$BuildVersion,
    [Parameter(Mandatory=$true)][string]$ReleaseVersion,
    [Parameter(Mandatory=$true)][string]$DestinationAccountName,
    [Parameter(Mandatory=$true)][string]$DestinationSasTokenBase64,
    [Parameter(Mandatory=$true)][string]$ChecksumsAccountName
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

# Use the OAuth token that was obtained by the az cli when it logged in.
$Env:AZCOPY_AUTO_LOGIN_TYPE="AZCLI"

$sourceAccountName = 'dotnetstage'
$sourceContainerName = 'dotnet-monitor'
$destinationContainerName = 'dotnet'

function Generate-Source-Uri{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)][string]$AssetType
    )

    return "https://$sourceAccountName.blob.core.windows.net/$sourceContainerName/$BuildVersion/${AssetType}Assets/*"
}

function Generate-Destination-Uri{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)][string]$AccountName
    )

    return "https://$AccountName.blob.core.windows.net/$destinationContainerName/diagnostics/monitor/$ReleaseVersion"
}

function Transfer-File{
    [CmdletBinding(SupportsShouldProcess)]
    Param(
        [Parameter(Mandatory=$true)][string]$From,
        [Parameter(Mandatory=$true)][string]$To,
        [Parameter(Mandatory=$false)][string]$ToToken = $null
    )

    if ($ToToken -and ($ToToken[0] -ne '?')) {
        $ToToken = '?' + $ToToken
    }

    Write-Host "Copy $From -> $To"

    if ($From -eq $to) {
        Write-Host 'Skipping copy because source and destination are the same.'
    } else {
        [array]$azCopyArgs = "$From"
        $azCopyArgs += "$To$ToToken"
        $azCopyArgs += "--s2s-preserve-properties"
        $azCopyArgs += "--s2s-preserve-access-tier=false"
        if ($WhatIfPreference) {
            $azCopyArgs += "--dry-run"
        }
        & $AzCopyPath cp @azCopyArgs
    }
}

# Create source URI
$sourceUri = Generate-Source-Uri `
    -AssetType 'Blob'

# Create destination URI
$destinationUri = Generate-Destination-Uri `
    -AccountName $DestinationAccountName

# Copy files to destination account
Transfer-File `
    -From $sourceUri `
    -To $destinationUri `
    -ToToken ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($DestinationSasTokenBase64))) `
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
    -To $checksumsDestinationUri `
    -WhatIf:$WhatIfPreference
