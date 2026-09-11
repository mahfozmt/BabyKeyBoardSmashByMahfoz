# বেবিস্ম্যাশ (BabySmash-BN) — Bangla Keyboard-Smash App for Windows

Implementation plan for a from-scratch Windows desktop app inspired by Scott Hanselman's [BabySmash](https://github.com/shanselman/babysmash), localized in **Bangla**:
- Bangla numerals (`1, 2, 3` → `১, ২, ৩`) and alphabet characters.
- Open-source Bangla fonts (**Baloo Da 2** & **Noto Sans Bengali**).
- Open-source emoji for shapes, animals, fruits, and toys.
- Audio engine: Low-latency toy sound effects (pops, boings, chimes, animals) + spoken Bangla pronunciations.
- Complete toddler-proofing: Blocks Windows key, Alt+Tab, and Sticky Keys popups so child smashing never disturbs other apps or Windows settings.
- Easily closable by adults (`Alt+F4` or holding `Escape`).

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            MainWindow (WPF View)                            │
│  - Borderless, Topmost, WindowState=Maximized                               │
│  - Covers full screen (single or all monitors)                              │
│  - Fullscreen Canvas for bursts, particles, typography, and emoji           │
│  - Cursor follower (smiling face / sparkle trail)                           │
└───────────────┬─────────────────────────────────────────────┬───────────────┘
                │ Raw Window Events                           │ Mouse / Touch
                ▼                                             ▼
┌───────────────────────────────┐              ┌──────────────────────────────┐
│  KeyboardHookService          │              │  MouseInteractionService     │
│  - Intercepts WinKey, Alt+Tab │              │  - Cursor trailing particle  │
│  - Disables StickyKeys / SPI  │              │  - Click/Touch spawns bursts │
│  - Detects Adult Exit combo   │              └──────────────┬───────────────┘
└───────────────┬───────────────┘                             │
                │ Validated Toddler Key                       │
                ▼                                             │
┌───────────────────────────────┐                             │
│  KeyMappingService            │                             │
│  - Digits: 0-9 -> ০-৯         │                             │
│  - Alpha: A-Z -> Bangla glyph │                             │
│  - Special: Space/Enter bursts│                             │
└───────────────┬───────────────┘                             │
                │                                             │
                ▼                                             │
    ┌───────────────────────┐                                 │
    │     SmashContent      │◄────────────────────────────────┘
    │ - BanglaGlyph         │
    │ - EmojiAssetPath      │
    │ - PaletteColor        │
    │ - SfxPath             │
    │ - VoiceoverPath       │
    └───────────┬───────────┘
                │
        ┌───────┴───────────────────────────┐
        ▼                                   ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│       RenderService       │   │       AudioService        │
