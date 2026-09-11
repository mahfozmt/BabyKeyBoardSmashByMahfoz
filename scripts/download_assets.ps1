# Asset download and generation script for BabySmashBN
$ErrorActionPreference = "Stop"

$baseDir = Split-Path -Parent $PSScriptRoot
$assetsDir = Join-Path $baseDir "src\BabySmashBN\Assets"
$fontsDir = Join-Path $assetsDir "Fonts"
$emojiDir = Join-Path $assetsDir "Emoji"
$sfxDir = Join-Path $assetsDir "Sounds\Sfx"
$voiceDir = Join-Path $assetsDir "Sounds\Voice"

foreach ($dir in @($fontsDir, $emojiDir, $sfxDir, $voiceDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
}

Write-Host "1. Downloading Open-Source Bangla Fonts (OFL)..."
$fonts = @(
    @{
        Url = "https://raw.githubusercontent.com/google/fonts/main/ofl/balooda2/BalooDa2%5Bwght%5D.ttf"
        Out = Join-Path $fontsDir "BalooDa2.ttf"
    },
    @{
        Url = "https://raw.githubusercontent.com/google/fonts/main/ofl/notosansbengali/NotoSansBengali%5Bwdth%2Cwght%5D.ttf"
        Out = Join-Path $fontsDir "NotoSansBengali.ttf"
    }
)

foreach ($f in $fonts) {
    if (-not (Test-Path $f.Out)) {
        Write-Host "  Downloading $($f.Out | Split-Path -Leaf)..."
        Invoke-WebRequest -Uri $f.Url -OutFile $f.Out -Headers @{ "User-Agent" = "PowerShell" }
    } else {
        Write-Host "  $($f.Out | Split-Path -Leaf) already exists."
    }
}

Write-Host "`n2. Downloading Curated Open-Source Emoji (Google Noto Emoji - Apache 2.0)..."
$emojiMap = @{
    "dog.png"         = "emoji_u1f436.png"
    "cat.png"         = "emoji_u1f431.png"
    "lion.png"        = "emoji_u1f981.png"
    "tiger.png"       = "emoji_u1f42f.png"
    "rabbit.png"      = "emoji_u1f430.png"
    "bear.png"        = "emoji_u1f43b.png"
    "panda.png"       = "emoji_u1f43c.png"
    "cow.png"         = "emoji_u1f42e.png"
    "pig.png"         = "emoji_u1f437.png"
    "frog.png"        = "emoji_u1f438.png"
    "monkey.png"      = "emoji_u1f435.png"
    "chicken.png"     = "emoji_u1f414.png"
    "penguin.png"     = "emoji_u1f427.png"
    "duck.png"        = "emoji_u1f986.png"
    "butterfly.png"   = "emoji_u1f98b.png"
    "turtle.png"      = "emoji_u1f422.png"
    "dolphin.png"     = "emoji_u1f42c.png"
    "elephant.png"    = "emoji_u1f418.png"
    "apple.png"       = "emoji_u1f34e.png"
    "banana.png"      = "emoji_u1f34c.png"
    "watermelon.png"  = "emoji_u1f349.png"
    "strawberry.png"  = "emoji_u1f353.png"
    "star.png"        = "emoji_u2b50.png"
    "glowing_star.png"= "emoji_u1f31f.png"
    "heart.png"       = "emoji_u1f496.png"
    "balloon.png"     = "emoji_u1f388.png"
    "car.png"         = "emoji_u1f697.png"
    "rocket.png"      = "emoji_u1f680.png"
    "airplane.png"    = "emoji_u2708.png"
    "soccer.png"      = "emoji_u26bd.png"
    "rainbow.png"     = "emoji_u1f308.png"
    "icecream.png"    = "emoji_u1f366.png"
    "sun.png"         = "emoji_u2600.png"
    "flower.png"      = "emoji_u1f338.png"
    "fish.png"        = "emoji_u1f41f.png"
    "bell.png"        = "emoji_u1f514.png"
    "guitar.png"      = "emoji_u1f3b8.png"
    "crown.png"       = "emoji_u1f451.png"
    "sparkles.png"    = "emoji_u2728.png"
    "smile.png"       = "emoji_u1f604.png"
}

$notoBase = "https://raw.githubusercontent.com/googlefonts/noto-emoji/main/png/128/"
foreach ($item in $emojiMap.GetEnumerator()) {
    $dest = Join-Path $emojiDir $item.Key
    if (-not (Test-Path $dest)) {
        $sourceUrl = $notoBase + $item.Value
        try {
            Invoke-WebRequest -Uri $sourceUrl -OutFile $dest -Headers @{ "User-Agent" = "PowerShell" }
            Write-Host "  Downloaded $($item.Key)"
        } catch {
            Write-Warning "  Failed downloading $($item.Key) from $sourceUrl"
        }
    }
}

Write-Host "`nAsset download completed successfully."
