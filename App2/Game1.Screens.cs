using Microsoft.Xna.Framework;

public partial class Game1
{
    const int MENU_BW = 220, MENU_BH = 56, MENU_GAP = 22;

    void DrawMenuBg()
    {
        R(0, 0, SW, SH, new Color(30, 33, 52));
        for (int x = 0; x < SW; x += 80) R(x, 0, 1, SH, new Color(44, 48, 72));
        for (int y = 0; y < SH; y += 80) R(0, y, SW, 1, new Color(44, 48, 72));
    }

    bool DrawButton(int bx, int by, int bw, int bh, string label)
    {
        bool hover = _mousePos.X >= bx && _mousePos.X <= bx + bw &&
                     _mousePos.Y >= by && _mousePos.Y <= by + bh;
        Color bg     = hover ? new Color(50, 70, 130)   : new Color(38, 42, 72);
        Color border = hover ? new Color(110, 150, 255)  : new Color(50, 60, 100);
        Color tc     = hover ? Color.White               : new Color(160, 170, 210);
        R(bx + 3, by + 3, bw, bh, new Color(0, 0, 0, 60));
        R(bx, by, bw, bh, bg);
        R(bx, by, bw, 2, border);
        R(bx, by + bh - 2, bw, 2, border);
        R(bx, by, 2, bh, border);
        R(bx + bw - 2, by, 2, bh, border);
        TxtBig(label, bx + bw/2 - TxtBigW(label)/2, by + bh/2 - _fontBig.LineSpacing/2, tc);
        return hover;
    }

    bool Clicked(bool click, int bx, int by, int bw, int bh) =>
        click && _mousePos.X >= bx && _mousePos.X <= bx + bw &&
                 _mousePos.Y >= by && _mousePos.Y <= by + bh;

    void DrawMenuTabs(int activeTab)
    {
        string coinsStr = $"COINS: {_coins}";
        TxtMed(coinsStr, SW - TxtMedW(coinsStr) - 20, 20, new Color(255, 200, 50));

        string[] labels = { "PLAY", "CONFIG", "SHOP", "CHALLENGES" };
        const int TAB_W = 220, TAB_GAP = 20;
        int totalW = labels.Length * TAB_W + (labels.Length - 1) * TAB_GAP;
        int startX = SW / 2 - totalW / 2;
        int tabY = 60;
        for (int i = 0; i < labels.Length; i++)
        {
            int tx = startX + i * (TAB_W + TAB_GAP);
            bool sel   = activeTab == i;
            bool hover = _mousePos.X >= tx && _mousePos.X <= tx + TAB_W &&
                         _mousePos.Y >= tabY && _mousePos.Y <= tabY + MENU_BH;
            Color tbg   = sel   ? new Color(50, 70, 140)
                       : hover  ? new Color(44, 55, 110)
                                : new Color(38, 42, 72);
            Color tedge = sel   ? new Color(110, 150, 255) : new Color(50, 60, 100);
            Color tc    = sel   ? Color.White
                       : hover  ? new Color(200, 210, 255)
                                : new Color(160, 170, 210);
            R(tx + 3, tabY + 3, TAB_W, MENU_BH, new Color(0, 0, 0, 60));
            R(tx, tabY, TAB_W, MENU_BH, tbg);
            R(tx, tabY, TAB_W, 2, tedge);
            R(tx, tabY + MENU_BH - 2, TAB_W, 2, sel ? new Color(110, 150, 255) : new Color(38, 42, 72));
            R(tx, tabY, 2, MENU_BH, tedge);
            R(tx + TAB_W - 2, tabY, 2, MENU_BH, tedge);
            TxtBig(labels[i], tx + TAB_W/2 - TxtBigW(labels[i])/2, tabY + MENU_BH/2 - _fontBig.LineSpacing/2, tc);
        }
    }

    bool HandleMenuTabsClick(bool click)
    {
        if (!click) return false;
        string[] labels = { "PLAY", "CONFIG", "SHOP", "CHALLENGES" };
        const int TAB_W = 220, TAB_GAP = 20;
        int totalW = labels.Length * TAB_W + (labels.Length - 1) * TAB_GAP;
        int startX = SW / 2 - totalW / 2;
        int tabY = 60;
        for (int i = 0; i < labels.Length; i++)
        {
            int tx = startX + i * (TAB_W + TAB_GAP);
            if (!Clicked(click, tx, tabY, TAB_W, MENU_BH)) continue;
            if      (i == 0) _state = GameState.PlayMenu;
            else if (i == 1) { _state = GameState.SkinConfig; _configScrollY = 0; }
            else if (i == 2) _state = GameState.Shop;
            else             _state = GameState.Challenges;
            return true;
        }
        return false;
    }

