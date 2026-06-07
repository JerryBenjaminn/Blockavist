# CLAUDE.md — Blockavist

## Project Overview

## Visual Style

**Art style:** High contrast cartoon, Lego/block-inspired aesthetic. Assets from Kenney Game Assets pack (CC0).

**Cubby design:**
- Colored block/cube shape with separate face sprite layered on top
- No walk animation — block character charm is intentional
- Sprite flips horizontally when changing direction
- Subtle up-down bob while moving (Unity Animator tween, no spritesheet needed)
- Expressions: neutral/happy while walking, sad/shocked on death, big smile on level complete
- Level start: Cubby shows a peace sign gesture during countdown, disappears on GO!



Blockavist is a mobile puzzle game for Android. The player guides a character that moves automatically through levels. The player's task is to tap/destroy elements in the level with their finger so the character can reach the goal. The game will be released on Google Play Store as a free app monetized with ads.

**Engine:** Unity 6000.3.11f1 LTS  
**Target Platform:** Android (Google Play)  
**Monetization:** Free + interstitial ads (Google AdMob)  
**Release Target:** 20 levels, 2 worlds

---

## Core Loop

1. Player starts a level
2. Character begins moving automatically forward
3. Character turns around when it hits a wall
4. Character falls down when there is no surface below, continues moving from the next platform
5. Player taps elements with their finger to modify the level structure
6. Character reaches the goal → level complete → next level unlocks

**Game Over:** Character hits a hazard or falls out of bounds.

---

## Architecture

### Level System

Levels are built using **ScriptableObjects**. Each level is its own `.asset` file containing:
- Level data (tilemap / element positions)
- Level name and number
- World it belongs to
- Star threshold values (optional, for future update)

Adding a new level = new ScriptableObject asset. No code changes required.

### LevelManager

```
LevelManager
├── Loads level from ScriptableObject
├── Instantiates elements
├── Tracks player state (alive / dead / at goal)
└── Triggers level complete / game over
```

### Element System

All level elements inherit from a shared `TileElement` abstract base class:

```
TileElement (abstract)
├── IsDestructible : bool
├── OnPlayerTouch()
├── OnPlayerCollide()
└── Render()
```

Adding a new element type = new class extending `TileElement`. Zero changes to existing code.

### Player

- Uses `CapsuleCollider2D` (not BoxCollider2D) — rounded edges prevent spurious wall detection at tile seams. Size 0.85f x 0.85f.
- `PhysicsMaterial2D` with zero friction applied via `[RequireComponent]`.
- Wall detection uses contact normal threshold of 0.7f (tunable via `[SerializeField]` in inspector).
- Movement uses `rb.linearVelocity` (Unity 6 API — legacy `.velocity` is deprecated).
- Input uses Unity Input System package (new) — `EnhancedTouch` for multi-touch, `Mouse.current` for editor. Zero legacy Input references.

---

## MVP Elements

| Element | Description | Interaction |
|---|---|---|
| **Destructible Tile** | Normal tile that breaks | Tap → destroyed |
| **Indestructible Tile** | Solid obstacle | Cannot be destroyed, player turns |
| **Spikes** | Static hazard | Player dies on contact |
| **Goal** | Level completion point | Player reaches → victory |

### Post-MVP Elements (v1.1 update)
- Launch pad (bounces player upward — can be used as hazard or progression tool)
- Falling hazard tiles (destroy block underneath red hazard tile → it falls)
- More levels (World 3+)
- Star rating system per level
- Moving hazards

---

## Level Structure

**Release target: 20 levels, 2 worlds**

| World | Levels | Theme | Elements |
|---|---|---|---|
| World 1 | 1–10 | Basic Blocks | Destructible tile, indestructible tile, spikes |
| World 2 | 11–20 | *(TBD)* | All MVP elements combined |

**Difficulty curve:**
- Levels 1–3: tutorial, one mechanic at a time
- Levels 4–7: combinations, solutions are clear
- Levels 8–10: requires planning
- World 2: rising difficulty, one "aha moment" per level

**Adding new worlds in updates:**  
New world = new folder of ScriptableObject assets + new world node in UI. No architecture changes needed.

---

## UI & Navigation

### Screens
1. **Main Menu** — logo, Play button, Settings
2. **World Select** — worlds in a grid, locked/unlocked visually
3. **Level Select** — selected world's levels in a grid (Candy Crush style layout)
4. **Game** — playable view
5. **Level Complete** — result, Next Level / World Select
6. **Game Over** — Retry / World Select

### Mobile UX
- All interaction via single finger tap
- No on-screen controls (character moves automatically)
- Pause button in screen corner

---

## Monetization

**Google AdMob — Interstitial Ads**
- Ad shown after every 3rd level completion
- No ads during gameplay
- Implementation: Google Mobile Ads Unity Plugin

**No in-app purchases in MVP.**

---

## Tech Stack

| Area | Choice |
|---|---|
| Engine | Unity 6000.3.11f1 LTS |
| Language | C# |
| Level Data | ScriptableObjects |
| Level Editor | Unity TileMap + custom inspector |
| Input | Unity Input System (new) |
| Ads | Google AdMob (Google Mobile Ads Unity Plugin) |
| Version Control | Git + GitHub |
| Build | Unity Android Build + Google Play Console |

---

## Development Phases

### Phase 1 — Core (prototype) ✅
- [x] Player movement logic (automatic walking, turning, falling)
- [x] TileElement abstract base class architecture
- [x] Destructible tile + indestructible tile
- [x] Spikes (game over)
- [x] Goal (level complete)
- [x] Prefabs for all tile types and player
- [x] Clean GameScene with GameManager and InputHandler
- [x] Unity Input System (new) integration
- [x] One working test level

### Phase 2 — Level System
- [ ] ScriptableObject-based level data
- [ ] LevelManager
- [ ] First 5 levels (World 1)

### Phase 3 — UI & Navigation ✅
- [x] Main Menu
- [x] World Select + Level Select
- [x] Level Complete + Game Over screens
- [x] Level lock/unlock system
- [x] Countdown timer before level starts (3-2-1) — player frozen until GO
- [x] ProgressManager (PlayerPrefs)
- [x] AudioManager (music/sfx toggles)

### Phase 4 — Content ✅
- [x] All 20 levels designed and implemented
- [x] Difficulty curve testing

### Phase 5 — Monetization ✅
- [x] AdMob integration (interstitial every 3rd level)
- [x] Android build & test on device

### Phase 6 — Art, Audio & Polish
- [x] Replace placeholder primitives with purchased assets
- [x] Sound effects (tap, destroy, death, level complete)
- [x] Background music
- [ ] Particle effects (tile destroy, goal reached)
- [ ] UI polish

### Phase 7 — Release
- [ ] Google Play Console account + Store listing
- [ ] Screenshots & store art
- [ ] Age rating, privacy policy
- [ ] Final Android build & optimization
- [ ] Release

---

## Devlog & Marketing

**Platform:** TikTok (primary), possibly YouTube Shorts

**Content ideas:**
- "I built a mobile game myself" series
- Level design process (before/after)
- Claude Code in development — what it looks like
- Bugs and fixes
- Google Play release process

**Goal:** Consistent uploads during development, authenticity over perfection.

---

## Known Constraints & Risks

| Risk | Mitigation |
|---|---|
| Android build issues | Reserve time at the start of Phase 5 |
| AdMob approval takes time | Apply for AdMob account well in advance |
| Level design takes time | Start designing levels on paper during Phase 2 |
| Scope creep | Post-MVP list exists — new ideas go there, not into MVP |
