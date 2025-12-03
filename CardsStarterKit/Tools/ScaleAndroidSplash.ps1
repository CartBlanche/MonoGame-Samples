# Scale Android Splash Screens to Multiple Densities
# This script takes a source splash image and scales it to all required Android densities

param(
    [string]$SourceSplash = "..\Platforms\Android\Resources\drawable\icon.png",  # Using icon as base for now
    [string]$OutputBase = "..\Platforms\Android\Resources",
    [int]$Width = 480,   # Default landscape width for splash
    [int]$Height = 320   # Default landscape height for splash
)

Add-Type -AssemblyName System.Drawing

# Define Android density multipliers for splash screens (landscape orientation)
$densities = @{
    "drawable-mdpi" = @{ Width = 480; Height = 320 }      # 1x
    "drawable-hdpi" = @{ Width = 800; Height = 480 }      # 1.5x
    "drawable-xhdpi" = @{ Width = 1280; Height = 720 }    # 2x
    "drawable-xxhdpi" = @{ Width = 1600; Height = 960 }   # 3x
    "drawable-xxxhdpi" = @{ Width = 1920; Height = 1280 } # 4x
}

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDir $SourceSplash
$outputBasePath = Join-Path $scriptDir $OutputBase

Write-Host "Source splash: $sourcePath" -ForegroundColor Cyan
Write-Host "Output base: $outputBasePath" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: Source splash not found at $sourcePath" -ForegroundColor Red
    exit 1
}

# Load source image
$sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
Write-Host "Source image size: $($sourceImage.Width)x$($sourceImage.Height)" -ForegroundColor Yellow
Write-Host ""

# Process each density
foreach ($density in $densities.GetEnumerator()) {
    $targetWidth = $density.Value.Width
    $targetHeight = $density.Value.Height
    $outputDir = Join-Path $outputBasePath $density.Key
    $outputPath = Join-Path $outputDir "splash.png"

    # Create directory if it doesn't exist
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }

    Write-Host "Creating splash for $($density.Key): ${targetWidth}x${targetHeight} px" -ForegroundColor Green

    # Create new bitmap with solid background color
    $newBitmap = New-Object System.Drawing.Bitmap($targetWidth, $targetHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($newBitmap)

    # Set high quality rendering
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    # Fill background with a color (matching the launcher background color from ic_launcher_background.xml)
    $backgroundColor = [System.Drawing.Color]::FromArgb(255, 154, 206, 235)  # #9ACEEB
    $graphics.Clear($backgroundColor)

    # Calculate scaling to fit icon centered in splash screen (maintain aspect ratio)
    # Use 40% of splash screen height for icon size
    $iconHeight = [int]($targetHeight * 0.4)
    $scale = $iconHeight / $sourceImage.Height
    $iconWidth = [int]($sourceImage.Width * $scale)

    # Center the icon
    $x = [int](($targetWidth - $iconWidth) / 2)
    $y = [int](($targetHeight - $iconHeight) / 2)

    # Draw the scaled icon
    $destRect = New-Object System.Drawing.Rectangle($x, $y, $iconWidth, $iconHeight)
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
Write-Host "Splash screen scaling complete!" -ForegroundColor Green
Write-Host "All density splash screens have been generated." -ForegroundColor Green