    void DrawPlayMenu()
    {
        DrawMenuBg();
        DrawMenuTabs(0);
        int cx = SW / 2, cy = SH / 2;
        string title = "PLAY";
        TxtBig(title, cx - TxtBigW(title)/2, cy - 160, new Color(100, 140, 255));

        string[] labels = { "LOCAL VS BOT", "MULTIPLAYER" };
        int totalH = labels.Length * (MENU_BH + MENU_GAP) - MENU_GAP;
        int startY = cy - totalH / 2 + 20;
        for (int i = 0; i < labels.Length; i++)
            DrawButton(cx - MENU_BW / 2, startY + i * (MENU_BH + MENU_GAP), MENU_BW, MENU_BH, labels[i]);
    }

    void HandlePlayMenuClick(bool click)
    {
        if (HandleMenuTabsClick(click)) return;
        if (!click) return;
        int cx = SW / 2, cy = SH / 2;
        string[] labels = { "LOCAL VS BOT", "MULTIPLAYER" };
        int totalH = labels.Length * (MENU_BH + MENU_GAP) - MENU_GAP;
        int startY = cy - totalH / 2 + 20;
        for (int i = 0; i < labels.Length; i++)
        {
            int bx = cx - MENU_BW / 2, by = startY + i * (MENU_BH + MENU_GAP);
            if (!Clicked(click, bx, by, MENU_BW, MENU_BH)) continue;
            if (i == 0)
            {
                _isLocalMode = true;
                _bot         = new BotController();
                _state       = GameState.MapSelect;
            }
            else
            {
                _isLocalMode = false;
                _myCode      = _rng.Next(1000, 10000);
                _codeInput   = "";
                _net.Dispose();
                _net = new GameNet();
                _net.OpenLobby(_myCode);
                _state = GameState.Lobby;
            }
            break;
        }
    }

    static readonly string[] MAP_NAMES = { "CLASSIC", "SPACE", "BRIDGE", "RUINS" };

    void DrawMapSelect()
    {
        DrawMenuBg();
        int cx = SW / 2, cy = SH / 2;

        string title = "SELECT MAP";
        TxtBig(title, cx - TxtBigW(title)/2, 30, new Color(100, 140, 255));

        const int cardW = 340, cardH = 80, cardGap = 22;
        int totalW = 2 * cardW + cardGap;
        int totalH = 2 * cardH + cardGap;
        int startX = cx - totalW / 2;
        int startY = cy - totalH / 2 - 10;

        for (int i = 0; i < MAP_NAMES.Length; i++)
        {
            int col = i % 2, row = i / 2;
            int cx2 = startX + col * (cardW + cardGap);
            int cy2 = startY + row * (cardH + cardGap);

            bool sel   = _selectedMap == i;
            bool hover = _mousePos.X >= cx2 && _mousePos.X <= cx2 + cardW &&
                         _mousePos.Y >= cy2 && _mousePos.Y <= cy2 + cardH;
            Color bg   = sel   ? new Color(50, 70, 140)
                       : hover ? new Color(40, 52, 100)
                               : new Color(28, 32, 60);
            Color edge = sel   ? new Color(110, 150, 255)
                       : hover ? new Color(80, 110, 200)
                               : new Color(44, 52, 90);
            R(cx2 + 3, cy2 + 3, cardW, cardH, new Color(0, 0, 0, 60));
            R(cx2, cy2, cardW, cardH, bg);
            R(cx2, cy2, cardW, 2, edge);
            R(cx2, cy2 + cardH - 2, cardW, 2, edge);
            R(cx2, cy2, 2, cardH, edge);
            R(cx2 + cardW - 2, cy2, 2, cardH, edge);

            Color nameCol = sel || hover ? Color.White : new Color(160, 170, 220);
            TxtBig(MAP_NAMES[i], cx2 + cardW/2 - TxtBigW(MAP_NAMES[i])/2, cy2 + cardH/2 - _fontBig.LineSpacing/2, nameCol);
        }

        int playBtnW = MENU_BW, playBtnY = startY + totalH + 28;
        DrawButton(cx - playBtnW / 2, playBtnY, playBtnW, MENU_BH, "PLAY");
        TxtMed("[ESC] BACK", cx - TxtMedW("[ESC] BACK")/2, playBtnY + MENU_BH + 16, new Color(100, 110, 160));
    }

    void HandleMapSelectClick(bool click)
    {
        if (!click) return;
        int cx = SW / 2, cy = SH / 2;

        const int cardW = 340, cardH = 110, cardGap = 22;
        int totalW = 2 * cardW + cardGap;
        int totalH = 2 * cardH + cardGap;
        int startX = cx - totalW / 2;
        int startY = cy - totalH / 2 - 10;

        for (int i = 0; i < MAP_NAMES.Length; i++)
        {
            int col = i % 2, row = i / 2;
            int cx2 = startX + col * (cardW + cardGap);
            int cy2 = startY + row * (cardH + cardGap);
            if (Clicked(click, cx2, cy2, cardW, cardH))
            {
                _selectedMap = i;
                return;
            }
        }

        int playBtnW = MENU_BW, playBtnY = startY + totalH + 28;
        if (Clicked(click, cx - playBtnW / 2, playBtnY, playBtnW, MENU_BH))
        {
            ResetMatch();
        }
    }

