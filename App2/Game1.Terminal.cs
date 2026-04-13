using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    void TermPrint(string text, Color col)
    {
        _termLines.Add((text, col));
        if (_termLines.Count > TERM_MAX_LINES) _termLines.RemoveAt(0);
    }

    void SubmitTermCmd()
    {
        string cmd = _termInput.Trim();
        _termInput = "";
        if (cmd == "") return;
        _termScrollOffset = 0;
        TermPrint("> " + cmd, new Color(180, 200, 255));
        ProcessTermCmd(cmd);
    }

    void ProcessTermCmd(string cmd)
    {
        var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        string main = parts[0].ToLower();
        Color ok   = new Color(100, 220, 100);
        Color err  = new Color(220, 100, 80);
        Color info = new Color(180, 200, 255);

        if (cmd.Trim().ToLower() == "unlock --commands")
        {
            _commandsUnlocked = true;
            TermPrint("Commands unlocked.", ok);
            return;
        }

        if (main == "clear")
        {
            _termLines.Clear();
            return;
        }

        if (main == "help")
        {
            if (_commandsUnlocked)
            {
                TermPrint("clear                  Clear terminal", info);
                TermPrint("coins <n>              Add coins", info);
                TermPrint("unlock --all           Unlock all skins", info);
                TermPrint("lock --all             Remove all skins", info);
                TermPrint("challenges --complete  Complete challenges", info);
            }
            else
            {
                TermPrint("No commands available.", info);
            }
            return;
        }

        if (!_commandsUnlocked)
        {
            TermPrint("Unknown command: " + parts[0], err);
            return;
        }

        switch (main)
        {
            case "coins":
                if (parts.Length < 2 || !int.TryParse(parts[1], out int amount) || amount < 0)
                {
                    TermPrint("Usage: coins <positive number>", err);
                    break;
                }
                _coins += amount;
                SaveGame();
                TermPrint($"+{amount} coins  (total: {_coins})", ok);
                break;

            case "unlock":
                if (parts.Length >= 2 && parts[1].ToLower() == "--all")
                {
                    for (int i = 0; i < SKINS.Length; i++)
                        _ownedSkins |= (1L << i);
                    _ownedCHPatternsLo = unchecked((long)0xFFFFFFFFFFFFFFFFUL);
                    _ownedCHPatternsHi = 0x1FFFF;
                    for (int i = 0; i < ARM_SKINS.Length; i++)
                        _ownedArmSkins |= (1L << i);
                    SaveGame();
                    TermPrint($"All {SKINS.Length} skins + 81 CH patterns + {ARM_SKINS.Length} arm skins unlocked.", ok);
                }
                else
                {
                    TermPrint("Usage: unlock --all", err);
                }
                break;

            case "lock":
                if (parts.Length >= 2 && parts[1].ToLower() == "--all")
                {
                    _ownedSkins = (1L << 0) | (1L << 8);
                    _ownedCHPatternsLo = 0;
                    _ownedCHPatternsHi = 0;
                    _ownedArmSkins = (1L << 0);
                    if (_mySkin != 0 && _mySkin != 8) _mySkin = 0;
                    if (!ArmSkinOwned(_myArmSkin)) _myArmSkin = 0;
                    SaveGame();
                    TermPrint("All skins removed (free skins kept).", ok);
                }
                else
                {
                    TermPrint("Usage: lock --all", err);
                }
                break;

            case "challenges":
                if (parts.Length >= 2 && parts[1].ToLower() == "--complete")
                {
                    int visI = 0, done = 0;
                    for (int i = 0; i < CHALLENGES.Length && visI < 7; i++)
                    {
                        if ((_chalClaimed & (1L << i)) != 0) continue;
                        visI++;
                        if ((_chalActivated & (1L << i)) == 0)
                        {
                            _chalBaselines[i] = ChalStat(i);
                            _chalActivated |= (1L << i);
                        }
                        _chalBaselines[i] = ChalStat(i) - CHALLENGES[i].Target;
                        _chalClaimed  |= (1L << i);
                        _coins        += CHALLENGES[i].Coins;
                        done++;
                    }
                    bool allDone = true;
                    for (int j = 0; j < CHALLENGES.Length; j++)
                        if ((_chalClaimed & (1L << j)) == 0) { allDone = false; break; }
                    if (allDone) { _chalClaimed = 0; _chalActivated = 0; }
                    SaveGame();
                    TermPrint($"Completed & claimed {done} challenge(s).", ok);
                }
                else
                {
                    TermPrint("Usage: challenges --complete", err);
                }
                break;

            default:
                TermPrint("Unknown command. Type 'help' for help.", err);
                break;
        }
    }

    void DrawTerminal()
    {
        int ty = SH - TERM_H;
        R(0, ty, SW, TERM_H, new Color(8, 10, 16, 235));
        R(0, ty, SW, 1, new Color(60, 120, 200, 200));

        int headerH = _fontSmall.LineSpacing + 6;
        R(0, ty, SW, headerH, new Color(14, 18, 30, 240));
        Txt("TERMINAL", 8, ty + 3, new Color(80, 160, 255));
        string hint = "[INSERT] schliessen";
        Txt(hint, SW - TxtW(hint) - 8, ty + 3, new Color(60, 80, 110));

        int lineH = _fontSmall.LineSpacing + 2;
        int linesAreaH = TERM_H - headerH - 20;
        int maxVisible = linesAreaH / lineH;
        int maxScroll  = Math.Max(0, _termLines.Count - maxVisible);
        int startLine  = Math.Max(0, _termLines.Count - maxVisible - _termScrollOffset);
        startLine      = Math.Max(0, Math.Min(startLine, _termLines.Count - maxVisible));
        if (_termLines.Count <= maxVisible) startLine = 0;
        for (int i = startLine; i < Math.Min(startLine + maxVisible, _termLines.Count); i++)
        {
            int ly = ty + headerH + 2 + (i - startLine) * lineH;
            Txt(_termLines[i].text, 8, ly, _termLines[i].col);
        }

        if (maxScroll > 0)
        {
            int sbX  = SW - 6;
            int sbH  = linesAreaH;
            int sbY  = ty + 20;
            R(sbX, sbY, 3, sbH, new Color(20, 30, 50));
            int thumbH = Math.Max(12, sbH * maxVisible / _termLines.Count);
            int thumbY = sbY + (sbH - thumbH) * (maxScroll - _termScrollOffset) / maxScroll;
            R(sbX, thumbY, 3, thumbH, new Color(60, 120, 200));
        }

        int inputY = ty + TERM_H - 16;
        R(0, inputY - 2, SW, 1, new Color(30, 50, 80));
        bool cursorOn = _menuTime % 1.0f < 0.5f;
        string inputStr = "> " + _termInput + (cursorOn ? "_" : " ");
        Txt(inputStr, 8, inputY, new Color(200, 230, 255));
    }
}
