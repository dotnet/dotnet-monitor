param(
  [Parameter(Mandatory=$true)][string] $SourcePath,
  [Parameter(Mandatory=$true)][string] $BinariesTargetPath,
  [Parameter(Mandatory=$true)][string] $SymbolsTargetPath,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

if ($null -ne $properties) {
  Write-Host "Unexpected extra parameters: $properties."
  exit 1
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-TargetPath() {
  param(
    [string] $FileName
  )
  if ($FileName.Contains('symbols')) {
    return $SymbolsTargetPath
  } else {
    return $BinariesTargetPath
  }
}

function Create-PackageDir() {
  param(
    [string] $FileName
  )
  $targetPath = Get-TargetPath -FileName $FileName
  $extractionPath = Join-Path -Path $targetPath -ChildPath ($FileName + "_dir")
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
  tar -xzf $FileInfo.FullName -C $extractionPath
}

function Copy-File() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  Write-Host "Copying $($FileInfo.Name)"
  Copy-Item -Path $FileInfo.FullName -Destination $BinariesTargetPath
}

New-Item -ItemType Directory -Force -Path $BinariesTargetPath | Out-Null
New-Item -ItemType Directory -Force -Path $SymbolsTargetPath | Out-Null

foreach ($fileInfo in (Get-ChildItem $SourcePath -Recurse -Attributes !Directory)) {
  if ($fileInfo.Name.EndsWith('nupkg')) {
    Extract-Zip -FileInfo $fileInfo
  } elseif ($fileInfo.Name.EndsWith('zip')) {
    Extract-Zip -FileInfo $fileInfo
  } elseif ($fileInfo.Name.EndsWith('tar.gz')) {
    Extract-TarGz -FileInfo $fileInfo
  } else {
    Copy-File -FileInfo $fileInfo
  }
}
