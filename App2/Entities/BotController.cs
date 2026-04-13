using Microsoft.Xna.Framework;
using System;

public class BotController
{
    // Zustand vom letzten Frame für Rising-Edge-Erkennung
    bool _prevJump, _prevAttack, _prevDodge;

    // Zufallsgenerator für menschliche Fehler
    readonly Random _rng = new Random();

    // Verzögerungen / Cooldowns
    float _attackDelay   = 0f;   // kurze Pause zwischen Angriffen
    float _reactionDelay = 0f;   // Reaktionsverzögerung

    public PlayerInput Update(float dt, Player bot, Player target)
    {
        if (_attackDelay   > 0f) _attackDelay   -= dt;
        if (_reactionDelay > 0f) _reactionDelay -= dt;

        float dx   = target.Pos.X - bot.Pos.X;
        float dy   = target.Pos.Y - bot.Pos.Y;   // negativ = Ziel ist höher
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        // ── Bewegung ──────────────────────────────────────────────────────
        bool wantLeft  = dx < -55f;
        bool wantRight = dx > 55f;

        // Zu nah → kurz zurückweichen (nur wenn nicht gerade angreifend)
        if (dist < 40f && !bot.AttackActive)
        {
            wantLeft  = dx > 0;
            wantRight = dx < 0;
        }

        // ── Springen ──────────────────────────────────────────────────────
        // Springt wenn Ziel deutlich höher ist oder über einem hängt
        bool wantJump = false;
        if (bot.Grounded && _reactionDelay <= 0f)
        {
            if (dy < -120f)                         wantJump = true;   // Ziel oben
            if (dy < -40f && dist < 200f)           wantJump = true;   // Ziel leicht oben, nah
            if (_rng.NextDouble() < 0.003)           wantJump = true;  // gelegentlich zufällig
        }

        // Nicht von der Plattform fallen: wenn Boden-Edge in Laufrichtung
        // (einfache Heuristik: Rand-X der Hauptplattform ±500)
        const float EDGE = 480f;
        if (wantRight && bot.Pos.X > EDGE)  { wantRight = false; wantLeft  = true; }
        if (wantLeft  && bot.Pos.X < -EDGE) { wantLeft  = false; wantRight = true; }

        // ── Angriff ───────────────────────────────────────────────────────
        bool wantAttack = false;
        if (_attackDelay <= 0f && _reactionDelay <= 0f)
        {
            float hitRange = bot.AttackActive ? 130f : 105f;
            if (dist < hitRange && MathF.Abs(dy) < 90f)
            {
                // 80 % Chance zu attackieren (kein perfekter Bot)
                if (_rng.NextDouble() < 0.80)
                {
                    wantAttack   = true;
                    _attackDelay = 0.55f + (float)_rng.NextDouble() * 0.25f;
                }
            }
        }

        // ── Ausweichen ────────────────────────────────────────────────────
        bool wantDodge = false;
        if (bot.DodgeCD <= 0f && bot.Hitstun > 0.05f && _rng.NextDouble() < 0.55)
        {
            wantDodge      = true;
            _reactionDelay = 0.18f;
        }

        // ── Aim-Winkel ────────────────────────────────────────────────────
        // Beim Dodge: weg vom Gegner und leicht nach oben
        // Beim Angriff: Richtung Gegner
        byte aimAngle;
        if (wantDodge)
        {
            float awayX = dx <= 0 ? 1f : -1f;
            float awayY = -0.5f;
            float ang   = MathF.Atan2(awayY, awayX);
            aimAngle    = (byte)((int)(ang / (MathF.PI * 2f) * 256f + 256f) & 0xFF);
        }
        else
        {
            aimAngle = dx >= 0f ? Player.AIM_RIGHT : Player.AIM_LEFT;
        }

        // ── Input-Byte zusammenbauen ──────────────────────────────────────
        byte b = 0;
        if (wantLeft)                          b |= 0x80;
        if (wantRight)                         b |= 0x40;
        if (wantJump)                          b |= 0x20; // Up gehalten
        if (wantJump   && !_prevJump)          b |= 0x08; // Jump rising edge
        if (wantAttack && !_prevAttack)        b |= 0x04; // Attack rising edge
        if (wantDodge  && !_prevDodge)         b |= 0x02; // Dodge rising edge

        _prevJump   = wantJump;
        _prevAttack = wantAttack;
        _prevDodge  = wantDodge;

        return new PlayerInput { Raw = b, AimAngle = aimAngle };
    }
}
