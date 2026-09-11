# বেবিস্ম্যাশ (BabySmash-BN)

A fullscreen, toddler-proof, **Bangla-language** keyboard-smash toy for Windows — inspired by
Scott Hanselman's [BabySmash](https://github.com/shanselman/babysmash), rebuilt from scratch so
every number, letter, and spoken word your kid sees and hears is in Bangla.

Let a toddler smash the keyboard: each keypress fills the screen with a giant animated Bangla
digit or letter, a colorful open-source emoji, a sound effect, and a spoken Bangla pronunciation
— while the app blocks the Windows key, Alt+Tab, and Sticky/Filter Keys popups so nothing outside
the app gets disturbed. An adult can always exit with **Alt+F4**.

## Features

- Fullscreen, borderless, always-on-top window — covers the whole screen, nothing else is reachable.
- Digits `0–9` → Bengali numerals `০–৯`, letters `A–Z` → Bengali বর্ণমালা characters, each with a
  kid-friendly word association.
- Random colorful open-source emoji (animals, fruit, shapes, toys) with spring/fade animations,
  max on-screen bursts throttled so rapid mashing never lags the UI.
- Sound effects (pops, chimes, animal sounds) plus spoken Bangla voice pronunciations, played
  polyphonically (rapid keypresses don't cut each other off).
- Toddler-proofing: a low-level keyboard hook swallows the Windows key, Alt+Tab, and Ctrl+Esc, and
  Sticky Keys / Filter Keys accessibility popups are temporarily disabled while the app runs (and
  restored on exit).
- Mouse/touch support: cursor-follower and click/tap bursts.
- 100% offline. No telemetry, no network calls at runtime.
- Fully open-source — code, fonts, emoji, and audio assets are all permissively licensed. See
  [ASSETS_LICENSES.md](ASSETS_LICENSES.md) for exact sources/licenses of every bundled asset.

## Requirements

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (to build/run from source)
- PowerShell (for the asset-collection scripts, first-time setup only)

## Getting started

```powershell
git clone <this-repo-url>
cd KeyBoardSmashByMahfoz

# First time only: download fonts/emoji and generate audio assets
./scripts/download_assets.ps1
./scripts/generate_audio.ps1

# Build and run
dotnet run --project src/BabySmashBN
```

To build a standalone portable `.exe` (no .NET runtime install required on the target PC):

```powershell
dotnet publish src/BabySmashBN -r win-x64 -p:PublishSingleFile=true --self-contained
```

## Project structure

```
KeyBoardSmashByMahfoz/
├─ src/
│  ├─ BabySmashBN.Core/     # Key-mapping, models, Win32 interop (class library)
│  └─ BabySmashBN/          # WPF app: window, rendering, audio, assets
├─ tests/
│  └─ BabySmashBN.Tests/    # xUnit tests
├─ tools/
│  └─ AssetGenerator/       # Helper tool for generating/curating assets
├─ scripts/                 # Asset download/generation scripts (fonts, emoji, audio)
├─ PLAN.md                  # Full architecture & implementation plan
├─ ASSETS_LICENSES.md       # License/attribution for every bundled font/emoji/sound
└─ LICENSE                  # MIT license (code only — see ASSETS_LICENSES.md for assets)
```

See [PLAN.md](PLAN.md) for the full architecture, library stack, and milestone breakdown.

## Exiting the app

Press **Alt+F4** at any time — this is always passed through even while the toddler-proofing
keyboard hook is active.

## Contributing

Issues and pull requests welcome — this started as a personal project (a niece who loves smashing
keyboards) but is shared in case it's useful for other Bangla-speaking families. Keep any new
bundled assets (fonts/emoji/sounds) permissively licensed and add them to
`ASSETS_LICENSES.md`.

## License

Code is [MIT licensed](LICENSE). Bundled fonts, emoji, and audio each carry their own open-source
licenses — see [ASSETS_LICENSES.md](ASSETS_LICENSES.md) before reusing assets elsewhere.
