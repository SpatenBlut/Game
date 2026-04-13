# BRAWLHAVEN

Ein lokales 2-Spieler Plattform-Kampfspiel inspiriert von Super Smash Bros — gebaut mit MonoGame und C#.

---

## Über das Spiel

BRAWLHAVEN ist ein 2D-Plattform-Kampfspiel, bei dem zwei Spieler gleichzeitig auf einer mehrstöckigen Arena kämpfen. Statt einer klassischen Lebensleiste sammeln Spieler **Schadensprozente** — je höher der Schaden, desto weiter wird man beim nächsten Treffer aus der Arena geschleudert. Wer zuerst 3 Runden gewinnt, gewinnt das Match.

---

## Features

- **Prozentuales Schadensystem** — Knockback skaliert mit angesammeltem Schaden (`kbBase * (1 + damage/55)`)
- **Best of 3** — Runden à 2 Minuten, Punkte durch Arenen-Abwürfe
- **Plattformphysik** — Doppelsprung, Wandsprung, Coyote-Zeit, Sprungpufferung
- **20 Skins & 15 Arm-Skins** — Common, Rare (animiert) und Legendary (texturbasiert)
- **CS2-Style Shop** — Mystery Chest mit Scroll-Animation, gewichteten Seltenheiten und Duplikat-Erkennung
- **64 Challenges** — Win-/Hit-/Schaden-/Streak-/Perfect-Milestones mit Coin-Belohnungen
- **Bot-KI** — Absichtlich imperfekt: 80% Angriffsgenauigkeit, Dodge-Wahrscheinlichkeit, Kantenvermeidung
- **LAN-Multiplayer** — Custom UDP mit 4-stelligem Code zur Erkennung im lokalen Netzwerk
- **Debug-Terminal** — Mit INSERT öffnen, versteckte Befehle mit `unlock --commands`

---

## Steuerung

| Aktion | Spieler 1 |
|---|---|
| Bewegen | `A` / `D` |
| Springen / Doppelsprung | `W` |
| Schwerer Angriff | Linke Maustaste (Ziel: Mauszeiger) |
| Ausweichen | `S` + Linke Maustaste |
| Debug-Overlay | `F1` |
| Terminal | `INSERT` |
| Beenden | `ESC` |

---

## Installation & Starten

**Voraussetzungen:** .NET 9 SDK, MonoGame 3.8

```bash
# Bauen
~/.dotnet/dotnet build

# Bauen & starten
~/.dotnet/dotnet run --project App2

# Vorkompiliertes Binary starten (Linux)
./start.sh
```

Das Spiel startet im Vollbild ohne festen Timestep (ungedeckelte FPS, VSync deaktiviert).

Speicherdatei: `~/.local/share/BRAWLHAVEN/save.dat`

---

## Tech Stack

| | |
|---|---|
| Framework | MonoGame 3.8 (DesktopGL / OpenGL via SDL2) |
| Sprache | C# (.NET 9) |
| Rendering | 1×1 Pixel-Textur + SpriteBatch-Tinting; custom 5×7 Pixel-Font |
| Netzwerk | Raw `UdpClient` (kein externes Netzwerk-Framework) |
| Build | MonoGame Content Pipeline (MGCB), nur 4 SpriteFont-Dateien |
| Plattform | Linux (primär), Windows (`win-x64` Publish) |

---

## Projektstruktur

```
App2/
├── Game1.cs                  # Haupt-Game-Loop (Update/Draw)
├── Game1.Fields.cs           # Alle Felder, Konstanten, Skin-/Challenge-Arrays
├── Game1.Init.cs             # Konstruktor, LoadContent, Textur-Baking
├── Game1.Gameplay.cs         # Map, Runden-Reset, Kamera, Partikel
├── Game1.Rendering.cs        # Primitive Drawing (R(), DrawEllipse, Plattformen, Spieler)
├── Game1.HUD.cs              # Timer, Schaden-Panels, Score-Dots, Overlays
├── Game1.Screens.cs          # Menü, PlayMenu, NameEntry, Lobby
├── Game1.Shop.cs             # Chest-Opening-Animation, Gewichtungspool
├── Game1.Challenges.cs       # Challenge-Tracking-Logik
├── Game1.ChallengesUI.cs     # Challenges-Screen-Rendering
├── Game1.SkinConfig.cs       # Skin-/Arm-Auswahl, Case-Hardened-Pattern-Picker
├── Game1.Terminal.cs         # Debug-Terminal mit Befehlen
├── Game1.SaveLoad.cs         # Binäres Speicherformat v9
├── Game1.Helpers.cs          # W2S(), KeyJustPressed(), SkinColor(), HsvToRgb()
├── Core/
│   ├── GameState.cs          # State-Enum (Menu, Playing, Shop, ...)
│   ├── PlayerInput.cs        # 2-Byte gepacktes Input-Struct (netzwerkfreundlich)
│   ├── RectF.cs              # Float-AABB mit Intersects()
│   └── Logger.cs             # Session-Datei-Logger
├── Entities/
│   ├── Player.cs             # Physik, Input, Hitbox (OBB/SAT), State Machine
│   └── BotController.cs      # CPU-KI
├── Net/
│   └── GameNet.cs            # UDP LAN-Multiplayer (Host/Client, Input-Sync)
├── Rendering/
│   └── Glyphs.cs             # 5×7 Bitmask-Pixel-Font
└── World/
    ├── Platform.cs           # Platform-Struct + PlatType-Enum
    └── Particle.cs           # Partikel mit Velocity/Lifetime
```
