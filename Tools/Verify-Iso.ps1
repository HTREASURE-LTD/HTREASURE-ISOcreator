<#
.SYNOPSIS
    Independently verifies an ISO image created by HT ISO Creator (Joliet format).

.DESCRIPTION
    Mounts the image with Windows' own ISO driver (Mount-DiskImage), which is
    independent of the DiscUtils library used to write it, then compares every
    file and folder against the source folder by relative path, size and SHA-256.
    The image is always dismounted afterwards.

    Use this for images created with Joliet enabled (the default). Without Joliet,
    names are deliberately converted to ISO 9660 form and will not match the source.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Verify-Iso.ps1 -IsoPath D:\ISO\Tools.iso -SourceFolder C:\Software\Tools
#>
param(
    [Parameter(Mandatory = $true)][string]$IsoPath,
    [Parameter(Mandatory = $true)][string]$SourceFolder,
    [string]$ExpectedLabel
)

$ErrorActionPreference = 'Stop'
$IsoPath = (Resolve-Path -LiteralPath $IsoPath).Path
$SourceFolder = (Resolve-Path -LiteralPath $SourceFolder).Path.TrimEnd('\')

function Get-Tree([string]$root) {
    $items = @{}
    foreach ($item in Get-ChildItem -LiteralPath $root -Recurse -Force) {
        $relative = $item.FullName.Substring($root.Length).TrimStart('\')
        if ($item.PSIsContainer) {
            $items[$relative] = 'DIR'
        } else {
            $hash = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
            $items[$relative] = "$($item.Length):$hash"
        }
    }
    return $items
}

$image = Mount-DiskImage -ImagePath $IsoPath -PassThru
try {
    $volume = $image | Get-Volume
    if (-not $volume.DriveLetter) { throw "Windows mounted the image but assigned no drive letter." }
    $mountRoot = "$($volume.DriveLetter):\"
    Write-Host "Mounted $IsoPath as $mountRoot (label '$($volume.FileSystemLabel)', file system $($volume.FileSystem))"

    $problems = New-Object System.Collections.Generic.List[string]
    if ($ExpectedLabel -and $volume.FileSystemLabel -ne $ExpectedLabel) {
        $problems.Add("Volume label is '$($volume.FileSystemLabel)', expected '$ExpectedLabel'")
    }

    $source = Get-Tree $SourceFolder
    $mounted = Get-Tree $mountRoot.TrimEnd('\')

    foreach ($key in $source.Keys) {
        if (-not $mounted.ContainsKey($key)) { $problems.Add("Missing from image: $key") }
        elseif ($mounted[$key] -ne $source[$key]) { $problems.Add("Different in image: $key") }
    }
    foreach ($key in $mounted.Keys) {
        if (-not $source.ContainsKey($key)) { $problems.Add("Extra in image: $key") }
    }

    $fileCount = ($source.Values | Where-Object { $_ -ne 'DIR' }).Count
    $dirCount = ($source.Values | Where-Object { $_ -eq 'DIR' }).Count
    if ($problems.Count -eq 0) {
        Write-Host "PASS: $fileCount files and $dirCount folders match the source (path, size and SHA-256)." -ForegroundColor Green
        $exitCode = 0
    } else {
        Write-Host "FAIL: $($problems.Count) problem(s):" -ForegroundColor Red
        $problems | ForEach-Object { Write-Host "  $_" }
        $exitCode = 1
    }
}
finally {
    Dismount-DiskImage -ImagePath $IsoPath | Out-Null
}
exit $exitCode
