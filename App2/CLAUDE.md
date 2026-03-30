# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
~/.dotnet/dotnet build    # Build (requires .NET 9; system dotnet is .NET 8)
~/.dotnet/dotnet run      # Build and run the game
```

The game runs fullscreen. Press `ESC` to exit, `F1` to toggle the debug overlay.

## Architecture

This is **BRAWLHAVEN**, a 2-player local platform fighter (Smash Bros-style) built with MonoGame 3.8 (DesktopGL) targeting **.NET 9**. The game is split across these files:

```
App2/
├── Game1.cs              — Game1 class: game loop, camera, HUD, rendering, state machine
├── Core/RectF.cs         — RectF struct (AABB float rectangle + Intersects)
├── Entities/Player.cs    — Player physics, input, attack logic, state
├── Rendering/Glyphs.cs   — 5×7 bitmask font dictionary (no font assets)
└── World/
    ├── Particle.cs       — Short-lived visual effect
    └── Platform.cs       — Platform data + PlatType enum (Main/Side/Small)
```

No `namespace` declarations — all files share the implicit global namespace.

### Coordinate systems

- **World space**: origin-centered, bounds `BL=-1050`, `BR=1050`, `BT=-680`, `BB=780`. Players die when they leave these bounds (`CheckBlast`).
- **Screen space**: `W2S(worldPos)` converts world → screen, applying camera position, zoom, and shake offset. The debug panel (`DEBUG_W=285px`) shifts the game viewport right when open.

### Rendering

No content assets are loaded. All rendering uses a single 1×1 white `Texture2D` (`_pixel`) tinted via `SpriteBatch.Draw`. Helper `R(x,y,w,h,color)` draws colored rectangles. Text is rendered pixel-by-pixel via `DrawPx` / `Txt` / `TxtBig` using the `Glyphs` bitmask font.

Player body is drawn as an ellipse built from stacked horizontal rects. Base radius `25f` (+15% vs original `22f`). At rest `rx == ry` — guaranteed perfect circle (`SX=SY=1`, wobble=0).

### Game loop highlights

- Delta time capped at `0.05s` to avoid tunneling on frame spikes.
- `Player.Update`: input → dodge/attack → jump → gravity → `DoPhysics` (AABB platform resolution) → `UpdateState`.
- Damage scaling: knockback = `kbBase * (1 + damage/55)`. Heavy attack: 19% dmg / 620 base KB. Attack direction locked at start via `AtkDir` field.
- Attack hitbox active only during first `ATK_HITBOX_DUR = 0.10s` of `ATK_DUR = 0.30s` animation.
- Coyote time (`0.12s`) and jump buffer (`0.10s`) for responsive jumping.
- Air control: `Vel.X += (tVX - Vel.X) * dt * 22f`.

### State machine

`GameState`: `Menu → Playing → RoundOver → Playing` (repeat until score reaches `SCORE_TO_WIN=3`) → `GameOver`.

- **Timer**: 2 minutes (`_roundTime = 120f`). On expiry: higher score wins → `EndGame(winner)`; tie → `EndGame(0)` → "UNENTSCHIEDEN" screen.
- **RoundOver**: brief overlay, then auto-resets via `_roundOverTimer`.
- **GameOver**: `[R]` returns to Menu via `ResetMatch()`.

### Controls (current — solo P1 vs CPU placeholder)

| Action | P1 |
|--------|----|
| Move | A / D |
| Jump (×2 = double jump) | W |
| Heavy attack | Left mouse button |
| Dodge (ground only) | S + Left mouse button |
