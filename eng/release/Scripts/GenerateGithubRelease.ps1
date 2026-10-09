param(
  [Parameter(Mandatory=$true)][string] $ManifestPath,
  [Parameter(Mandatory=$false)][string] $ReleaseNotes,
  [Parameter(Mandatory=$true)][string] $GhOrganization,
  [Parameter(Mandatory=$true)][string] $GhRepository,
  [Parameter(Mandatory=$false)][string] $GhCliLink = "https://github.com/cli/cli/releases/download/v1.2.0/gh_1.2.0_windows_amd64.zip",
  [Parameter(Mandatory=$true)][string] $TagName,
  [bool] $DraftRelease = $false,
  [switch] $help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)
function Write-Help() {
    Write-Host "Publish release to GitHub. Expects an environtment variable GITHUB_TOKEN to perform auth."
    Write-Host "Common settings:"
    Write-Host "  -ManifestPath <value>       Path to a publishing manifest."
    Write-Host "  -ReleaseNotes <value>       Path to release notes."
    Write-Host "  -GhOrganization <value>     GitHub organization the repository lives in."
    Write-Host "  -GhRepository <value>       GitHub repository in the organization to create the release on."
    Write-Host "  -GhCliLink <value>          GitHub CLI download link."
    Write-Host "  -TagName <value>            Tag to use for the release."
    Write-Host "  -DraftRelease               Stage the release, but don't make it public yet."
    Write-Host ""
}
function Get-ReleaseNotes()
{
    if ($ReleaseNotes)
    {
        if (!(Test-Path $ReleaseNotes))
        {
            Write-Error "Error: unable to find notes at $ReleaseNotes."
            exit 1
        }

        return Get-Content -Raw -Path $ReleaseNotes
    }
}

function Get-ReleasedPackages ($manifest)
{
    if ($manifest.NugetAssets.Length -eq 0)
    {
        return ""
    }

    $releasedAssetTable = "`n`n<details>`n"
    $releasedAssetTable += "<summary>Packages released to NuGet</summary>`n`n"

    foreach ($nugetPackage in $manifest.NugetAssets)
    {
        $packageName = Split-Path $nugetPackage.PublishRelativePath -Leaf
        $releasedAssetTable += "- ``" + $packageName  + "```n"
    }

    $releasedAssetTable += "</details>`n`n"

    return $releasedAssetTable
}

function Test-GitHubCommit([string] $commit)
{
    $headers = @{
        Authorization = "Bearer $env:GITHUB_TOKEN"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "dotnet-monitor-release"
    }

    try
    {
        Invoke-RestMethod `
            -Uri "https://api.github.com/repos/$GhOrganization/$GhRepository/commits/$commit" `
            -Headers $headers `
            -Method Get | Out-Null
        return $true
    }
    catch
    {
        $statusCode = [int]$_.Exception.Response.StatusCode
        if ($statusCode -eq 404 -or $statusCode -eq 422)
        {
            return $false
        }

        throw
    }
}

function Resolve-GitHubCommit($manifest)
{
    if (Test-GitHubCommit $manifest.Commit)
    {
        return $manifest.Commit
    }

    if ([string]::IsNullOrWhiteSpace($env:SYSTEM_ACCESSTOKEN))
    {
        Write-Error "Manifest commit '$($manifest.Commit)' is not available in GitHub, and SYSTEM_ACCESSTOKEN is unavailable to resolve its public parent."
        exit 1
    }

    if ($manifest.RepoUrl -notmatch "^(https://dev\.azure\.com/[^/]+/[^/]+)/_git/([^/]+)$")
    {
        Write-Error "Manifest commit '$($manifest.Commit)' is not available in GitHub, and repository '$($manifest.RepoUrl)' is not a supported Azure Repos URL."
        exit 1
    }

    $projectUrl = $Matches[1]
    $repositoryName = [Uri]::EscapeDataString($Matches[2])
    $commitUri = "$projectUrl/_apis/git/repositories/$repositoryName/commits/$($manifest.Commit)?api-version=7.1"

    try
    {
        $internalCommit = Invoke-RestMethod `
            -Uri $commitUri `
            -Headers @{ Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN" } `
            -Method Get
    }
    catch
    {
        Write-Error "Unable to inspect internal manifest commit '$($manifest.Commit)': $_"
        exit 1
    }

    $publicParents = @($internalCommit.parents | Where-Object { Test-GitHubCommit $_ })
    if ($publicParents.Count -ne 1)
    {
        Write-Error "Expected exactly one GitHub parent for internal manifest commit '$($manifest.Commit)', but found $($publicParents.Count)."
        exit 1
    }

    Write-Host "Resolved internal manifest commit '$($manifest.Commit)' to GitHub commit '$($publicParents[0])'."
    return $publicParents[0]
}

function Post-GithubRelease($manifest, [string]$releaseBody, [string]$targetCommit)
{
    $extractionPath = New-TemporaryFile | % { Remove-Item $_; New-Item -ItemType Directory -Path $_ }
    $zipPath = Join-Path $extractionPath "ghcli.zip"
    $ghTool = [IO.Path]::Combine($extractionPath, "bin", "gh.exe")

    Write-Host "Downloading GitHub CLI from $GhCliLink."
    try
    {
        $progressPreference = 'silentlyContinue'
        Invoke-WebRequest $GhCliLink -OutFile $zipPath
        Expand-Archive -Path $zipPath -DestinationPath $extractionPath
        $progressPreference = 'Continue'
    }
    catch 
    {
        Write-Error "Unable to get GitHub CLI for release"
        exit 1
    }

    if (!(Test-Path $ghTool))
    {
        Write-Error "Error: unable to find GitHub tool at expected location."
        exit 1
    }

    if (!(Test-Path env:GITHUB_TOKEN))
    {
        Write-Error "Error: unable to find GitHub token. Please set GITHUB_TOKEN."
        exit 1
    }

    $extraParameters = @()

    if ($DraftRelease -eq $true)
    {
        $extraParameters += '-d'
    }

    $releaseNotes = "release_notes.md"

    Set-Content -Path $releaseNotes -Value $releaseBody

    if (-Not (Test-Path $releaseNotes)) {
        Write-Error "Unable to find release notes"
    }

    $releaseNotes = $(Get-ChildItem $releaseNotes).FullName
    & $ghTool release create $TagName `
        --repo "`"$GhOrganization/$GhRepository`"" `
        --title "`"Dotnet-Monitor Release - $TagName`"" `
        --notes-file "`"$releaseNotes`"" `
        --target $targetCommit `
        ($extraParameters -join ' ')

    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Write-Error "Something failed in creating the release."
        exit 1
    }
}

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?')))) {
    Write-Help
    exit 1
}

if ($null -ne $properties) {
    Write-Error "Unexpected extra parameters: $properties."
    exit 1
}

if (!(Test-Path $ManifestPath))
{
    Write-Error "Error: unable to find manifest at $ManifestPath."
    exit 1
}

$manifestSize = $(Get-ChildItem $ManifestPath).length / 1kb

# Limit size. For large manifests
if ($manifestSize -gt 500)
{
    Write-Error "Error: Manifest $ManifestPath too large."
    exit 1
}

$manifestJson = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
$releaseNotesText = Get-ReleaseNotes
$releaseNotesText += Get-ReleasedPackages $manifestJson
$targetCommit = Resolve-GitHubCommit $manifestJson

Post-GithubRelease -manifest $manifestJson `
                -releaseBody $releaseNotesText `
                -targetCommit $targetCommit `
