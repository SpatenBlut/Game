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
        >= 64 and <= 68 => _statHeavyHits,
        >= 69 and <= 73 => _statChestsOpened,
        >= 74 and <= 78 => _statMatches,
        _ => 0
    };

    int ChalProgress(int i) => Math.Max(0, ChalStat(i) - _chalBaselines[i]);

    bool IsChalClaimed(int i)   => i < 64 ? (_chalClaimed    & (1L << i))      != 0
                                           : (_chalClaimedHi  & (1L << (i-64))) != 0;
    bool IsChalActivated(int i) => i < 64 ? (_chalActivated  & (1L << i))      != 0
                                           : (_chalActivatedHi& (1L << (i-64))) != 0;
    void SetChalClaimed(int i)
    {
        if (i < 64) _chalClaimed    |= (1L << i);
        else        _chalClaimedHi  |= (1L << (i-64));
    }
    void SetChalActivated(int i)
    {
        if (i < 64) _chalActivated   |= (1L << i);
        else        _chalActivatedHi |= (1L << (i-64));
    }
    bool AllChalsClaimed()
    {
        for (int j = 0; j < CHALLENGES.Length; j++)
            if (!IsChalClaimed(j)) return false;
        return true;
    }
    void ResetAllChals()
    {
        _chalClaimed = 0; _chalClaimedHi = 0;
        _chalActivated = 0; _chalActivatedHi = 0;
        for (int i = 0; i < _chalBaselines.Length; i++) _chalBaselines[i] = 0;
        _chalDirty = true;
    }

    void ActivateVisibleChallenges()
    {
        if (!_chalDirty) return;
        _chalDirty = false;
        bool changed = false;
        int visIdx = 0;
        for (int i = 0; i < CHALLENGES.Length; i++)
        {
            if (IsChalClaimed(i)) continue;
            if (visIdx >= 7) break;
            if (!IsChalActivated(i))
            {
                int stat   = ChalStat(i);
                int target = CHALLENGES[i].Target;
                // Wenn Ziel bereits erreicht → Baseline so setzen dass Progress = Target (sofort abholbar)
                _chalBaselines[i] = stat >= target ? stat - target : stat;
                SetChalActivated(i);
                changed = true;
            }
            visIdx++;
        }
        if (changed) SaveGame();
    }

    bool ArmSkinOwned(int i) => i == 0 || (_ownedArmSkins & (1L << i)) != 0;

    // Case Hardened is "owned" when at least one pattern is unlocked
    bool SkinOwned(int skin)
    {
        if (skin == SKIN_CASEHARDENED) return HasAnyCHPattern();
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

}
