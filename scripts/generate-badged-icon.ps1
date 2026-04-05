param(
    [string]$OutputPath = 'src/applanch/Assets/applanch-badged.ico'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-BadgedBitmap {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Size
    )

    $scale = $Size / 256.0
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(60, 15, 34, 64))
        $badgeBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(39, 72, 122))
        $ringWidth = [Math]::Max(1.0, 2.0 * $scale)
        $ringPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(140, 255, 255, 255), [single]$ringWidth)

        try {
            $badgeX = 10.0 * $scale
            $badgeY = 10.0 * $scale
            $badgeSize = 236.0 * $scale
            $shadowX = 14.0 * $scale
            $shadowY = 16.0 * $scale

            $graphics.FillEllipse($shadowBrush, [single]$shadowX, [single]$shadowY, [single]$badgeSize, [single]$badgeSize)
            $graphics.FillEllipse($badgeBrush, [single]$badgeX, [single]$badgeY, [single]$badgeSize, [single]$badgeSize)

            $ringInset = 2.0 * $scale
            $graphics.DrawEllipse(
                $ringPen,
                [single]($badgeX + $ringInset),
                [single]($badgeY + $ringInset),
                [single]($badgeSize - (2.0 * $ringInset)),
                [single]($badgeSize - (2.0 * $ringInset))
            )

            $rocketBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 255, 240))
            try {
                $tx = 23.0 * $scale
                $ty = 51.0 * $scale
                $rs = 0.7109375 * $scale
                $points = @(
                    [System.Drawing.PointF]::new([single]($tx + (248.0 * $rs)), [single]($ty + (8.0 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (208.6 * $rs)), [single]($ty + (93.4 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (128.9 * $rs)), [single]($ty + (173.1 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (123.2 * $rs)), [single]($ty + (248.0 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (107.9 * $rs)), [single]($ty + (194.2 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (75.2 * $rs)), [single]($ty + (180.8 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (61.8 * $rs)), [single]($ty + (148.1 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (8.0 * $rs)), [single]($ty + (132.8 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (82.9 * $rs)), [single]($ty + (127.1 * $rs))),
                    [System.Drawing.PointF]::new([single]($tx + (162.6 * $rs)), [single]($ty + (47.4 * $rs)))
                )
                $graphics.FillPolygon($rocketBrush, $points)
            }
            finally {
                $rocketBrush.Dispose()
            }
        }
        finally {
            $ringPen.Dispose()
            $badgeBrush.Dispose()
            $shadowBrush.Dispose()
        }

        return $bitmap
    }
    catch {
        $bitmap.Dispose()
        throw
    }
    finally {
        $graphics.Dispose()
    }
}

function Convert-BitmapToIconImageBytes {
    param(
        [Parameter(Mandatory = $true)]
        [System.Drawing.Bitmap]$Bitmap,

        [Parameter(Mandatory = $true)]
        [int]$Size
    )

    $rect = [System.Drawing.Rectangle]::new(0, 0, $Size, $Size)
    $bitmapData = $Bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    try {
        $rowBytes = $Size * 4
        $xorBytes = New-Object byte[] ($rowBytes * $Size)

        for ($srcY = 0; $srcY -lt $Size; $srcY++) {
            $destY = $Size - 1 - $srcY
            $srcPtr = [System.IntPtr]::Add($bitmapData.Scan0, $srcY * $bitmapData.Stride)
            [System.Runtime.InteropServices.Marshal]::Copy($srcPtr, $xorBytes, $destY * $rowBytes, $rowBytes)
        }

        $maskRowBytes = [int]([Math]::Ceiling($Size / 32.0) * 4)
        $andMaskBytes = New-Object byte[] ($maskRowBytes * $Size)

        $memory = New-Object System.IO.MemoryStream
        $writer = New-Object System.IO.BinaryWriter($memory)

        try {
            $writer.Write([uint32]40)
            $writer.Write([int32]$Size)
            $writer.Write([int32]($Size * 2))
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]0)
            $writer.Write([uint32]$xorBytes.Length)
            $writer.Write([int32]0)
            $writer.Write([int32]0)
            $writer.Write([uint32]0)
            $writer.Write([uint32]0)
            $writer.Write($xorBytes)
            $writer.Write($andMaskBytes)
            $writer.Flush()
            return $memory.ToArray()
        }
        finally {
            $writer.Dispose()
            $memory.Dispose()
        }
    }
    finally {
        $Bitmap.UnlockBits($bitmapData)
    }
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$entries = New-Object System.Collections.Generic.List[object]

foreach ($size in $sizes) {
    $bitmap = New-BadgedBitmap -Size $size
    try {
        $imageBytes = Convert-BitmapToIconImageBytes -Bitmap $bitmap -Size $size
        $entries.Add([PSCustomObject]@{
            Size = $size
            Bytes = [byte[]]$imageBytes
        })
    }
    finally {
        $bitmap.Dispose()
    }
}

$outputFullPath = (Resolve-Path (Split-Path -Parent $OutputPath)).Path
$outputFile = Join-Path $outputFullPath (Split-Path -Leaf $OutputPath)

$fileStream = [System.IO.File]::Create($outputFile)
$writer = [System.IO.BinaryWriter]::new($fileStream)

try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$entries.Count)

    $imageOffset = 6 + (16 * $entries.Count)
    foreach ($entry in $entries) {
        $sizeByte = if ($entry.Size -ge 256) { [byte]0 } else { [byte]$entry.Size }
        $writer.Write($sizeByte)
        $writer.Write($sizeByte)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$entry.Bytes.Length)
        $writer.Write([uint32]$imageOffset)
        $imageOffset += $entry.Bytes.Length
    }

    foreach ($entry in $entries) {
        $writer.Write([byte[]]$entry.Bytes)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}

Get-Item $outputFile | Select-Object FullName, Length, LastWriteTime
