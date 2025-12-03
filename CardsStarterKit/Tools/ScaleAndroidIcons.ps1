# Scale Android Icons to Multiple Densities
# This script takes a source icon and scales it to all required Android densities

param(
    [string]$SourceIcon = "..\Platforms\Android\Resources\drawable\icon.png",
    [string]$OutputBase = "..\Platforms\Android\Resources"
)

Add-Type -AssemblyName System.Drawing

# Define Android density sizes for launcher icons
$densities = @{
    "drawable-mdpi" = 48
    "drawable-hdpi" = 72
    "drawable-xhdpi" = 96
    "drawable-xxhdpi" = 144
    "drawable-xxxhdpi" = 192
}

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDir $SourceIcon
$outputBasePath = Join-Path $scriptDir $OutputBase

Write-Host "Source icon: $sourcePath" -ForegroundColor Cyan
Write-Host "Output base: $outputBasePath" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: Source icon not found at $sourcePath" -ForegroundColor Red
    exit 1
}

# Load source image
$sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
Write-Host "Source image size: $($sourceImage.Width)x$($sourceImage.Height)" -ForegroundColor Yellow

# Check if image is square
if ($sourceImage.Width -ne $sourceImage.Height) {
    Write-Host "Warning: Source image is not square. Will scale to fit within square bounds." -ForegroundColor Yellow
    $maxDimension = [Math]::Max($sourceImage.Width, $sourceImage.Height)
} else {
    $maxDimension = $sourceImage.Width
}

Write-Host ""

# Process each density
foreach ($density in $densities.GetEnumerator()) {
    $targetSize = $density.Value
    $outputDir = Join-Path $outputBasePath $density.Key
    $outputPath = Join-Path $outputDir "icon.png"

    # Create directory if it doesn't exist
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }

    Write-Host "Scaling to $($density.Key): ${targetSize}x${targetSize} px" -ForegroundColor Green

    # Create new bitmap
    $newBitmap = New-Object System.Drawing.Bitmap($targetSize, $targetSize)
    $graphics = [System.Drawing.Graphics]::FromImage($newBitmap)

    # Set high quality rendering
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    # Calculate scaling to fit within target size (maintain aspect ratio)
    $scaleWidth = $targetSize / $sourceImage.Width
    $scaleHeight = $targetSize / $sourceImage.Height
    $scale = [Math]::Min($scaleWidth, $scaleHeight)

    $newWidth = [int]($sourceImage.Width * $scale)
    $newHeight = [int]($sourceImage.Height * $scale)

    # Center the image
    $x = [int](($targetSize - $newWidth) / 2)
    $y = [int](($targetSize - $newHeight) / 2)

    # Clear background (transparent)
    $graphics.Clear([System.Drawing.Color]::Transparent)

    # Draw the scaled image
    $destRect = New-Object System.Drawing.Rectangle($x, $y, $newWidth, $newHeight)
    $srcRect = New-Object System.Drawing.Rectangle(0, 0, $sourceImage.Width, $sourceImage.Height)
    $graphics.DrawImage($sourceImage, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    # Save the image
    $newBitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

    # Cleanup
    $graphics.Dispose()
    $newBitmap.Dispose()

    Write-Host "  -> Saved to: $outputPath" -ForegroundColor Gray
}

# Cleanup source image
$sourceImage.Dispose()

Write-Host ""
Write-Host "Icon scaling complete!" -ForegroundColor Green
Write-Host "All density icons have been generated." -ForegroundColor Green
