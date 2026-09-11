# Generates procedural SFX WAVs and downloads native Bangla speech audio
$ErrorActionPreference = "Stop"

$baseDir = Split-Path -Parent $PSScriptRoot
$assetsDir = Join-Path $baseDir "src\BabySmashBN\Assets"
$sfxDir = Join-Path $assetsDir "Sounds\Sfx"
$voiceDir = Join-Path $assetsDir "Sounds\Voice"

foreach ($dir in @($sfxDir, $voiceDir)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
}

function New-WavFile {
    param(
        [string]$Path,
        [int]$SampleRate = 44100,
        [scriptblock]$Generator,
        [double]$DurationSeconds
    )
    $totalSamples = [int]($SampleRate * $DurationSeconds)
    $dataSize = $totalSamples * 2
    $stream = [System.IO.File]::Create($Path)
    $writer = New-Object System.IO.BinaryWriter($stream)
    
    # RIFF header
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
    $writer.Write([int]($dataSize + 36))
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
    
    # fmt chunk
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
    $writer.Write([int]16)
    $writer.Write([short]1) # PCM
    $writer.Write([short]1) # Mono
    $writer.Write([int]$SampleRate)
    $writer.Write([int]($SampleRate * 2))
    $writer.Write([short]2)
    $writer.Write([short]16)
    
    # data chunk
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
    $writer.Write([int]$dataSize)
    
    for ($i = 0; $i -lt $totalSamples; $i++) {
        $t = $i / [double]$SampleRate
        $val = & $Generator $t $DurationSeconds
        if ($val -gt 1.0) { $val = 1.0 }
        elseif ($val -lt -1.0) { $val = -1.0 }
        $shortVal = [short]($val * 32767.0)
        $writer.Write($shortVal)
    }
    
    $writer.Close()
    $stream.Close()
}

Write-Host "1. Generating Procedural Toy Sound Effects (WAV)..."

# Pop sound (bubble pop)
New-WavFile -Path (Join-Path $sfxDir "pop.wav") -DurationSeconds 0.12 -Generator {
    param($t, $d)
    $freq = 700.0 * [Math]::Exp(-$t * 35.0) + 120.0
    $phase = 2.0 * [Math]::PI * $freq * $t
    $env = [Math]::Exp(-$t * 28.0)
    return [Math]::Sin($phase) * $env
}

# Boing sound (cartoon spring)
New-WavFile -Path (Join-Path $sfxDir "boing.wav") -DurationSeconds 0.45 -Generator {
    param($t, $d)
    $vibrato = [Math]::Sin(2.0 * [Math]::PI * 18.0 * $t) * 45.0
    $baseFreq = 220.0 + ($t * 300.0) + $vibrato
    $phase = 2.0 * [Math]::PI * $baseFreq * $t
    $env = [Math]::Pow(1.0 - ($t / $d), 1.5)
    return [Math]::Sin($phase) * $env * 0.85
}

# Chime sound (sparkling chime)
New-WavFile -Path (Join-Path $sfxDir "chime.wav") -DurationSeconds 0.65 -Generator {
    param($t, $d)
    $env = [Math]::Exp(-$t * 6.5)
    $w1 = [Math]::Sin(2.0 * [Math]::PI * 1046.5 * $t) # C6
    $w2 = [Math]::Sin(2.0 * [Math]::PI * 1318.5 * $t) # E6
    $w3 = [Math]::Sin(2.0 * [Math]::PI * 1567.98 * $t) # G6
    $w4 = [Math]::Sin(2.0 * [Math]::PI * 2093.0 * $t) # C7
    return ($w1 * 0.4 + $w2 * 0.3 + $w3 * 0.2 + $w4 * 0.1) * $env
}

# Bell sound (resonant bell)
New-WavFile -Path (Join-Path $sfxDir "bell.wav") -DurationSeconds 0.7 -Generator {
    param($t, $d)
    $env = [Math]::Exp(-$t * 5.0)
    $w1 = [Math]::Sin(2.0 * [Math]::PI * 880.0 * $t)
    $w2 = [Math]::Sin(2.0 * [Math]::PI * 1760.0 * $t) * 0.4
    $w3 = [Math]::Sin(2.0 * [Math]::PI * 2640.0 * $t) * 0.2
    return ($w1 + $w2 + $w3) * $env * 0.7
}

