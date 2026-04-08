# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
~/.dotnet/dotnet build    # Build (requires .NET 9; system dotnet is .NET 8)
~/.dotnet/dotnet run      # Build and run the game
```

The game runs fullscreen. Press `ESC` to exit, `F1` to toggle the debug overlay.

## Architecture

This is **BRAWLHAVEN**, a 2-player local platform fighter (Smash Bros-style) built with MonoGame 3.8 (DesktopGL) targeting **.NET 9**.

No `namespace` declarations — all files share the implicit global namespace.

### File structure

```
App2/
├── Game1.cs                — Game loop: Update() dispatch + Draw() dispatch
├── Game1.Fields.cs         — All Game1 field declarations; SKINS[], ARM_SKINS[], CHALLENGES[]; SKIN_* index constants
├── Game1.Init.cs           — Constructor, Initialize(), LoadContent() (texture baking), OnExiting()
├── Game1.Helpers.cs        — W2S(), KeyJustPressed(), OnTextInput(), SkinColor(), HsvToRgb()
├── Game1.Gameplay.cs       — BuildMap(), ResetRound(), CheckBlast(), camera, particle spawning
├── Game1.Rendering.cs      — All drawing: players, platforms, particles, attack animations, primitives (R/Txt/DrawEllipse)
├── Game1.HUD.cs            — Timer, damage panels, score dots, round/game-over overlays, debug panel
├── Game1.Screens.cs        — Menu, PlayMenu, NameEntry, Lobby screens + click handlers
├── Game1.Shop.cs           — Chest animation, rarity pools, AwardChestResult(), shop UI
├── Game1.Challenges.cs     — Challenge activation/tracking, skin ownership (SkinOwned, CHPatternOwned)
├── Game1.ChallengesUI.cs   — Challenges screen drawing + claim click handler
├── Game1.SkinConfig.cs     — Skin tab, Arm tab, Case Hardened pattern picker overlay
├── Game1.Terminal.cs       — In-game debug terminal (coins, unlock, lock commands)
├── Game1.SaveLoad.cs       — Binary save format v9 (LoadSave / SaveGame)
├── Core/
│   ├── GameState.cs        — GameState enum (Menu/Playing/Shop/…)
│   ├── Logger.cs           — File-based session logger
│   ├── RectF.cs            — Float AABB struct + Intersects()
│   └── PlayerInput.cs      — 2-byte packed input struct for networking
├── Entities/
│   ├── Player.cs           — Physics, input, attack hitbox, squash/stretch
│   └── BotController.cs    — CPU opponent AI
├── Net/
│   └── GameNet.cs          — UDP LAN multiplayer (discovery + game traffic)
├── Rendering/
│   └── Glyphs.cs           — 5×7 bitmask font dictionary
└── World/
    ├── Particle.cs         — Short-lived visual particle
    └── Platform.cs         — Platform data + PlatType enum
```

### Key constants (Game1.Fields.cs)

Skin indices — always use the named constants, never magic numbers:
- `SKIN_RAINBOW=14`, `SKIN_AURORA=15`, `SKIN_MOLTEN=16` — rare (2 % chest chance)
- `SKIN_CASEHARDENED=17`, `SKIN_DAMASCUS=18`, `SKIN_2145=19` — legendary (0.5 % chest chance)

World bounds: `BL=-1050`, `BR=1050`, `BT=-680`, `BB=780`

### Coordinate systems

- **World space**: origin-centered; players die when they leave bounds (`CheckBlast`).
- **Screen space**: `W2S(worldPos)` converts world → screen, applying camera position, zoom, and shake offset. The debug panel (`DEBUG_W=285px`) shifts the viewport right when open.

### Rendering

All rendering uses a single 1×1 white `Texture2D` (`_pixel`) tinted via `SpriteBatch.Draw`. One `Begin`/`End` pair per frame.

- `R(x,y,w,h,color)` — draw colored rectangle
- `DrawEllipse(cx,cy,rx,ry,color)` — stacked horizontal rects
- `DrawSkinEllipse` / `DrawSkinBodyEllipse` — dispatch to texture skin or plain ellipse
- Texture skins (Damascus/CaseHardened/2145) are pre-baked to `Texture2D` at load time (`BakeCircularSkin`) — drawn as a single scaled `SpriteBatch.Draw` call, not per-pixel
- Text rendered pixel-by-pixel via `DrawPx` / `Txt` / `TxtBig` / `TxtHuge` using `Glyphs`

### Game loop highlights

- Delta time capped at `0.05s` to avoid tunneling on frame spikes.
- `Player.Update`: input → dodge/attack → jump → gravity → `DoPhysics` (AABB) → `UpdateState`.
- Damage scaling: knockback = `kbBase * (1 + damage/55)`. Heavy attack: 19 % dmg / 620 base KB.
- Attack hitbox active only during first `ATK_HITBOX_DUR=0.10s` of `ATK_DUR=0.30s`.
- Coyote time `0.12s`, jump buffer `0.10s`.
- Particles capped at `MAX_PARTICLES=250`; dead particles removed with swap-and-pop.
- Challenge scan (`ActivateVisibleChallenges`) runs only when `_chalDirty=true`.

### State machine

`GameState` (defined in `Core/GameState.cs`):
`Menu → Playing → RoundOver → Playing` (repeat until `SCORE_TO_WIN=3`) → `GameOver`.

- **Timer**: 2 minutes (`_roundTime=120f`). Tie → `EndGame(0)` → "UNENTSCHIEDEN".
- **RoundOver**: auto-resets via `_roundOverTimer` after `ROUND_OVER_DELAY=2.2s`.
- **GameOver**: `[R]` → Menu via `ResetMatch()`.

### Controls

| Action | P1 |
|--------|----|
| Move | A / D |
| Jump (×2 = double jump) | W |
| Heavy attack | Left mouse button |
| Dodge (ground only) | S + Left mouse button |
