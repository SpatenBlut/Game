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

    void DrawMenu()
    {
        DrawMenuBg();
        int cx = SW / 2;

        // Coins oben rechts
        string coinsStr = $"COINS: {_coins}";
        TxtMed(coinsStr, SW - TxtMedW(coinsStr) - 20, 20, new Color(255, 200, 50));

        // Tabs horizontal oben
        string[] labels = { "PLAY", "CONFIG", "SHOP", "CHALLENGES" };
        const int TAB_W = 220, TAB_GAP = 20;
        int totalW = labels.Length * TAB_W + (labels.Length - 1) * TAB_GAP;
        int startX = cx - totalW / 2;
        int tabY = 60;
        for (int i = 0; i < labels.Length; i++)
            DrawButton(startX + i * (TAB_W + TAB_GAP), tabY, TAB_W, MENU_BH, labels[i]);
    }

    void HandleMenuClick(bool click)
    {
        if (!click) return;
        int cx = SW / 2;
        string[] labels = { "PLAY", "CONFIG", "SHOP", "CHALLENGES" };
        const int TAB_W = 220, TAB_GAP = 20;
        int totalW = labels.Length * TAB_W + (labels.Length - 1) * TAB_GAP;
        int startX = cx - totalW / 2;
        int tabY = 60;
        for (int i = 0; i < labels.Length; i++)
        {
            int bx = startX + i * (TAB_W + TAB_GAP);
            if (!Clicked(click, bx, tabY, TAB_W, MENU_BH)) continue;
            if      (i == 0) _state = GameState.PlayMenu;
            else if (i == 1) { _state = GameState.SkinConfig; _configScrollY = 0; }
            else if (i == 2) _state = GameState.Shop;
            else             _state = GameState.Challenges;
            break;
        }
    }

    void DrawPlayMenu()
    {
        DrawMenuBg();
        int cx = SW / 2, cy = SH / 2;
        string title = "PLAY";
        TxtBig(title, cx - TxtBigW(title)/2, cy - 160, new Color(100, 140, 255));

        string[] labels = { "LOCAL VS BOT", "MULTIPLAYER" };
        int totalH = labels.Length * (MENU_BH + MENU_GAP) - MENU_GAP;
        int startY = cy - totalH / 2 + 20;
        for (int i = 0; i < labels.Length; i++)
            DrawButton(cx - MENU_BW / 2, startY + i * (MENU_BH + MENU_GAP), MENU_BW, MENU_BH, labels[i]);

        TxtMed("[ESC] BACK", cx - TxtMedW("[ESC] BACK")/2, cy + 120, new Color(100, 110, 160));
    }

    void HandlePlayMenuClick(bool click)
    {
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
                ResetMatch();
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
