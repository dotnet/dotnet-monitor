[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $KeyVaultName,

    [Parameter(Mandatory = $true)]
    [string] $KeyName,

    [Parameter(Mandatory = $true)]
    [string] $AppClientId,

    [Parameter(Mandatory = $true)]
    [string] $InstallationOwner,

    [Parameter(Mandatory = $true)]
    [string] $OutputVariableName
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

. $PSScriptRoot\..\..\common\pipeline-logging-functions.ps1

function ConvertTo-Base64Url([byte[]] $bytes) {
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$jwtHeader = [ordered]@{
    alg = 'RS256'
    typ = 'JWT'
}
$now = [System.DateTimeOffset]::UtcNow
$jwtPayload = [ordered]@{
    iat = $now.AddMinutes(-1).ToUnixTimeSeconds()
    exp = $now.AddMinutes(5).ToUnixTimeSeconds()
    iss = $AppClientId
}

$headerEncoded = ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes(($jwtHeader | ConvertTo-Json -Compress)))
$payloadEncoded = ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes(($jwtPayload | ConvertTo-Json -Compress)))
$signingInput = "$headerEncoded.$payloadEncoded"

$sha256 = [System.Security.Cryptography.SHA256]::Create()
$digestBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput))
$digestBase64 = [Convert]::ToBase64String($digestBytes)

Write-Host "Signing JWT with key '$KeyName' in vault '$KeyVaultName'..."
$previousNativeCommandErrorPreference = $PSNativeCommandUseErrorActionPreference
try {
    $PSNativeCommandUseErrorActionPreference = $false
    $signatureBase64 = az keyvault key sign `
        --vault-name $KeyVaultName `
        --name $KeyName `
        --algorithm RS256 `
        --digest $digestBase64 `
        --query signature `
        --output tsv `
        --only-show-errors
    $signExitCode = $LASTEXITCODE
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to sign the JWT via Key Vault (key '$KeyName', vault '$KeyVaultName'): $_. Verify the service connection identity has the 'Key Vault Crypto User' role (Sign action) on the key."
    exit 1
}
finally {
    $PSNativeCommandUseErrorActionPreference = $previousNativeCommandErrorPreference
}
if ($signExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($signatureBase64)) {
    Write-PipelineTelemetryError -Category 'Build' -Message "'az keyvault key sign' exited with code $signExitCode for key '$KeyName' in vault '$KeyVaultName'. Verify the service connection identity has the 'Key Vault Crypto User' role (Sign action) on the key."
    exit 1
}
$signatureUrl = $signatureBase64.Trim().TrimEnd('=').Replace('+', '-').Replace('/', '_')
$jwt = "$signingInput.$signatureUrl"

$headers = @{
    Authorization          = "Bearer $jwt"
    'X-GitHub-Api-Version' = '2022-11-28'
    Accept                 = 'application/vnd.github+json'
    'User-Agent'           = 'dotnet-monitor-release-pipeline'
}

Write-Host "Looking up installation for '$InstallationOwner'..."
try {
    $installations = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        $pageResponse = Invoke-RestMethod `
            -Uri "https://api.github.com/app/installations?per_page=100&page=$page" `
            -Headers $headers `
            -Method Get
        $pageInstallationCount = 0
        foreach ($installation in $pageResponse) {
            $installations.Add($installation)
            $pageInstallationCount++
        }
        $page++
    } while ($pageInstallationCount -eq 100)
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to list GitHub App installations: $_. The signed JWT may be invalid or the App's Client ID ('$AppClientId') may be incorrect."
    exit 1
}

$matchingInstallations = @($installations | Where-Object { $_.account.login -ieq $InstallationOwner })
if ($matchingInstallations.Count -eq 0) {
    $found = ($installations | ForEach-Object { $_.account.login }) -join ', '
    Write-PipelineTelemetryError -Category 'Build' -Message "No installation found for '$InstallationOwner'. App is installed on: $found"
    exit 1
}
if ($matchingInstallations.Count -ne 1) {
    $matchingIds = ($matchingInstallations | ForEach-Object { $_.id }) -join ', '
    Write-PipelineTelemetryError -Category 'Build' -Message "Found multiple installations for '$InstallationOwner': $matchingIds"
    exit 1
}
$installation = $matchingInstallations[0]
Write-Host "Using installation $($installation.id) for '$($installation.account.login)'."

try {
    $tokenResponse = Invoke-RestMethod `
        -Uri "https://api.github.com/app/installations/$($installation.id)/access_tokens" `
        -Headers $headers `
        -Method Post `
        -ContentType 'application/json'
}
catch {
    Write-PipelineTelemetryError -Category 'Build' -Message "Failed to mint an installation access token for '$InstallationOwner' (installation $($installation.id)): $_"
    exit 1
}

Write-Host "Got installation token for '$InstallationOwner' (expires $($tokenResponse.expires_at))."
Write-Host "Setting pipeline variable '$OutputVariableName'."
Write-Host "##vso[task.setvariable variable=$OutputVariableName;issecret=true]$($tokenResponse.token)"
