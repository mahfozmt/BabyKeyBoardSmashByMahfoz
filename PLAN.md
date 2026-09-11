# বেবিস্ম্যাশ (BabySmash-BN) — Bangla Keyboard-Smash App for Windows

Implementation plan for a from-scratch Windows desktop app, inspired by Scott Hanselman's
[BabySmash](https://github.com/shanselman/babysmash), but fully in Bangla: Bangla digits/letters,
open-source Bangla font, open-source emoji as shapes, and collected sound effects.

Goal: a toddler mashes the keyboard → fullscreen app shows a big colorful Bangla character/emoji
with a sound, and nothing else on the PC is disturbed. Closable by an adult.

---

## 1. High-level architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        MainWindow (WPF)                     │
│  - Borderless, Topmost, WindowState=Maximized, no chrome     │
│  - Covers full screen (single or all monitors)               │
│  - Focus trap: grabs keyboard/mouse input while open         │
└───────────────┬───────────────────────────────┬─────────────┘
                │ KeyDown / KeyUp events         │ MouseMove/Click
                ▼                                ▼
      ┌───────────────────┐            ┌───────────────────┐
      │  KeyMapService     │            │  MouseFxService    │
      │  key -> content    │            │  cursor trail/glow │
      └─────────┬──────────┘            └─────────┬──────────┘
                │ SmashContent (glyph, color, emoji, sound)
                ▼
      ┌───────────────────────────────────────────────┐
      │              RenderService                     │
      │  spawns a "burst" visual on Canvas:            │
      │   - Bangla glyph/number (Noto Sans Bengali)     │
      │   - random open-source emoji shape (image)      │
      │   - random color, random position/rotation      │
      │   - scale/fade animation (Storyboard)            │
      └─────────┬───────────────────────────────────────┘
                │
                ▼
      ┌───────────────────┐
      │   AudioService     │  (NAudio) plays a random sound
      │   per key/category │  clip from Assets/Sounds
      └───────────────────┘
```

Single-process WPF app. No network calls, no telemetry. Everything (font, emoji, sounds) is
bundled locally as app resources so it works fully offline.

---

## 2. Technology stack

| Concern              | Choice                                   | Why |
|-----------------------|-------------------------------------------|-----|
| Platform/UI framework | **.NET 8 + WPF (C#)**                     | Windows-only is fine (explicitly requested), WPF gives easy fullscreen/topmost windows, XAML animations, DPI-aware rendering, and mature keyboard/mouse event model. Simpler than WinUI3/MAUI for this scope. |
| Language              | C# 12                                     | Standard for WPF, good tooling. |
| Audio playback        | **NAudio** (NuGet)                        | Supports overlapping/rapid sound playback (important — toddler mashes fast), WAV/MP3, low latency. `MediaPlayer`/`SoundPlayer` (built-in) can't overlap sounds well. |
| Fonts                 | Embedded font resource (WPF `FontFamily` from `pack://` URI) | No install needed, font ships inside app folder. |
| Images/emoji          | PNG or SVG assets bundled under `Assets/Emoji`, rendered via `Image` control (convert SVG→PNG at build time if needed, WPF has no native SVG support) |
| Packaging             | `dotnet publish -r win-x64 --self-contained` (single-file exe) | Easiest to hand to a Windows PC, no installer required for personal use. MSIX optional later. |
| Testing               | xUnit for `KeyMapService` mapping logic  | Pure logic, easy to unit test without UI. |

No game engine (Unity/Godot) needed — this is 2D UI + sound, WPF is sufficient and much lighter.

---

## 3. Project structure

```
KeyBoardSmashByMahfoz/
├─ src/
│  └─ BabySmashBN/
│     ├─ BabySmashBN.csproj
│     ├─ App.xaml / App.xaml.cs
│     ├─ MainWindow.xaml / MainWindow.xaml.cs
│     ├─ Services/
│     │  ├─ KeyMapService.cs        (key -> Bangla glyph/number, color)
│     │  ├─ RenderService.cs        (spawns/animates shapes on canvas)
│     │  ├─ AudioService.cs         (NAudio playback, sound pooling)
│     │  └─ MouseFxService.cs       (optional: cursor trail/face)
│     ├─ Models/
│     │  └─ SmashContent.cs         (Glyph, EmojiPath, Color, SoundPath)
│     └─ Assets/
│        ├─ Fonts/NotoSansBengali/  (OFL-licensed font files)
│        ├─ Emoji/                  (open-source emoji PNGs, shapes/animals)
│        └─ Sounds/                 (collected CC0/OFL sound clips)
├─ tests/
│  └─ BabySmashBN.Tests/            (xUnit tests for KeyMapService)
├─ PLAN.md                          (this file)
└─ ASSETS_LICENSES.md               (track source + license per asset — required for CC-BY assets)
```

---

## 4. Bangla key-mapping logic

### 4.1 Digits (straightforward)
Physical number keys `0–9` map directly to Bengali numerals:

| Key | Bangla |
|-----|--------|
| 0 | ০ |
| 1 | ১ |
| 2 | ২ |
| 3 | ৩ |
| 4 | ৪ |
| 5 | ৫ |
| 6 | ৬ |
| 7 | ৭ |
| 8 | ৮ |
| 9 | ৯ |

### 4.2 Letters
Physical keys `A–Z` (26 keys) map to Bangla বর্ণমালা (which has 50+ letters — vowels + consonants).
Since it's a 1:1 static lookup (not phonetic transliteration — simplicity over correctness, this is
for a 3-year-old, not a spelling app), pick 26 representative/common letters, e.g.:

