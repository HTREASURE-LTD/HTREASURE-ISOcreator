<#
.SYNOPSIS
    Regenerates HTISOCreator\Resources\app.ico (a disc on a dark-blue tile).

.DESCRIPTION
    Draws the icon at 16, 20, 24, 32, 40, 48, 64 and 256 pixels. Sizes up to 64 are stored
    as 32-bit DIBs (readable by System.Drawing.Icon on .NET Framework), 256 as PNG.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Make-Icon.ps1
#>
param([string]$OutFile = (Join-Path $PSScriptRoot '..\HTISOCreator\Resources\app.ico'))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = [double]$size

    # Rounded dark-blue tile
    $r = [Math]::Max(2, $s * 0.18)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $w = $s - 1
    $path.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90)
    $path.AddArc($w - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
    $path.AddArc($w - 2 * $r, $w - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $path.AddArc(0, $w - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $path.CloseFigure()
    $tile = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, $s), ([System.Drawing.Color]::FromArgb(40, 96, 170)), ([System.Drawing.Color]::FromArgb(18, 48, 94))
    $g.FillPath($tile, $path)

    # Disc
    $c = $s / 2
    $dr = $s * 0.36
    $disc = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF ($c - $dr), ($c - $dr)), (New-Object System.Drawing.PointF ($c + $dr), ($c + $dr)), ([System.Drawing.Color]::FromArgb(250, 252, 255)), ([System.Drawing.Color]::FromArgb(176, 192, 214))
    $g.FillEllipse($disc, [single]($c - $dr), [single]($c - $dr), [single](2 * $dr), [single](2 * $dr))

    # Sheen across the disc
    if ($size -ge 24) {
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150, 120, 170, 235)), ([single]([Math]::Max(1, $s * 0.04)))
        $sr = $dr * 0.68
        $g.DrawArc($pen, [single]($c - $sr), [single]($c - $sr), [single](2 * $sr), [single](2 * $sr), 200, 70)
        $pen.Dispose()
    }

    # Hub and hole
    $hr = $s * 0.12
    $hub = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(150, 168, 194))
    $g.FillEllipse($hub, [single]($c - $hr), [single]($c - $hr), [single](2 * $hr), [single](2 * $hr))
    $holeR = [Math]::Max(1, $s * 0.06)
    $hole = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(18, 48, 94))
    $g.FillEllipse($hole, [single]($c - $holeR), [single]($c - $holeR), [single](2 * $holeR), [single](2 * $holeR))

    $g.Dispose()
    return $bmp
}

function Get-DibBytes([System.Drawing.Bitmap]$bmp) {
    $size = $bmp.Width
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms
    $bw.Write([int]40); $bw.Write([int]$size); $bw.Write([int]($size * 2)); $bw.Write([int16]1); $bw.Write([int16]32)
    $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)
    for ($y = $size - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $size; $x++) {
            $p = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$p.B); $bw.Write([byte]$p.G); $bw.Write([byte]$p.R); $bw.Write([byte]$p.A)
        }
    }
    # AND mask (all zero: alpha channel decides transparency), rows padded to 4 bytes
    $maskRow = [int]([Math]::Ceiling($size / 32.0) * 4)
    $bw.Write((New-Object byte[] ($maskRow * $size)))
    $bw.Flush()
    return $ms.ToArray()
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$images = @()
foreach ($size in $sizes) {
    $bmp = New-IconBitmap $size
    if ($size -eq 256) {
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += , @($size, $ms.ToArray())
    } else {
        $images += , @($size, (Get-DibBytes $bmp))
    }
    $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([int16]0); $w.Write([int16]1); $w.Write([int16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $dim = if ($img[0] -ge 256) { 0 } else { $img[0] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([int16]1); $w.Write([int16]32); $w.Write([int]$img[1].Length); $w.Write([int]$offset)
    $offset += $img[1].Length
}
foreach ($img in $images) { $w.Write([byte[]]$img[1]) }
$w.Flush()

New-Item -ItemType Directory -Force (Split-Path $OutFile) | Out-Null
[System.IO.File]::WriteAllBytes($OutFile, $out.ToArray())
Write-Host "Wrote $OutFile ($($out.Length) bytes, sizes: $($sizes -join ', '))"