# Xylophone notes (C4, D4, E4, F4, G4, A4, B4, C5)
$xyloNotes = @{
    "xylo_c1.wav" = 261.63
    "xylo_d.wav"  = 293.66
    "xylo_e.wav"  = 329.63
    "xylo_f.wav"  = 349.23
    "xylo_g.wav"  = 392.00
    "xylo_a.wav"  = 440.00
    "xylo_b.wav"  = 493.88
    "xylo_c2.wav" = 523.25
}

foreach ($note in $xyloNotes.GetEnumerator()) {
    $filePath = Join-Path $sfxDir $note.Key
    $pitch = $note.Value
    New-WavFile -Path $filePath -DurationSeconds 0.5 -Generator {
        param($t, $d)
        $env = [Math]::Exp(-$t * 8.0)
        $fundamental = [Math]::Sin(2.0 * [Math]::PI * $pitch * $t)
        $overtone = [Math]::Sin(2.0 * [Math]::PI * ($pitch * 3.0) * $t) * 0.25
        return ($fundamental + $overtone) * $env * 0.8
    }
}

Write-Host "  Procedural SFX generation completed."

Write-Host "`n2. Downloading Native Bangla Voice Clips (Voiceovers)..."

$voiceItems = @{
    # Numerals 0-9
    "num_0.mp3" = "শূন্য"
    "num_1.mp3" = "এক"
    "num_2.mp3" = "দুই"
    "num_3.mp3" = "তিন"
    "num_4.mp3" = "চার"
    "num_5.mp3" = "পাঁচ"
    "num_6.mp3" = "ছয়"
    "num_7.mp3" = "সাত"
    "num_8.mp3" = "আট"
    "num_9.mp3" = "নয়"

    # Alphabet A-Z (Phonetic Bangla Letters)
    "let_a.mp3" = "অ"
    "let_b.mp3" = "ব"
    "let_c.mp3" = "চ"
    "let_d.mp3" = "দ"
    "let_e.mp3" = "এ"
    "let_f.mp3" = "ফ"
    "let_g.mp3" = "গ"
    "let_h.mp3" = "হ"
    "let_i.mp3" = "ই"
    "let_j.mp3" = "জ"
    "let_k.mp3" = "ক"
    "let_l.mp3" = "ল"
    "let_m.mp3" = "ম"
    "let_n.mp3" = "ন"
    "let_o.mp3" = "ও"
    "let_p.mp3" = "প"
    "let_q.mp3" = "ৎ"
    "let_r.mp3" = "র"
    "let_s.mp3" = "শ"
    "let_t.mp3" = "ট"
    "let_u.mp3" = "উ"
    "let_v.mp3" = "ভ"
    "let_w.mp3" = "ঐ"
    "let_x.mp3" = "ক্ষ"
    "let_y.mp3" = "য়"
    "let_z.mp3" = "ঝ"

    # Celebratory Voice Clips
    "cheer_1.mp3" = "দারুণ"
    "cheer_2.mp3" = "সাবাশ"
    "cheer_3.mp3" = "বাহ্"
}

$wc = New-Object System.Net.WebClient
$wc.Headers.Add("User-Agent", "Mozilla/5.0")

foreach ($item in $voiceItems.GetEnumerator()) {
    $dest = Join-Path $voiceDir $item.Key
    if (-not (Test-Path $dest)) {
        $encoded = [System.Web.HttpUtility]::UrlEncode($item.Value, [System.Text.Encoding]::UTF8)
        if (-not $encoded) {
            # Fallback URL encoding for pure powershell
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($item.Value)
            $encoded = ($bytes | ForEach-Object { "%{0:X2}" -f $_ }) -join ""
        }
        $ttsUrl = "https://translate.google.com/translate_tts?ie=UTF-8&q=$encoded&tl=bn&client=tw-ob"
        try {
            $wc.DownloadFile($ttsUrl, $dest)
            Write-Host "  Voice saved: $($item.Key) ($($item.Value))"
            Start-Sleep -Milliseconds 80 # Avoid rate-limiting
        } catch {
            Write-Warning "  Could not download voice for $($item.Key): $($_.Exception.Message)"
        }
    }
}

Write-Host "`nAudio generation and collection completed successfully."
