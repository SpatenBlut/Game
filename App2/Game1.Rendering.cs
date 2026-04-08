using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public partial class Game1
{
    void DrawBackground()
    {
        R(GameStartX, 0, GameW, SH, new Color(36, 40, 60));

        Txt($"BRAWLHAVEN [{_net.Role}]", GameStartX + 10, 10, new Color(45, 50, 80));

        string fpsStr = $"FPS {(int)_fps}";
        Color fpsCol  = _fps >= 55 ? new Color(80, 220, 100)
                      : _fps >= 30 ? new Color(255, 200, 50)
                                   : new Color(255, 70, 70);
        Txt(fpsStr, SW - fpsStr.Length * 6 - 10, 10, fpsCol);
    }

    void DrawPlatform(Platform p)
    {
        var s = W2S(new Vector2(p.X, p.Y));
        int w = (int)(p.W * _camZoom), h = (int)(p.H * _camZoom);

        Color body, edge, bot;
        switch (p.Type)
        {
            case PlatType.Main:
                body = new Color(58, 65, 105);
                edge = new Color(120, 138, 210);
                bot  = new Color(0, 0, 0, 80);
                break;
            case PlatType.Side:
                body = new Color(48, 54, 90);
                edge = new Color(95, 112, 175);
                bot  = new Color(0, 0, 0, 60);
                break;
            default:
                body = new Color(40, 46, 78);
                edge = new Color(78, 95, 155);
                bot  = new Color(0, 0, 0, 50);
                break;
        }

        R((int)s.X + 4, (int)s.Y + 5, w, h, new Color(0, 0, 0, 60));
        R((int)s.X, (int)s.Y, w, h, body);
        R((int)s.X + 2, (int)s.Y + 2, w - 4, h - 4, new Color(body.R + 10, body.G + 10, body.B + 12, 255));
        R((int)s.X, (int)s.Y, w, Math.Max(2, (int)(3 * _camZoom)), edge);
        R((int)s.X, (int)s.Y + h - 2, w, 2, bot);
        R((int)s.X, (int)s.Y, 1, h, new Color((int)edge.R, (int)edge.G, (int)edge.B, 60));
        R((int)s.X + w - 1, (int)s.Y, 1, h, new Color(0, 0, 0, 40));
    }

    void DrawEllipse(int cx, int cy, int rx, int ry, Color col, int ox = 0, int oy = 0)
    {
        if (rx < 1 || ry < 1) return;
        for (int dy = -ry; dy <= ry; dy++)
        {
            float t  = (float)dy / ry;
            int   hw = Math.Max(1, (int)(rx * MathF.Sqrt(Math.Max(0f, 1f - t * t))));
            R(cx - hw + ox, cy + dy + oy, hw * 2, 1, col);
        }
    }

    // Samples from a slot of the 9×9 Case Hardened pattern sheet
    Color CaseHardenedColor(float fx, float fy, int patternIdx)
    {
        if (_chPixels == null) return new Color(30, 120, 255);
        int tileW = _chW / 9, tileH = _chH / 9;
        int offX  = (patternIdx % 9) * tileW;
        int offY  = (patternIdx / 9) * tileH;
        int px    = offX + Math.Clamp((int)((fx * 0.5f + 0.5f) * (tileW - 1)), 0, tileW - 1);
        int py    = offY + Math.Clamp((int)((fy * 0.5f + 0.5f) * (tileH - 1)), 0, tileH - 1);
        return _chPixels[py * _chW + px];
    }

    void DrawCaseHardenedEllipse(int cx, int cy, int rx, int ry, int patternIdx)
    {
        if (rx < 1 || ry < 1) return;
        if (_chBaked != null && patternIdx >= 0 && patternIdx < _chBaked.Length && _chBaked[patternIdx] != null)
        {
            _spriteBatch.Draw(_chBaked[patternIdx], new Rectangle(cx - rx, cy - ry, rx * 2, ry * 2), Color.White);
            return;
        }
        for (int dy = -ry; dy <= ry; dy++)
        {
            float ft = (float)dy / ry;
            int   hw = Math.Max(1, (int)(rx * MathF.Sqrt(Math.Max(0f, 1f - ft * ft))));
            for (int dx = -hw; dx < hw; dx++)
                R(cx + dx, cy + dy, 1, 1, CaseHardenedColor((float)dx / rx, ft, patternIdx));
        }
    }

    Color DamascusColor(float fx, float fy)
    {
        if (_damascusPixels == null) return new Color(40, 160, 180);
        int px = Math.Clamp((int)((fx * 0.5f + 0.5f) * (_damascusW - 1)), 0, _damascusW - 1);
        int py = Math.Clamp((int)((fy * 0.5f + 0.5f) * (_damascusH - 1)), 0, _damascusH - 1);
        return _damascusPixels[py * _damascusW + px];
    }

    void DrawDamascusEllipse(int cx, int cy, int rx, int ry)
    {
        if (rx < 1 || ry < 1) return;
        if (_damascusBaked != null)
        {
            _spriteBatch.Draw(_damascusBaked, new Rectangle(cx - rx, cy - ry, rx * 2, ry * 2), Color.White);
            return;
        }
        for (int dy = -ry; dy <= ry; dy++)
        {
            float ft = (float)dy / ry;
            int   hw = Math.Max(1, (int)(rx * MathF.Sqrt(Math.Max(0f, 1f - ft * ft))));
            for (int dx = -hw; dx < hw; dx++)
                R(cx + dx, cy + dy, 1, 1, DamascusColor((float)dx / rx, ft));
        }
    }

    Color Skin2145Color(float fx, float fy)
    {
        if (_skin2145Pixels == null) return new Color(180, 140, 255);
        int px = Math.Clamp((int)((fx * 0.5f + 0.5f) * (_skin2145W - 1)), 0, _skin2145W - 1);
        int py = Math.Clamp((int)((fy * 0.5f + 0.5f) * (_skin2145H - 1)), 0, _skin2145H - 1);
        return _skin2145Pixels[py * _skin2145W + px];
    }

    void DrawSkin2145Ellipse(int cx, int cy, int rx, int ry)
    {
        if (rx < 1 || ry < 1) return;
        if (_skin2145Baked != null)
        {
            _spriteBatch.Draw(_skin2145Baked, new Rectangle(cx - rx, cy - ry, rx * 2, ry * 2), Color.White);
            return;
        }
        for (int dy = -ry; dy <= ry; dy++)
        {
            float ft = (float)dy / ry;
            int   hw = Math.Max(1, (int)(rx * MathF.Sqrt(Math.Max(0f, 1f - ft * ft))));
            for (int dx = -hw; dx < hw; dx++)
                R(cx + dx, cy + dy, 1, 1, Skin2145Color((float)dx / rx, ft));
        }
    }

    void DrawSkinEllipse(int cx, int cy, int rx, int ry, int skinIdx, float time, int ox = 0, int oy = 0)
    {
        if      (skinIdx == SKIN_CASEHARDENED) DrawCaseHardenedEllipse(cx + ox, cy + oy, rx, ry, _myCHPattern);
        else if (skinIdx == SKIN_DAMASCUS)     DrawDamascusEllipse(cx + ox, cy + oy, rx, ry);
        else if (skinIdx == SKIN_2145)         DrawSkin2145Ellipse(cx + ox, cy + oy, rx, ry);
        else                                   DrawEllipse(cx, cy, rx, ry, SkinColor(skinIdx, time), ox, oy);
    }

    void DrawSkinBodyEllipse(int cx, int cy, int rx, int ry, int skinIdx, Color col, float time)
    {
        bool flashing = col.B == 255 && col.G > 180;
        bool hitstun  = col.R > 200 && col.G > 200 && col.B > 200;
        if (flashing || hitstun) { DrawEllipse(cx, cy, rx, ry, col); return; }
        if      (skinIdx == SKIN_CASEHARDENED) DrawCaseHardenedEllipse(cx, cy, rx, ry, _myCHPattern);
        else if (skinIdx == SKIN_DAMASCUS)     DrawDamascusEllipse(cx, cy, rx, ry);
        else if (skinIdx == SKIN_2145)         DrawSkin2145Ellipse(cx, cy, rx, ry);
        else                                   DrawEllipse(cx, cy, rx, ry, col);
    }

    void DrawPlayer(Player p)
    {
        if (p == null || p.Dead) return;
        if (p.Invincible && (int)(p.InvTime * 18) % 2 == 0) return;

        var   feet = W2S(p.Pos);
        float z    = _camZoom;
        Color col  = p.Hitstun > 0  ? new Color(Math.Min(255, p.Color.R + 100),
                                                Math.Min(255, p.Color.G + 100),
                                                Math.Min(255, p.Color.B + 100))
                   : p.State == "DODGE" ? new Color(Math.Min(255, p.Color.R + 80),
                                                    Math.Min(255, p.Color.G + 80), 255, 220)
                   : p.Color;

        int cx  = (int)feet.X;
        int fy  = (int)feet.Y;

        int baseR = Math.Max(3, (int)(25f * z));
        float wobble = MathF.Sin(p.WalkTimer * 2.2f) * 1.8f * z;
        int rx = Math.Max(3, (int)(baseR * p.SX) + (int)wobble);
        int ry = Math.Max(3, (int)(baseR * p.SY) - (int)(wobble * 0.6f));

        int bcy = fy - ry;

        DrawEllipse(cx, fy, rx, Math.Max(1, (int)(5 * z)), new Color(0, 0, 0, 50), 4, 4);
        DrawEllipse(cx, bcy, rx, ry, new Color(0, 0, 0, 70), 4, 4);
        DrawSkinBodyEllipse(cx, bcy, rx, ry, p.SkinIdx, col, _menuTime);
        DrawEllipse(cx, bcy + ry / 3, Math.Max(1, rx - (int)(3*z)), Math.Max(1, ry / 2),
                    new Color(0, 0, 0, 30));

        bool drawEyes = p.Id != 1 || _showEyes;

        int hlRx = Math.Max(1, (int)(rx * 0.42f));
        int hlRy = Math.Max(1, (int)(ry * 0.35f));
        if (drawEyes)
            DrawEllipse(cx - (int)(rx * 0.25f), bcy - (int)(ry * 0.32f), hlRx, hlRy,
                        new Color(255, 255, 255, 60));

        if (drawEyes)
        {
            int es        = Math.Max(3, (int)(6f * z));
            int eyeY      = bcy - (int)(ry * 0.18f);
            int eyeSpread = Math.Max(3, (int)(rx * 0.40f));
            int pupilOff  = p.FacingRight ? Math.Max(1, (int)(1.5f * z)) : -Math.Max(1, (int)(1.5f * z));
            int ps        = Math.Max(1, (int)(es * 0.55f));
            int ss        = Math.Max(1, ps / 2);

            DrawEllipse(cx - eyeSpread, eyeY, es, es, Color.White);
            DrawEllipse(cx + eyeSpread, eyeY, es, es, Color.White);
            DrawEllipse(cx - eyeSpread + pupilOff, eyeY + Math.Max(1, (int)(z * 0.6f)), ps, ps, new Color(15, 30, 110));
            DrawEllipse(cx + eyeSpread + pupilOff, eyeY + Math.Max(1, (int)(z * 0.6f)), ps, ps, new Color(15, 30, 110));
            DrawEllipse(cx - eyeSpread + pupilOff - 1, eyeY - 1, ss, ss, new Color(255, 255, 255, 210));
            DrawEllipse(cx + eyeSpread + pupilOff - 1, eyeY - 1, ss, ss, new Color(255, 255, 255, 210));
        }

        if (drawEyes && p.Hitstun > 0)
        {
            int es2 = Math.Max(3, (int)(6f * z));
            int eyeY2 = bcy - (int)(ry * 0.18f);
            int mw = Math.Max(3, (int)(7 * z));
            int my = eyeY2 + es2 + Math.Max(1, (int)(3 * z));
            R(cx - mw / 2, my, mw, Math.Max(1, (int)(1.5f * z)), new Color(0, 0, 0, 130));
        }

        string nameTag = p.Id == 1 ? _playerName : (_isLocalMode ? "BOT" : "P2");
        if (nameTag.Length > 0)
        {
            Color nameCol = p.Id == 1 ? new Color(80, 220, 80) : new Color(220, 80, 80);
            TxtBig(nameTag, cx - nameTag.Length * 6, bcy - ry - 30, nameCol);
        }

        if (_debugOpen)
            Txt(p.State, cx - p.State.Length * 3, bcy - ry - 46, new Color(160, 165, 200));

        if (p.DodgeCD > 0)
        {
            float ratio = 1f - p.DodgeCD / Player.DODGE_CD;
            int   barW  = rx * 2;
            R(cx - barW / 2, fy + 6, barW, 3, new Color(30, 32, 50));
            R(cx - barW / 2, fy + 6, (int)(barW * ratio), 3, new Color(80, 180, 255));
        }

        Color wa = new Color(255, 50, 50, 200);
        if (p.Pos.X < BL + 200) R(cx - rx - 10, bcy - 5, 7, 10, wa);
        if (p.Pos.X > BR - 200) R(cx + rx + 3,  bcy - 5, 7, 10, wa);
        if (p.Pos.Y > BB - 200) R(cx - 5, fy + 6, 10, 7, wa);
    }

    void DrawAttackHitbox(Player p)
    {
        if (p == null || !p.AttackActive) return;
        if (p.AtkTimer <= (Player.ATK_DUR - Player.ATK_HITBOX_DUR)) return;
        Vector2 center = W2S(p.HitboxCenter);
        float   w = p.HitboxHalfW * 2f * _camZoom;
        float   h = p.HitboxHalfH * 2f * _camZoom;
        Color fill = p.IsHeavy ? new Color(255, 100, 50, 55)  : new Color(255, 220, 50, 55);
        Color edge = p.IsHeavy ? new Color(255, 130, 60, 210) : new Color(255, 230, 60, 210);
        var origin = new Vector2(0.5f, 0.5f);
        _spriteBatch.Draw(_pixel, center, null, edge, p.HitboxAngle,
            origin, new Vector2(w + 3, h + 3), SpriteEffects.None, 0f);
        _spriteBatch.Draw(_pixel, center, null, fill, p.HitboxAngle,
            origin, new Vector2(w, h), SpriteEffects.None, 0f);
    }

    void DrawParticle(Particle p)
    {
        var s = W2S(p.Pos);
        int sz = Math.Max(1, (int)(p.Size * _camZoom));
        byte a = (byte)(p.Life / p.MaxLife * 255);
        R((int)s.X - sz/2, (int)s.Y - sz/2, sz, sz, new Color(p.Col.R, p.Col.G, p.Col.B, a));
    }

    void DrawAttackAnim(Player p)
    {
        if (p == null || !p.AttackActive) return;

        var   feet = W2S(p.Pos);
        float z    = _camZoom;
        float t    = Math.Clamp(p.AtkTimer / Player.ATK_DUR, 0f, 1f);
        byte  alpha = (byte)(t * 230 + 40);

        Color armBase = p.Id == 1 ? ARM_SKINS[_myArmSkin].Col : p.Color;
        Color col = new Color(
            Math.Min(255, armBase.R + 50),
            Math.Min(255, armBase.G + 50),
            Math.Min(255, armBase.B + 50),
            (int)alpha);

        var  aimD    = p.GetAtkAimDir();
        int  bodyRx  = Math.Max(3, (int)(25f * z * p.SX));
        int  bodyRy  = Math.Max(3, (int)(25f * z * p.SY));
        int  bodyBcY = (int)feet.Y - bodyRy;

        int armLen    = (int)(65f * z * t + 14f * z);
        int armThick  = Math.Max(2, (int)(8f * z));
        int armStartX = (int)feet.X  + (int)(aimD.X * bodyRx);
        int armStartY = bodyBcY      + (int)(aimD.Y * bodyRy);
        int armEndX   = armStartX    + (int)(aimD.X * armLen);
        int armEndY   = armStartY    + (int)(aimD.Y * armLen);

        int steps = Math.Max(4, armLen / 7);
        for (int s = 0; s <= steps; s++)
        {
            float frac = (float)s / steps;
            int px = armStartX + (int)(aimD.X * armLen * frac);
            int py = armStartY + (int)(aimD.Y * armLen * frac);
            DrawEllipse(px, py, armThick, armThick, col);
        }

        int fr = Math.Max(3, (int)(13f * z));
        DrawEllipse(armEndX, armEndY, fr, fr, col);
        DrawEllipse(armEndX, armEndY, Math.Max(1, fr / 2), Math.Max(1, fr / 2),
                    new Color(255, 255, 255, (int)(alpha * 0.55f)));

        if (t > 0.55f)
        {
            byte sa = (byte)((t - 0.55f) / 0.45f * 255);
            Color spark = new Color(
                Math.Min(255, armBase.R + 80),
                Math.Min(255, armBase.G + 80),
                Math.Min(255, armBase.B + 80),
                (int)sa);
            int sr = (int)(20f * z);
            R(armEndX - 2, armEndY - sr, 4, sr * 2, spark);
            R(armEndX - sr, armEndY - 2, sr * 2, 4, spark);
            DrawEllipse(armEndX, armEndY, Math.Max(1, (int)(sr * 0.55f)), Math.Max(1, (int)(sr * 0.55f)),
                        new Color(Math.Min(255, armBase.R + 80),
                                  Math.Min(255, armBase.G + 80),
                                  Math.Min(255, armBase.B + 80),
                                  (int)sa / 3));
        }
    }

    // ── Primitives ───────────────────────────────────────────────────────────

    void R(int x, int y, int w, int h, Color c)
    { if (w > 0 && h > 0) _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), c); }

    void Txt(string t, int x, int y, Color c)
    { DrawPx(t, x+1, y+1, new Color(0,0,0,100), 1); DrawPx(t, x, y, c, 1); }

    void TxtMed(string t, int x, int y, Color c)
    { DrawPx(t, x+1, y+1, new Color(0,0,0,120), 2); DrawPx(t, x, y, c, 2); }

    void TxtBig(string t, int x, int y, Color c)
    { DrawPx(t, x+2, y+2, new Color(0,0,0,100), 2); DrawPx(t, x, y, c, 2); }

    void TxtHuge(string t, int x, int y, Color c)
    { DrawPx(t, x+3, y+3, new Color(0,0,0,120), 3); DrawPx(t, x, y, c, 3); }

    void DrawPx(string text, int x, int y, Color col, int sc)
    {
        int cx = x;
        foreach (char ch in text.ToUpper())
        {
            if (Glyphs.Map.TryGetValue(ch, out ulong bits))
                for (int row=0; row<7; row++)
                for (int c2=0; c2<5; c2++)
                    if (((bits >> (34 - row*5 - c2)) & 1) == 1)
                        R(cx + c2*sc, y + row*sc, sc, sc, col);
            cx += 6*sc;
        }
    }
}
