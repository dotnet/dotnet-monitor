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

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Create-PackageDir() {
  param( 
    [string] $FileName
  )
  $extractionPath = Join-Path -Path $TargetPath -ChildPath ($FileName + "_dir")
  New-Item -ItemType Directory -Force -Path $extractionPath | Out-Null
  return $extractionPath
}

function Extract-Zip() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  $extractionPath = Create-PackageDir -FileName $FileInfo.Name
  Write-Host "Extracting $($FileInfo.Name)"
  [System.IO.Compression.ZipFile]::ExtractToDirectory($FileInfo.FullName, $extractionPath)
}

function Extract-TarGz() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  $extractionPath = Create-PackageDir -FileName $FileInfo.Name
  Write-Host "Extracting $($FileInfo.Name)"
  tar -xf $FileInfo.FullName -C $extractionPath
}

function Copy-File() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  Write-Host "Copying $($FileInfo.Name)"
  Copy-Item -Path $FileInfo.FullName -Destination $TargetPath
}

New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null

foreach ($fileInfo in (Get-ChildItem $SourcePath -Recurse -Attributes !Directory)) {
  if ($fileInfo.Name.EndsWith('symbols.nupkg')) {
    Extract-Zip -FileInfo $fileInfo
  } elseif ($fileInfo.Name.EndsWith('nupkg')) {
    Extract-Zip -FileInfo $fileInfo
  } elseif ($fileInfo.Name.EndsWith('zip')) {
    Extract-Zip -FileInfo $fileInfo
  } elseif ($fileInfo.Name.EndsWith('tar.gz')) {
    Extract-TarGz -FileInfo $fileInfo
  } else {
    Copy-File -FileInfo $fileInfo
  }
}