│ - Spawns animated burst   │   │ - NAudio In-Memory Mixer  │
│ - Scale + Wobble + Fade   │   │ - Instant SFX playback    │
│ - Particle stars/circles  │   │ - Bangla Speech Voiceover │
│ - Max 15-item throttle    │   │ - Polyphonic concurrency  │
└───────────────────────────┘   └───────────────────────────┘
```

Single-process WPF app. Fully offline, no telemetry, no internet required at runtime. All fonts, emoji, and sounds are bundled locally as app resources.

---

## 2. Technology & Library Stack

| Concern | Choice | Why |
|---|---|---|
| **Platform / UI** | **.NET 8 / 10 + WPF (C# 12)** | Windows-only desktop app. WPF provides hardware-accelerated vector/font rendering, storyboard animations, DPI awareness, and seamless fullscreen window management. |
| **Audio Engine** | **NAudio** (`v2.2+` via NuGet) | Low-latency audio playback. Toddlers smash keys rapidly; standard `MediaPlayer` or `SoundPlayer` cannot handle polyphonic overlapping sounds without stuttering or crashing. NAudio's in-memory sample mixer handles dozens of simultaneous sounds cleanly. |
| **Toddler-Proofing** | **Win32 Low-Level Hook (`WH_KEYBOARD_LL`)** | Necessary to intercept the Windows Key (`VK_LWIN`, `VK_RWIN`), Application key, and system task shortcuts before the OS handles them. |
| **Accessibility Lock** | **`SystemParametersInfo` (Win32 API)** | Temporarily disables Sticky Keys (5x Shift) and Filter Keys (8s Shift hold) while the app is active, and restores them on close. |
| **Fonts** | **Baloo Da 2** (Primary) & **Noto Sans Bengali** (Fallback) | Google Fonts (SIL Open Font License 1.1). Baloo Da 2 has rounded, friendly curves specifically designed for high legibility for young children. |
| **Emoji / Shapes** | **Google Noto Emoji** / **Microsoft Fluent Emoji** (PNG, 512x512) | Apache 2.0 / MIT. Safe to bundle, vibrant 3D and flat styles covering animals, food, shapes, and toys. |
| **Bangla Voiceover** | **Edge-TTS / Wikimedia Commons** | Synthesized studio-quality Bengali voice clips (`bn-BD-NabanitaNeural`) for all numerals (`০-৯`) and letters. |
| **Packaging** | `dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained` | Produces a single, portable `.exe` file that runs on any Windows 10/11 machine without requiring .NET runtime installation. |
| **Testing** | **xUnit** | Unit tests for key mapping, data models, and JSON configuration. |

---

## 3. Toddler-Proofing ("Don't Disturb Other Apps or Settings")

When a toddler smashes the keyboard with their hands:
1. **Windows Key**: Slapping the bottom-left corner hits the Windows key, opening the Start menu and stealing window focus.
2. **Sticky Keys / Filter Keys**: Tapping Shift 5 times triggers the Windows accessibility popup. Holding Shift for 8 seconds triggers Filter Keys.
3. **Task Switching**: Smashing `Alt+Tab`, `Win+D`, `Win+M`, or `Ctrl+Esc` minimizes windows or reveals other running apps.

### 3.1 Low-Level Keyboard Interception (`WH_KEYBOARD_LL`)
A global low-level keyboard hook intercepts key events before Windows translates them:
- **Swallowed keys**:
  - `VK_LWIN` & `VK_RWIN` (Windows keys)
  - `VK_APPS` (Context Menu key)
  - `Alt + Tab`, `Alt + Esc`, `Ctrl + Esc`
  - `Win + D`, `Win + M`, `Win + L` (where interceptable)
- **Allowed Keys**:
  - `Alt + F4`: Allowed through so parents can exit instantly.
  - Normal alphanumeric keys, Space, Enter, Numpad keys are passed directly to `KeyMapService`.

### 3.2 Sticky Keys & Filter Keys Bypass
On app start, query and temporarily disable the accessibility shortcut triggers using `SystemParametersInfo`, restoring them cleanly on exit:
```csharp
[DllImport("user32.dll", SetLastError = true)]
static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref STICKYKEYS pvParam, uint fWinIni);

// Disable SKF_HOTKEYACTIVE during app lifetime
```

### 3.3 Safe Adult Exit Mechanisms
1. **Primary**: `Alt + F4` (instant close).
2. **Hold Escape for 2 Seconds**: An on-screen circular progress bar fills up ("Closing in 2... 1..."), preventing toddlers from exiting on accidental single Escape hits.
3. **Parent Settings Corner**: A semi-transparent lock icon in the top corner that requires a double-click to exit or adjust volume.

---

## 4. Bangla Key-Mapping Logic

### 4.1 Digits (0–9 & Numpad)
All physical number keys map to Bengali numerals:

| Key | Bangla Numeral | Pronunciation |
|---|---|---|
| `0` / `NumPad0` | **০** | Shunno (শূন্য) |
| `1` / `NumPad1` | **১** | Ek (এক) |
| `2` / `NumPad2` | **২** | Dui (দুই) |
| `3` / `NumPad3` | **৩** | Teen (তিন) |
| `4` / `NumPad4` | **৪** | Char (চার) |
| `5` / `NumPad5` | **৫** | Pach (পাঁচ) |
| `6` / `NumPad6` | **৬** | Chhoy (ছয়) |
| `7` / `NumPad7` | **৭** | Shaat (সাত) |
| `8` / `NumPad8` | **৮** | Aat (আট) |
| `9` / `NumPad9` | **৯** | Noy (নয়) |

### 4.2 Letters (A–Z) — Phonetic & Child-Friendly
Physical keys `A–Z` map to prominent Bengali characters intuitively:

| Key | Bangla | Word Association (Kid-Friendly) |
|---|---|---|
| `A` | **অ** | অজগর / আম |
| `B` | **ব** | বই / বাঘ |
| `C` | **চ** | চাঁদ / চশমা |
| `D` | **দ** | দোয়েল / ডালিম |
| `E` | **এ** | একতারা |
| `F` | **ফ** | ফুল / ফল |
| `G` | **গ** | গোলাপ / গাড়ি |
| `H` | **হ** | হাতি |
| `I` | **ই** | ইলিশ |
| `J` | **জ** | জাহাজ / জাম |
| `K` | **ক** | কলা / কাক |
| `L` | **ল** | লিচু / লাল |
| `M` | **ম** | মাছ / ময়ূর |
| `N` | **ন** | নৌকা / নদী |
| `O` | **ও** | ওলকপি |
| `P` | **প** | পাখি / প্রজাপতি |
| `Q` | **ক্ব / ৎ** | কৈতব |
| `R` | **র** | রংধনু (Rainbow) |
| `S` | **শ** | সিংহ / সূর্য |
| `T` | **ট** | টিয়া / তারা |
| `U` | **উ** | উট |
| `V` | **ভ** | ভালুক |
| `W` | **ঐ** | ঐরাবত |
| `X` | **ক্ষ** | ক্ষীর |
| `Y` | **য়** | ময়না |
| `Z` | **ঝ** | ঝিনুক |

*Configured via `Assets/keymap.json` so keys can be customized without recompiling.*

### 4.3 Special Keys
- **Spacebar**: Spawns a full-screen Celebration Firework / Confetti burst with encouraging Bangla phrases (`"দারুণ!"`, `"সাবাশ!"`, `"বাহ্!"`).
- **Enter**: Rainbow ripple wave across the canvas.
- **Backspace / Delete**: Playful bubble popping vacuum effect.
- **Arrow Keys**: Bouncing animated animal running in the pressed direction.

---

## 5. Visuals & UI Animation Engine

### 5.1 Color Palette
Bright, high-contrast, cheerful colors:
- Blue (`#2980B9`), Gold Yellow (`#F1C40F`), Pink (`#FF6B81`), Emerald Green (`#2ECC71`), Orange (`#FF793F`), Purple (`#9B59B6`), Crimson (`#FF4757`), Turquoise (`#00D2D3`).
- Background: Soft dark slate (`#1E1E2E`) or warm cream, keeping colors glowing without blinding glare.

