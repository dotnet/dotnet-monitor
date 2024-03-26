param(
  [Parameter(Mandatory=$true)][string] $SourcePath,
  [Parameter(Mandatory=$true)][string] $TargetPath,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

if ($null -ne $properties) {
  Write-Host "Unexpected extra parameters: $properties."
  exit 1
}

$copyCount = 0
$skipCount = 0

# Copy files from source to target directory while preserving the relative directory structure
function Copy-File() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  Write-Host "Copying $($FileInfo.FullName)"
  $relativePath = [System.IO.Path]::GetRelativePath($SourcePath, $FileInfo.FullName)
  $fileTargetPath = Join-Path -Path $TargetPath -ChildPath $relativePath
  $fileTargetDir = [System.IO.Path]::GetDirectoryName($fileTargetPath)
  [System.IO.Directory]::CreateDirectory($fileTargetDir) | Out-Null
  Copy-Item -Path $FileInfo.FullName -Destination $fileTargetPath
  $script:copyCount++
}

function Skip-File() {
  param(
    [System.IO.FileInfo] $FileInfo,
    [string] $Reason
  )
  Write-Host "Skipping $($FileInfo.FullName): $Reason"
  $script:skipCount++
}

New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null

foreach ($fileInfo in (Get-ChildItem $SourcePath -Recurse -Attributes !Directory)) {
  if ($fileInfo.Directory.FullName.Contains('symbols')) { # Skip symbols packages
    Skip-File -FileInfo $fileInfo -Reason "Symbols"
  } elseif ($fileInfo.Extension -eq ".dll") { # Library
    Copy-File -FileInfo $fileInfo
  } elseif ($fileInfo.Extension -eq ".exe") { # Executable
    Copy-File -FileInfo $fileInfo
  } elseif ($fileInfo.Extension -eq ".pdb") { # Program database
    Copy-File -FileInfo $fileInfo
  } else {
    Skip-File -FileInfo $fileInfo -Reason "Unsupported" # Skip remaining files
  }
}

Write-Host "Copied $copyCount files."
Write-Host "Skipped $skipCount files."