```
A→অ  B→আ  C→ই  D→ঈ  E→উ  F→ঊ  G→এ  H→ঐ  I→ও  J→ঔ
K→ক  L→খ  M→গ  N→ঘ  O→ঙ  P→চ  Q→ছ  R→জ  S→ঝ  T→ঞ
U→ট  V→ঠ  W→ড  X→ঢ  Y→ণ  Z→ত
```
(Exact mapping is a config table — swap any letters later without touching code. Store as a
simple `Dictionary<Key, string>` or a JSON file under `Assets/keymap.json` so it's easy to tweak
without recompiling.)

### 4.3 Shape/emoji per keypress
Independent of the glyph, pick a **random** open-source emoji from a curated pool (stars, animals,
fruit, hearts, circles/squares) each keypress — this is what makes each smash visually varied, same
as original BabySmash's random shapes.

### 4.4 Color
Pick from a fixed palette of ~10 bright, high-contrast colors (avoid pure black/white) at random
per keypress.

### 4.5 Sound
Pick a random short sound clip from the sound pool per keypress (pop/chime/cartoon-boop style),
independent of key — matches original BabySmash behavior (sound = feedback that a key worked, not
tied to which key).

---

## 5. Fullscreen / "don't disturb other apps" behavior

- `WindowStyle="None"`, `WindowState="Maximized"`, `Topmost="True"`, `ResizeMode="NoResize"`.
- Set window bounds to cover the primary screen (or all screens if multi-monitor — use
  `System.Windows.Forms.Screen.AllScreens` to compute bounding rect if "cover everything" is wanted).
- Handle `PreviewKeyDown` at the Window level and mark most keys `e.Handled = true` so they don't
  leak to the OS (prevents things like Windows spell-check popups, IME switching, etc. triggering).
