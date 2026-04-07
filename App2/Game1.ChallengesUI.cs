using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    void DrawChallenges()
    {
        DrawMenuBg();
        int cx = SW / 2;
        string title = "CHALLENGES";
        TxtBig(title, cx - title.Length * 6, 36, new Color(100, 140, 255));

        const int tileW = 560, tileH = 80, gapY = 10;
        int startX = cx - tileW / 2;
        int startY = 86;

        int visIdx = 0;
        for (int i = 0; i < CHALLENGES.Length; i++)
        {
            bool claimed = (_chalClaimed & (1L << i)) != 0;
            if (claimed) continue;
            if (visIdx >= 7) break;

            var  ch       = CHALLENGES[i];
            int  ty       = startY + visIdx * (tileH + gapY);
            int  progress = ChalProgress(i);
            bool complete = progress >= ch.Target;

            Color bg   = complete ? new Color(28, 40, 55) : new Color(22, 24, 42);
            Color edge = complete ? new Color(100, 160, 255) : new Color(45, 50, 80);
            R(startX + 3, ty + 3, tileW, tileH, new Color(0, 0, 0, 60));
            R(startX, ty, tileW, tileH, bg);
            R(startX, ty, tileW, 2, edge);
            R(startX, ty + tileH - 2, tileW, 2, edge);
            R(startX, ty, 2, tileH, edge);
            R(startX + tileW - 2, ty, 2, tileH, edge);

            DrawEllipse(startX + 22, ty + tileH / 2, 14, 14, new Color(255, 200, 50));

            Color titleCol = complete ? Color.White : new Color(160, 170, 210);
            TxtMed(ch.Title, startX + 44, ty + 10, titleCol);
            TxtMed(ch.Desc,  startX + 44, ty + 30, new Color(100, 110, 150));

            int barX = startX + 44, barY = ty + 54, barW = 280, barH = 8;
            R(barX, barY, barW, barH, new Color(30, 32, 50));
            int fill = (int)(barW * Math.Min(1f, (float)progress / ch.Target));
            Color barCol = complete ? new Color(100, 200, 255) : new Color(80, 130, 220);
            R(barX, barY, fill, barH, barCol);
            string progStr = $"{Math.Min(progress, ch.Target)}/{ch.Target}";
            Txt(progStr, barX + barW + 6, barY, new Color(120, 130, 170));

            int claimW  = 80;
            int claimX  = startX + tileW - claimW - 10;
            int claimBY = ty + 14;
            int claimBH = tileH - 28;
            int claimMY = ty + tileH / 2 - 7;
            if (complete)
            {
                bool hover = _mousePos.X >= claimX && _mousePos.X <= claimX + claimW &&
                             _mousePos.Y >= ty     && _mousePos.Y <= ty + tileH;
                R(claimX, claimBY, claimW, claimBH, hover ? new Color(50, 130, 50) : new Color(28, 80, 28));
                R(claimX, claimBY, claimW, 2, hover ? new Color(80, 180, 80) : new Color(40, 110, 40));
                R(claimX, claimBY + claimBH - 2, claimW, 2, hover ? new Color(80, 180, 80) : new Color(40, 110, 40));
                TxtMed("CLAIM", claimX + claimW / 2 - 30, claimMY, new Color(150, 220, 150));
            }
            else
            {
                string rewardStr = $"+{ch.Coins}";
                TxtMed(rewardStr, claimX + claimW / 2 - rewardStr.Length * 6, claimMY, new Color(70, 80, 55));
            }
            visIdx++;
        }

        TxtMed("[ESC] BACK", cx - 60, SH - 36, new Color(100, 110, 160));
    }

    void HandleChallengesClick(bool click)
    {
        if (!click) return;
        const int tileW = 560, tileH = 80, gapY = 10;
        int startX = SW / 2 - tileW / 2;
        int startY = 86;

        int visIdx = 0;
        for (int i = 0; i < CHALLENGES.Length; i++)
        {
            bool claimed = (_chalClaimed & (1L << i)) != 0;
            if (claimed) continue;
            if (visIdx >= 7) break;
            int  ty      = startY + visIdx * (tileH + gapY);
            bool complete = ChalProgress(i) >= CHALLENGES[i].Target;
            if (complete)
            {
                int claimW = 80;
                int claimX = startX + tileW - claimW - 10;
                if (Clicked(click, claimX, ty, claimW, tileH))
                {
                    _chalClaimed |= (1L << i);
                    _coins += CHALLENGES[i].Coins;
                    bool allDone = true;
                    for (int j = 0; j < CHALLENGES.Length; j++)
                        if ((_chalClaimed & (1L << j)) == 0) { allDone = false; break; }
                    if (allDone) { _chalClaimed = 0; _chalActivated = 0; }
                    SaveGame();
                    return;
                }
            }
            visIdx++;
        }
    }
}
