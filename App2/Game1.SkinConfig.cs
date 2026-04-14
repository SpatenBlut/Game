using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    void DrawSkinConfig()
    {
        DrawMenuBg();
        int cx = SW / 2;
        DrawMenuTabs(1);

        // ── Vertikale SKIN/ARMS Sub-Tabs links, vertikal zentriert ────────────
        string[] tabLabels = { "SKIN", "ARMS" };
        const int tabW = 140, tabH = 44, tabGap = 10, tabX = 30;
        int contentAreaTop = 130;
        int totalTabsH = tabLabels.Length * tabH + (tabLabels.Length - 1) * tabGap;
        int tabStartY  = contentAreaTop + (SH - contentAreaTop - totalTabsH) / 2;

        for (int t = 0; t < tabLabels.Length; t++)
        {
            int ty     = tabStartY + t * (tabH + tabGap);
            bool sel   = _configTab == t;
            bool hover = _mousePos.X >= tabX && _mousePos.X <= tabX + tabW &&
                         _mousePos.Y >= ty   && _mousePos.Y <= ty + tabH;
            Color tbg  = sel   ? new Color(50, 70, 140)
                       : hover ? new Color(34, 42, 90)
                               : new Color(34, 38, 66);
            Color tedge = sel  ? new Color(110, 150, 255) : new Color(44, 52, 90);
            R(tabX + 2, ty + 2, tabW, tabH, new Color(0, 0, 0, 50));
            R(tabX, ty, tabW, tabH, tbg);
            R(tabX, ty, tabW, 2, tedge);
            R(tabX, ty + tabH - 2, tabW, 2, tedge);
            R(tabX, ty, 2, tabH, tedge);
            R(tabX + tabW - 2, ty, 2, tabH, sel ? new Color(110, 150, 255) : new Color(44, 52, 90));
            Color tc = sel ? Color.White : new Color(140, 150, 200);
            TxtMed(tabLabels[t], tabX + tabW/2 - TxtMedW(tabLabels[t])/2, ty + tabH/2 - _fontMed.LineSpacing/2, tc);
        }
        // Vertikale Trennlinie rechts neben den Tabs
        R(tabX + tabW + 8, contentAreaTop, 1, SH - contentAreaTop, new Color(44, 52, 90));

        int contentY = contentAreaTop + 10;

        if (_configTab == 1)
        {
            const int tileW = 300, tileH = 54, gapY = 10;
            int startX = cx - tileW / 2;

            for (int i = 0; i < ARM_SKINS.Length; i++)
            {
                int ty = contentY + i * (tileH + gapY);
                if (ty + tileH < contentY || ty > SH - 50) continue;

                bool owned    = ArmSkinOwned(i);
                bool selected = owned && i == _myArmSkin;
                Color bg   = !owned   ? new Color(22, 24, 40)
                           : selected ? new Color(50, 70, 130)
                                      : new Color(38, 42, 72);
                Color edge = !owned   ? new Color(35, 40, 65)
                           : selected ? new Color(110, 150, 255)
                                      : new Color(50, 60, 100);

                R(startX + 3, ty + 3, tileW, tileH, new Color(0, 0, 0, 60));
                R(startX, ty, tileW, tileH, bg);
                R(startX, ty, tileW, 2, edge);
                R(startX, ty + tileH - 2, tileW, 2, edge);
                R(startX, ty, 2, tileH, edge);
                R(startX + tileW - 2, ty, 2, tileH, edge);

                if (owned)
                {
                    DrawEllipse(startX + 28, ty + tileH / 2, 16, 16, ARM_SKINS[i].Col);
                    TxtMed(ARM_SKINS[i].Name, startX + 52, ty + tileH/2 - _fontMed.LineSpacing/2, ARM_SKINS[i].Col);
                    if (selected)
                        DrawEllipse(startX + tileW - 14, ty + tileH / 2, 5, 5, new Color(100, 160, 255));
                }
                else
                {
                    int lx = startX + 20, ly = ty + tileH / 2 - 8;
                    R(lx + 3, ly,     10, 6, new Color(80, 85, 120));
                    R(lx + 3, ly + 3, 10, 3, bg);
                    R(lx,     ly + 6, 16, 11, new Color(80, 85, 120));
                    R(lx + 6, ly + 9,  4,  4, new Color(30, 33, 52));
                    int lockedMidY = ty + tileH/2 - _fontMed.LineSpacing/2;
                    TxtMed(ARM_SKINS[i].Name, startX + 52, lockedMidY, new Color(55, 60, 90));
                    TxtMed("LOCKED", startX + tileW - TxtMedW("LOCKED") - 14, lockedMidY, new Color(60, 65, 100));
                }
            }
        }

        if (_configTab == 0)
        {
            const int tileW = 300, tileH = 54, gapY = 10;
            int startX = cx - tileW / 2;

            // Eyes toggle
            const int togH = 36, togW = tileW;
            int togY = contentY;
            bool hoverTog = _mousePos.X >= startX && _mousePos.X <= startX + togW &&
                            _mousePos.Y >= togY    && _mousePos.Y <= togY + togH;
            Color togBg   = hoverTog ? new Color(34, 42, 90) : new Color(34, 38, 66);
            Color togEdge = new Color(50, 60, 100);
            R(startX, togY, togW, togH, togBg);
            R(startX, togY, togW, 2, togEdge);
            R(startX, togY + togH - 2, togW, 2, togEdge);
            R(startX, togY, 2, togH, togEdge);
            R(startX + togW - 2, togY, 2, togH, togEdge);
            int togMidY = togY + togH/2 - _fontMed.LineSpacing/2;
            TxtMed("EYES", startX + 14, togMidY, new Color(160, 170, 220));
            Color onCol  = _showEyes ? new Color(80, 220, 100) : new Color(55, 60, 90);
            Color offCol = _showEyes ? new Color(55, 60, 90)   : new Color(220, 80, 80);
            TxtMed("ON",  startX + togW - TxtMedW("ON")  - 56, togMidY, onCol);
            TxtMed("OFF", startX + togW - TxtMedW("OFF") - 20, togMidY, offCol);
            contentY += togH + gapY;

            int idx = 0;

            int listH     = SKINS.Length * (tileH + gapY);
            int visH      = SH - contentY - 50;
            int maxScroll = Math.Max(0, listH - visH);
            if (_configScrollY > maxScroll) _configScrollY = maxScroll;

            for (int i = 0; i < SKINS.Length; i++)
            {
                int ty = contentY + idx * (tileH + gapY) - _configScrollY;
                idx++;
                if (ty + tileH < contentY || ty > SH - 50) continue;

                bool owned      = SkinOwned(i);
                bool selected   = i == _mySkin;
                bool legendary  = i == SKIN_CASEHARDENED || i == SKIN_DAMASCUS || i == SKIN_2145;
                bool rare       = i == SKIN_RAINBOW || i == SKIN_AURORA || i == SKIN_MOLTEN;
                Color bg   = !owned    ? new Color(22, 24, 40)
                           : selected  ? new Color(50, 70, 130)
                                       : new Color(38, 42, 72);
                Color edge = !owned    ? new Color(35, 40, 65)
                           : legendary ? new Color(255, 180, 20)
                           : rare      ? new Color(180, 60, 255)
                           : selected  ? new Color(110, 150, 255)
                                       : new Color(50, 60, 100);

                R(startX + 3, ty + 3, tileW, tileH, new Color(0, 0, 0, 60));
                R(startX, ty, tileW, tileH, bg);
                R(startX, ty, tileW, 2, edge);
                R(startX, ty + tileH - 2, tileW, 2, edge);
                R(startX, ty, 2, tileH, edge);
                R(startX + tileW - 2, ty, 2, tileH, edge);

                if (owned)
                {
                    Color sc = SkinColor(i, _menuTime);
                    DrawSkinEllipse(startX + 28, ty + tileH / 2, 16, 16, i, _menuTime);
                    int skinMidY = ty + tileH/2 - _fontMed.LineSpacing/2;
                    TxtMed(SKINS[i].Name, startX + 52, skinMidY, sc);
                    if (rare && i != SKIN_CASEHARDENED)
                        TxtMed("*", startX + tileW - TxtMedW("*") - 14, skinMidY, new Color(255, 200, 50));
                    if (selected)
                        DrawEllipse(startX + tileW - 14, ty + tileH / 2, 5, 5, new Color(100, 160, 255));
                }
                else
                {
                    int lx = startX + 20, ly = ty + tileH / 2 - 8;
                    R(lx + 3, ly,     10, 6, new Color(80, 85, 120));
                    R(lx + 3, ly + 3, 10, 3, bg);
                    R(lx,     ly + 6, 16, 11, new Color(80, 85, 120));
                    R(lx + 6, ly + 9,  4,  4, new Color(30, 33, 52));
                    int lockedMidY = ty + tileH/2 - _fontMed.LineSpacing/2;
                    TxtMed(SKINS[i].Name, startX + 52, lockedMidY, new Color(55, 60, 90));
                    TxtMed("LOCKED", startX + tileW - TxtMedW("LOCKED") - 14, lockedMidY, new Color(60, 65, 100));
                }
            }

            if (maxScroll > 0)
            {
                int sbX = startX + tileW + 8;
                int sbH = visH;
                int thumbH = Math.Max(24, sbH * visH / listH);
                int thumbY = contentY + (sbH - thumbH) * _configScrollY / maxScroll;
                R(sbX, contentY, 4, sbH, new Color(30, 35, 65));
                R(sbX, thumbY, 4, thumbH, new Color(80, 100, 180));
            }
        }


        // ── Pattern Picker overlay ────────────────────────────────────────────
        if (_showCHPicker)
        {
            const int cols = 9, rows = 9;
            const int tileSize = 66, gap = 5;
            int gridW  = cols * tileSize + (cols - 1) * gap;
            int gridH  = rows * tileSize + (rows - 1) * gap;
            int gridX  = cx - gridW / 2;
            int gridY  = SH / 2 - gridH / 2 - 20;

            R(0, 0, SW, SH, new Color(0, 0, 0, 180));
            R(gridX - 16, gridY - 40, gridW + 32, gridH + 80, new Color(24, 28, 50));
            R(gridX - 16, gridY - 40, gridW + 32, 2, new Color(100, 140, 255));

            string ptitle = "PATTERN";
            TxtBig(ptitle, cx - TxtBigW(ptitle)/2, gridY - _fontBig.LineSpacing - 6, new Color(100, 140, 255));

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int pidx   = row * cols + col;
                    int tx     = gridX + col * (tileSize + gap);
                    int ty     = gridY + row * (tileSize + gap);
                    bool owned = CHPatternOwned(pidx);
                    bool sel   = owned && pidx == _myCHPattern;

                    Color tileBg   = !owned ? new Color(20, 22, 38)
                                   : sel    ? new Color(50, 70, 130)
                                            : new Color(38, 42, 72);
                    Color tileEdge = !owned ? new Color(35, 38, 60)
                                   : sel    ? new Color(110, 150, 255)
                                            : new Color(50, 60, 100);

                    R(tx, ty, tileSize, tileSize, tileBg);
                    R(tx, ty, tileSize, 2, tileEdge);
                    R(tx, ty + tileSize - 2, tileSize, 2, tileEdge);
                    R(tx, ty, 2, tileSize, tileEdge);
                    R(tx + tileSize - 2, ty, 2, tileSize, tileEdge);

                    if (owned)
                    {
                        DrawCaseHardenedEllipse(tx + tileSize / 2, ty + tileSize / 2, 20, 20, pidx);
                        string lbl = $"#{pidx + 1}";
                        TxtMed(lbl, tx + 4, ty + 4, new Color(180, 190, 220));
                    }
                    else
                    {
                        // Lock icon
                        int lx = tx + tileSize / 2 - 7, ly = ty + tileSize / 2 - 8;
                        R(lx + 3, ly,     10, 6, new Color(60, 65, 95));
                        R(lx + 3, ly + 3, 10, 3, tileBg);
                        R(lx,     ly + 6, 16, 11, new Color(60, 65, 95));
                        R(lx + 6, ly + 9,  4,  4, new Color(20, 22, 38));
                    }
                }
            }
            TxtMed("[ESC] CLOSE", cx - TxtMedW("[ESC] CLOSE")/2, gridY + gridH + 10, new Color(100, 110, 160));
        }
    }

    void HandleSkinConfigClick(bool click, bool rightClick)
    {
        if (_showCHPicker)
        {
            if (rightClick) { _showCHPicker = false; return; }
            if (click)
            {
                const int cols = 9, rows = 9;
                const int tileSize = 66, gap = 5;
                int gridW = cols * tileSize + (cols - 1) * gap;
                int gridH = rows * tileSize + (rows - 1) * gap;
                int gridX = SW / 2 - gridW / 2;
                int gridY = SH / 2 - gridH / 2 - 20;
                bool hitTile = false;
                for (int row = 0; row < rows && !hitTile; row++)
                    for (int col = 0; col < cols && !hitTile; col++)
                    {
                        int tx   = gridX + col * (tileSize + gap);
                        int ty   = gridY + row * (tileSize + gap);
                        int pidx = row * cols + col;
                        if (Clicked(click, tx, ty, tileSize, tileSize) && CHPatternOwned(pidx))
                        {
                            _myCHPattern = pidx;
                            _mySkin = SKIN_CASEHARDENED;
                            SaveGame();
                            hitTile = true;
                        }
                    }
            }
            return;
        }

        if (click && HandleMenuTabsClick(click)) return;
        if (!click && !rightClick) return;
        int cx = SW / 2;

        string[] tabLabels = { "SKIN", "ARMS" };
        const int tabW = 140, tabH = 44, tabGap = 10, tabX = 30;
        int contentAreaTop = 130;
        int totalTabsH = tabLabels.Length * tabH + (tabLabels.Length - 1) * tabGap;
        int tabStartY  = contentAreaTop + (SH - contentAreaTop - totalTabsH) / 2;

        for (int t = 0; t < tabLabels.Length; t++)
        {
            int ty = tabStartY + t * (tabH + tabGap);
            if (Clicked(click, tabX, ty, tabW, tabH)) { _configTab = t; return; }
        }

        if (_configTab == 1 && click)
        {
            int armContentY = contentAreaTop + 10;
            const int tileW2 = 300, tileH2 = 54, gapY2 = 10;
            int startX2 = cx - tileW2 / 2;
            for (int i = 0; i < ARM_SKINS.Length; i++)
            {
                int ty = armContentY + i * (tileH2 + gapY2);
                if (Clicked(click, startX2, ty, tileW2, tileH2))
                {
                    if (ArmSkinOwned(i)) { _myArmSkin = i; SaveGame(); }
                    return;
                }
            }
            return;
        }

        if (_configTab != 0) return;

        int contentY = contentAreaTop + 10;
        const int tileW = 300, tileH = 54, gapY = 10;
        int startX = cx - tileW / 2;

        // Eyes toggle click
        if (click && Clicked(click, startX, contentY, tileW, 36))
        {
            _showEyes = !_showEyes;
            SaveGame();
            return;
        }
        contentY += 36 + gapY;

        int idx = 0;

        for (int i = 0; i < SKINS.Length; i++)
        {
            int ty = contentY + idx * (tileH + gapY) - _configScrollY;
            idx++;
            if (ty + tileH < contentY || ty > SH - 50) continue;
            if (!SkinOwned(i)) continue;
            if (i == SKIN_CASEHARDENED && rightClick && Clicked(true, startX, ty, tileW, tileH))
            {
                _showCHPicker = true;
                return;
            }
            if (click && Clicked(click, startX, ty, tileW, tileH)) { _mySkin = i; SaveGame(); return; }
        }
    }
}