- **Leave `Alt+F4` working** (user's existing, known exit method) — don't suppress `SystemCommand`
  close on Alt+F4. Optionally add a second adult-only exit gesture (e.g. hold `Ctrl+Shift+Esc` for
  2 seconds) in case a toddler's mashing ever manages Alt+F4 accidentally, but this is optional
  polish, not required for MVP.
- **Do NOT** implement a low-level global keyboard hook (`WH_KEYBOARD_LL`) to block `Win` key /
  `Alt+Tab` system-wide unless explicitly asked later — that's an invasive, harder-to-reverse
  feature (affects the whole OS session, not just this app) and out of scope for MVP. Flag as a
  stretch goal only if the toddler discovers Alt+Tab is fun.
- Mouse: optionally also capture `MouseMove`/`MouseDown` on the fullscreen canvas for a cursor-follow
  effect (BabySmash's bouncing smiley face) — nice-to-have, not core.

---

## 6. Asset collection (font, emoji, sounds) — all must be open-source/permissively licensed

Track every asset's source URL + license in `ASSETS_LICENSES.md` as they're added (needed for
attribution compliance, especially with CC-BY/CC-BY-SA sources).

### 6.1 Bangla font (pick one, OFL-licensed = free to bundle/redistribute)
- **Noto Sans Bengali** — https://fonts.google.com/noto/specimen/Noto+Sans+Bengali (SIL Open Font
  License, Google/Noto project on GitHub `notofonts/bengali`). Recommended: broad coverage, very
  legible at large sizes, actively maintained.
- Alternatives: **Hind Siliguri** (OFL, Indian Type Foundry), **Baloo Da 2** (OFL, playful/rounded —
  nice for a kids' app), **Atma** (OFL, informal handwriting style).
- Pick a large, rounded, high-contrast weight (Bold/ExtraBold) for legibility at giant fullscreen size.

### 6.2 Emoji (shapes)
- **Noto Emoji** — https://github.com/googlefonts/noto-emoji (Apache License 2.0 — no attribution
  requirement, safe default). PNG/SVG per emoji.
- Alternative: **OpenMoji** — https://openmoji.org (CC BY-SA 4.0 — requires attribution + share-alike
  if redistributed; more colorful/playful style, larger variety of animal/shape emoji). If chosen,
  attribution must be recorded in `ASSETS_LICENSES.md` and shown somewhere (e.g. an "About" dialog).
- Curate a subset (~30–50 emoji): shapes (⭐🔵🔺⬛❤️), animals (🐶🐱🐰🦁🐘), fruit (🍎🍌🍇), etc. —
  export as PNG at a large fixed size (e.g. 512×512) at build time so runtime rendering is cheap.

### 6.3 Sounds (need to be collected/recorded)
- **Freesound.org** — https://freesound.org — filter search by **CC0** license specifically (no
  attribution needed) to avoid licensing overhead. Search terms: "pop", "cartoon boop", "chime",
  "kids giggle", "bubble", "xylophone note".
- **Kenney.nl game asset packs** — https://kenney.nl/assets?q=audio — all CC0, includes UI/pop/chime
  sound packs designed exactly for this kind of feedback sound.
- Alternative: record your own short (under 1s) sounds — fully avoids licensing questions.
- Need ~15–30 short (300ms–1s) varied "positive feedback" sounds — pops, chimes, boops, giggles.
  Normalize volume across all clips (e.g. with `ffmpeg -filter:a loudnorm`) so none is jarringly
  louder than others.
- Convert all to WAV (or MP3) at a consistent sample rate; keep files small (a few KB–100KB each).

---

## 7. Data model

```csharp
public record SmashContent(
    string BanglaGlyph,   // e.g. "১" or "ক"
    string EmojiAssetPath,
    Color Color,
    string SoundAssetPath
);
```

`KeyMapService.GetContentForKey(Key key)` returns a `SmashContent` by looking up the glyph from the
static map, and picking random color/emoji/sound from their respective pools.

---

## 8. Milestones (buildable increments)

1. **M1 — Skeleton**: fullscreen borderless topmost WPF window, `PreviewKeyDown` shows raw key
   text in the console/debug output. Alt+F4 closes it. Confirms input capture + fullscreen work.
2. **M2 — Bangla text rendering**: bundle Noto Sans Bengali, render the mapped Bangla glyph/number
   big and centered on keypress (static position first, no animation yet).
3. **M3 — Random shape/emoji**: add emoji asset pool, render random emoji alongside the glyph at a
   random screen position.
4. **M4 — Animation**: add scale-in + fade-out `Storyboard` per burst so screen doesn't just fill up.
5. **M5 — Sound**: integrate NAudio, play a random sound per keypress, verify overlapping/rapid
   keypresses don't stutter or throw (test by literally mashing the keyboard).
6. **M6 — Mouse effects** (optional): cursor trail or bouncing face on mouse move/click.
7. **M7 — Polish & packaging**: color palette tuning, multi-monitor coverage decision, self-contained
   single-file publish, `ASSETS_LICENSES.md` finalized.

---

## 9. Open questions to confirm before/while building (flag, don't block)

- Multi-monitor: cover just the primary screen, or all screens?
- Emoji set: Noto Emoji (no attribution needed) vs OpenMoji (nicer style, attribution required)?
- Any specific sound "personality" wanted (e.g. all cartoon boops vs mixed chimes/giggles)?
- Adult-only secondary exit gesture — wanted or is Alt+F4 alone enough?
