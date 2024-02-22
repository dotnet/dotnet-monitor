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

# Check if the file is a ARM64 PE file
function Test-PeArm64() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  $stream = [System.IO.File]::OpenRead($FileInfo.FullName)
  $reader = [System.IO.BinaryReader]($stream)
  # https://learn.microsoft.com/en-us/windows/win32/debug/pe-format
  # MS DOS Magic Number
  if (0x5A4D -ne $reader.ReadUInt16()) {
    $reader.Dispose()
    return $false
  }
  $stream.Position = 0x3C # NT Header offset, 4 bytes
  $ntHeaderOffset = $reader.ReadUInt32()
  $stream.Position = $ntHeaderOffset
  # PE Magic Number, 4 bytes
  if (0x00004550 -ne $reader.ReadUInt32()) {
    $reader.Dispose()
    return $false;
  }
  # Machine type, 2 bytes
  # 0xAA64 is ARM64
  $isArm64 = 0xAA64 -eq $reader.ReadUInt16();
  $reader.Dispose()
  return $isArm64
}

function Copy-Executable() {
  param(
    [System.IO.FileInfo] $FileInfo
  )
  if (Test-PeArm64 -FileInfo $fileInfo) {
    Skip-File -FileInfo $fileInfo -Reason "ARM64 PE" # ARM64 is not supported by ApiScan
  } else {
    Copy-File -FileInfo $fileInfo
  }
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
    Copy-Executable -FileInfo $fileInfo
  } elseif ($fileInfo.Extension -eq ".exe") { # Executable
    Copy-Executable -FileInfo $fileInfo
  } elseif ($fileInfo.Extension -eq ".pdb") { # Program database
    Copy-File -FileInfo $fileInfo
  } else {
    Skip-File -FileInfo $fileInfo -Reason "Unsupported" # Skip remaining files
  }
}

Write-Host "Copied $copyCount files."
Write-Host "Skipped $skipCount files."