### 5.2 Animated Burst Lifecycle
1. **Spawn**: Instantiated at a random position (or mouse position) on the fullscreen `Canvas`.
2. **Components**:
   - Giant Bangla glyph (180pt–240pt) in `Baloo Da 2`.
   - Curated emoji (180x180 PNG).
   - 8–14 colorful radiating particle sparkles.
3. **Storyboard Animation**:
   - **0.00s – 0.15s**: Spring pop from `0.2` to `1.15` scale with slight random rotation (`-12°` to `+12°`).
   - **0.15s – 0.30s**: Settle back to `1.0`.
   - **0.30s – 2.00s**: Gentle floating drift (`TranslateTransform`).
   - **2.00s – 3.00s**: Smooth opacity fade to `0.0`.
   - **Completed**: Cleanly detached from `Canvas.Children` to avoid memory accumulation.
4. **Throttling**: Limit maximum active on-screen items to 15. If more keys are pressed, fast-fade the oldest item.

### 5.3 Mouse & Touch Tracking
- Cursor follower: Cute smiling emoji or star tracking the mouse pointer.
- Cursor trail: Fading colorful bubbles / sparkles left behind mouse movement.
- Clicks or touchscreen taps spawn smash bursts directly under the finger.

---

## 6. Audio Architecture & Collection Pipeline

### 6.1 Audio Assets Needed
1. **Toy SFX**: 25–30 CC0 sound clips:
   - Pops, bubbles, boings, spring bounces.
   - Xylophone / chime notes (C, D, E, F, G, A, B).
   - Animal sounds (cat, dog, cow, duck, sheep, bird).
   - Baby chuckles, bells, squeaks.
2. **Bangla Speech Audio**:
   - Spoken Bengali digits: `"শূন্য"`, `"এক"`, `"দুই"`, `"তিন"`, `"চার"`, `"পাঁচ"`, `"ছয়"`, `"সাত"`, `"আট"`, `"নয়"`.
   - Spoken Bengali letters: `"অ"`, `"আ"`, `"ই"`, `"ক"`, `"খ"`, etc.
   - Reward phrases: `"দারুণ!"`, `"সাবাশ!"`, `"বাহ্!"`.

### 6.2 Asset Acquisition Pipeline (Automated Scripts)
- **`scripts/download_assets.ps1`**:
  - Downloads **Baloo Da 2** from Google Fonts.
  - Downloads curated **Kenney Audio** packs (CC0 - Digital Audio, Interface Sounds).
  - Downloads curated **Google Noto Emoji** PNGs (Animals, Shapes, Toys, Fruits).
- **`scripts/generate_bangla_audio.ps1`**:
  - Uses `edge-tts` (voice: `bn-BD-NabanitaNeural`) to synthesize crisp, studio-grade speech for all numerals and letters directly into `Assets/Sounds/Voice/`.