    void DrawNameEntry()
    {
        DrawMenuBg();
        int cx = SW / 2, cy = SH / 2;
        string title = "BRAWLHAVEN";
        TxtHuge(title, cx - TxtHugeW(title)/2, cy - 220, new Color(100, 140, 255));

        TxtMed("ENTER YOUR NAME", cx - TxtMedW("ENTER YOUR NAME")/2, cy - 100, new Color(160, 170, 210));

        int bw = 200, bh = _fontBig.LineSpacing + 16;
        int bx = cx - bw / 2, by = cy - 60;
        Color boxEdge = _playerName.Length >= 12 ? new Color(255, 160, 50) : new Color(80, 140, 255, 200);
        R(bx - 2, by - 2, bw + 4, bh + 4, boxEdge);
        R(bx, by, bw, bh, new Color(22, 26, 45));
        string display = _playerName.Length > 0 ? _playerName : "_";
        TxtBig(display, bx + 10, by + (bh - _fontBig.LineSpacing)/2, Color.White);

        if (_playerName.Length >= 1)
            TxtMed("[ENTER] CONFIRM", cx - TxtMedW("[ENTER] CONFIRM")/2, cy + 10, new Color(100, 220, 100));
        else
            TxtMed("TYPE A NAME (MAX 12 CHARS)", cx - TxtMedW("TYPE A NAME (MAX 12 CHARS)")/2, cy + 10, new Color(70, 80, 120));

        TxtMed("NAME APPEARS UNDER YOUR CHARACTER", cx - TxtMedW("NAME APPEARS UNDER YOUR CHARACTER")/2, cy + 36, new Color(60, 70, 110));
    }

    void DrawLobby()
    {
        R(0, 0, SW, SH, new Color(30, 33, 52));
        for (int x = 0; x < SW; x += 80) R(x, 0, 1, SH, new Color(27, 30, 50));
        for (int y = 0; y < SH; y += 80) R(0, y, SW, 1, new Color(27, 30, 50));

        int cx = SW / 2, cy = SH / 2;

        TxtBig("MULTIPLAYER", cx - TxtBigW("MULTIPLAYER")/2, cy - 200, new Color(100, 140, 255));

        // ── Your code (top section) ───────────────────────────────────────
        TxtMed("YOUR CODE", cx - TxtMedW("YOUR CODE")/2, cy - 155, new Color(120, 130, 180));
        string myCodeStr = _myCode.ToString();
        TxtBig(myCodeStr, cx - TxtBigW(myCodeStr)/2, cy - 130, new Color(100, 220, 130));
        TxtMed("SHARE THIS WITH A FRIEND", cx - TxtMedW("SHARE THIS WITH A FRIEND")/2, cy - 104, new Color(70, 80, 120));

        // Divider
        R(cx - 100, cy - 80, 200, 1, new Color(40, 48, 80));

        // ── Join section (bottom section) ─────────────────────────────────
        if (!_net.IsSeeking)
        {
            TxtMed("ENTER FRIEND'S CODE:", cx - TxtMedW("ENTER FRIEND'S CODE:")/2, cy - 62, new Color(160, 170, 210));

            int bw = 108, bh = _fontBig.LineSpacing + 8;
            R(cx - bw/2 - 2, cy - 36, bw + 4, bh + 4, new Color(80, 140, 255, 120));
            R(cx - bw/2,     cy - 34, bw,     bh,     new Color(22, 26, 45));
            string disp = _codeInput.PadRight(4, '_');
            TxtBig(disp, cx - bw/2 + 10, cy - 34 + (bh - _fontBig.LineSpacing)/2, Color.White);

            if (_codeInput.Length == 4)
                TxtMed("[ENTER] SEARCH", cx - TxtMedW("[ENTER] SEARCH")/2, cy + 12, new Color(100, 220, 100));
            else
                TxtMed("[TYPE 4 DIGITS]", cx - TxtMedW("[TYPE 4 DIGITS]")/2, cy + 12, new Color(70, 80, 120));
        }
        else
        {
            string status = $"SEARCHING FOR {_codeInput}...";
            TxtMed(status, cx - TxtMedW(status)/2, cy - 50, new Color(255, 200, 50));
            TxtMed("MAKE SURE YOUR FRIEND IS IN THE LOBBY", cx - TxtMedW("MAKE SURE YOUR FRIEND IS IN THE LOBBY")/2, cy - 24, new Color(70, 80, 120));
        }

        R(cx - 100, cy + 40, 200, 1, new Color(40, 48, 80));
        TxtMed("WAITING FOR FRIEND TO ENTER YOUR CODE", cx - TxtMedW("WAITING FOR FRIEND TO ENTER YOUR CODE")/2, cy + 50, new Color(60, 70, 110));

        TxtMed("[ESC] BACK TO MENU", cx - TxtMedW("[ESC] BACK TO MENU")/2, cy + 80, new Color(100, 110, 160));
    }
}
