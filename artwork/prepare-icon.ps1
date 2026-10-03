param([Parameter(Mandatory=$true)][string]$SourcePath)
$ErrorActionPreference = 'Stop'
$artworkPath = Join-Path $PSScriptRoot 'DadsFPP-original.png'
$iconPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'package\icon.png'
if ((Test-Path -LiteralPath $artworkPath) -or (Test-Path -LiteralPath $iconPath)) { throw 'Icon output already exists; preserve it before generating another version.' }
Copy-Item -LiteralPath $SourcePath -Destination $artworkPath
Add-Type -AssemblyName System.Drawing
$source = [Drawing.Image]::FromFile($artworkPath)
$bitmap = New-Object Drawing.Bitmap 256,256
$graphics = [Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.DrawImage($source, 0, 0, 256, 256)
    $bitmap.Save($iconPath, [Drawing.Imaging.ImageFormat]::Png)
} finally { $graphics.Dispose(); $bitmap.Dispose(); $source.Dispose() }
Write-Output $iconPath
