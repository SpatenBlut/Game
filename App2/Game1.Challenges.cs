using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    int ChalStat(int i) => i switch
    {
        >= 0  and <= 19 => _statWins,
        >= 20 and <= 33 => _statHits,
        >= 34 and <= 45 => _statDmgDealt,
        >= 46 and <= 52 => _statBestStreak,
        >= 53 and <= 58 => _statPerfectWins,
        >= 59 and <= 63 => _statFastWin,
        _ => 0
    };

    int ChalProgress(int i) => Math.Max(0, ChalStat(i) - _chalBaselines[i]);

    void ActivateVisibleChallenges()
    {
        if (!_chalDirty) return;
        _chalDirty = false;
        bool changed = false;
        int visIdx = 0;
        for (int i = 0; i < CHALLENGES.Length; i++)
        {
            if ((_chalClaimed & (1L << i)) != 0) continue;
            if (visIdx >= 7) break;
            if ((_chalActivated & (1L << i)) == 0)
            {
                int stat   = ChalStat(i);
                int target = CHALLENGES[i].Target;
                // Wenn Ziel bereits erreicht → Baseline so setzen dass Progress = Target (sofort abholbar)
                _chalBaselines[i] = stat >= target ? stat - target : stat;
                _chalActivated   |= (1L << i);
                changed = true;
            }
            visIdx++;
        }
        if (changed) SaveGame();
    }

    // Case Hardened (17) is "owned" when at least one pattern is unlocked
    bool SkinOwned(int skin)
    {
        if (skin == 17) return HasAnyCHPattern();
        return skin == 0 || skin == 8 || (_ownedSkins & (1L << skin)) != 0;
    }

    bool CHPatternOwned(int idx) =>
        idx < 64 ? (_ownedCHPatternsLo & (1L << idx)) != 0
                 : (_ownedCHPatternsHi  & (1  << (idx - 64))) != 0;

    void UnlockCHPattern(int idx)
    {
        if (idx < 64) _ownedCHPatternsLo |= (1L << idx);
        else          _ownedCHPatternsHi  |= (1  << (idx - 64));
    }

    bool HasAnyCHPattern() => _ownedCHPatternsLo != 0 || _ownedCHPatternsHi != 0;

    bool HasUnownedSkins()
    {
        for (int i = 1; i < SKINS.Length; i++)
            if (!SkinOwned(i)) return true;
        return false;
    }

    Color SkinColor(int idx, float t)
    {
        var s = SKINS[idx];
        if (!s.Animated) return s.Col;
        if (s.Name == "RAINBOW") return HsvToRgb((t * 0.3f) % 1f, 1f, 1f);
        float f = MathF.Sin(t * 1.5f) * 0.5f + 0.5f;
        return new Color(
            (int)(s.Col.R + (s.Col2.R - s.Col.R) * f),
            (int)(s.Col.G + (s.Col2.G - s.Col.G) * f),
            (int)(s.Col.B + (s.Col2.B - s.Col.B) * f));
    }

    static Color HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - MathF.Abs((h * 6f) % 2f - 1f));
        float m = v - c;
        float r, g, b;
        switch ((int)(h * 6) % 6) {
            case 0:  r=c; g=x; b=0; break;
            case 1:  r=x; g=c; b=0; break;
            case 2:  r=0; g=c; b=x; break;
            case 3:  r=0; g=x; b=c; break;
            case 4:  r=x; g=0; b=c; break;
            default: r=c; g=0; b=x; break;
        }
        return new Color((int)((r+m)*255), (int)((g+m)*255), (int)((b+m)*255));
    }
}