### 6.3 Low-Latency NAudio Playback
- Audio files are pre-decoded into RAM (`CachedSound` float buffers).
- Keypress feeds audio directly to `MixingSampleProvider`.
- Multiple sounds play simultaneously without stutter, latency, or thread blocking.

---

## 7. Project Structure

```
KeyBoardSmashByMahfoz/
├─ scripts/
│  ├─ download_assets.ps1          # Downloads fonts, CC0 sounds, and emoji icons
│  └─ generate_bangla_audio.ps1    # Synthesizes studio-quality Bangla voice clips
├─ src/
│  └─ BabySmashBN/
│     ├─ BabySmashBN.csproj        # .NET 8 WPF Project
│     ├─ App.xaml / App.xaml.cs
│     ├─ MainWindow.xaml           # Fullscreen Canvas, Topmost
│     ├─ MainWindow.xaml.cs        # Orchestration & lifecycle
│     ├─ Interop/
│     │  ├─ Win32Hooks.cs          # WH_KEYBOARD_LL low-level keyboard hook
│     │  └─ AccessibilityHelper.cs # Disables StickyKeys & FilterKeys safely
│     ├─ Services/
│     │  ├─ IKeyMapService.cs      # Key -> Bangla Glyph & Word contract
│     │  ├─ KeyMapService.cs       # JSON-driven key resolver
│     │  ├─ IAudioService.cs       # Audio playback contract
│     │  ├─ AudioService.cs        # Low-latency NAudio mixer
│     │  ├─ IRenderService.cs      # Canvas spawning & animations
│     │  ├─ RenderService.cs       # Storyboards, particles, emoji loader
│     │  └─ MouseTrailService.cs   # Cursor sparkles & follower face
│     ├─ Models/
│     │  ├─ SmashContent.cs        # (Glyph, Emoji, Color, Sfx, Voice)
│     │  └─ AppConfig.cs           # Options (SoundMode, MonitorMode, ExitMode)
│     └─ Assets/
│        ├─ keymap.json            # Bangla letter & number mappings
│        ├─ Fonts/                 # Baloo Da 2 (OFL) & Noto Sans Bengali
│        ├─ Emoji/                 # 40-50 curated 512x512 PNGs (Animals, Shapes, Toys)
│        └─ Sounds/
│           ├─ Sfx/                # CC0 pops, boings, xylophone, animals
│           └─ Voice/              # Bengali speech for numerals and letters
├─ tests/
│  └─ BabySmashBN.Tests/           # Unit tests for KeyMapService & Config
├─ PLAN.md                         # This file
└─ ASSETS_LICENSES.md              # Attribution & licenses (OFL, CC0, Apache 2.0)
```

---

## 8. Step-by-Step Implementation Milestones

1. **M1 — Project Scaffolding & Asset Acquisition**:
   - Initialize `.NET 8` WPF project with NAudio dependency.
   - Run download scripts to collect fonts, CC0 sound effects, emoji, and synthesize Bangla voice files.
   - Create `Assets/keymap.json` and `ASSETS_LICENSES.md`.

2. **M2 — Toddler-Proofing Window & Low-Level Hooks**:
   - Implement borderless topmost fullscreen window.
   - Implement `WH_KEYBOARD_LL` to swallow Windows Key, Alt+Tab, and application keys.
   - Disable StickyKeys / FilterKeys via `SystemParametersInfo`.
   - Verify `Alt + F4` closes the app and restores OS settings.

3. **M3 — Bangla Typography & Key Mapping**:
   - Embed `Baloo Da 2` font.
   - Implement `KeyMapService` translating `0–9` to `০–৯` and `A–Z` to Bengali letters.
   - Add xUnit unit tests verifying all keys map cleanly.

4. **M4 — Visual Burst & Particle Animation Engine**:
   - Build `RenderService` spawning Bangla character + emoji + particle sparkles on `Canvas`.
   - Add elastic scale, drift, and fade-out Storyboards.
   - Implement 15-item throttle to prevent UI lag during rapid key mashing.

5. **M5 — Polyphonic Low-Latency Audio Engine**:
   - Build `AudioService` with NAudio in-memory sample mixer.
   - Test simultaneous rapid key mashing (play SFX + spoken Bangla pronunciation).

6. **M6 — Mouse & Touch Interaction**:
   - Add cursor-following smiling face and sparkle trail.
   - Support mouse clicks and touchscreen taps generating bursts.

7. **M7 — Verification, Polish & Single-File Packaging**:
   - Stress test keyboard mashing.
   - Verify zero leakage of Windows key or StickyKeys.
   - Publish standalone single-file binary: `dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained`.
