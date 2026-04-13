using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    void DrawHUD()
    {
        int cx = GameStartX + GameW / 2;
        int hudY = 12;

        // ── Timer (center) ────────────────────────────────────────────────
        int mins = (int)(_roundTime / 60f);
        int secs = (int)(_roundTime % 60f);
        string timeStr = $"{mins}:{secs:D2}";
        Color timeCol = _roundTime > 30f ? new Color(200, 205, 240)
                      : _roundTime > 10f ? new Color(255, 200, 50)
                                         : new Color(255, 70, 70);
        int tpW = TxtBigW(timeStr) + 24;
        int tpH = _fontBig.LineSpacing + 10;
        R(cx - tpW/2 - 4, hudY - 4, tpW + 8, tpH + 8, new Color(0, 0, 0, 140));
        R(cx - tpW/2 - 2, hudY - 2, tpW + 4, tpH + 4, new Color(35, 40, 68));
        TxtBig(timeStr, cx - TxtBigW(timeStr)/2, hudY + 5, timeCol);

        // ── Score dots ────────────────────────────────────────────────────
        DrawScoreBlock(cx - 59 - 130, hudY, _scoreP1,    _p1.Color, "P1", true);
        DrawScoreBlock(cx + 59,       hudY, _scoreEnemy, _p2.Color, "P2", false);

        // ── Damage panels (top-left P1, top-right P2) ─────────────────────
        DrawDamagePanel(GameStartX + 10,          10, _p1, true);
        DrawDamagePanel(GameStartX + GameW - 10,  10, _p2, false);

        if (_lastKillText != "")
            Txt(_lastKillText, cx - TxtW(_lastKillText)/2, hudY + tpH + 14, new Color(255, 190, 80));
    }

    void DrawDamagePanel(int edgeX, int y, Player p, bool leftSide)
    {
        string pctStr = $"{(int)p.Damage}%";
        int    panH   = 48;

        var  measured = _fontBig.MeasureString(pctStr);
        int  panW     = Math.Max(90, (int)measured.X + 24);
        int  panX     = leftSide ? edgeX : edgeX - panW;

        R(panX - 2, y - 2, panW + 4, panH + 4, new Color(0, 0, 0, 150));
        R(panX,     y,     panW,     panH,     new Color(30, 35, 58));
        R(panX, y, panW, 2, p.Color);  // colored top border

        Color dc = p.Damage < 50  ? new Color(100, 230, 100)
                 : p.Damage < 100 ? new Color(255, 210, 60)
                                  : new Color(255, 80,  80);

        // zentriert in der Box
        float txtX = panX + (panW - measured.X) / 2f;
        float txtY = y    + (panH - measured.Y) / 2f;
        // Schatten
        _spriteBatch.DrawString(_fontBig, pctStr,
            new Microsoft.Xna.Framework.Vector2(txtX + 2, txtY + 2),
            new Color(0, 0, 0, 120));
        _spriteBatch.DrawString(_fontBig, pctStr,
            new Microsoft.Xna.Framework.Vector2(txtX, txtY), dc);
    }

    void DrawScoreBlock(int x, int y, int score, Color col, string label, bool leftAlign)
    {
        int pw = 130, ph = 36;
        R(x - 2, y - 2, pw + 4, ph + 4, new Color(0, 0, 0, 140));
        R(x,     y,     pw,     ph,     new Color(22, 26, 45));

        int lblY = y + (ph - _fontSmall.LineSpacing) / 2;
        Txt(label, leftAlign ? x + 6 : x + pw - TxtW(label) - 6, lblY, col);

        int dotR = 7, gap = 20;
        int dotStartX = x + pw / 2 - (SCORE_TO_WIN - 1) * gap / 2;
        for (int i = 0; i < SCORE_TO_WIN; i++)
        {
            Color dc = i < score ? col : new Color(35, 40, 62);
            int dx = dotStartX + i * gap;
            int dy = y + ph / 2;
            DrawEllipse(dx, dy, dotR, dotR, new Color(0, 0, 0, 80), 2, 2);
            DrawEllipse(dx, dy, dotR, dotR, dc);
            if (i < score)
                DrawEllipse(dx - 2, dy - 2, dotR / 2, dotR / 2, new Color(255, 255, 255, 60));
        }
    }

    void DrawRoundOver()
    {
        R(0, 0, SW, SH, new Color(0, 0, 0, 110));
        int cx = GameStartX + GameW / 2;
        TxtBig("ROUND OVER", cx - TxtBigW("ROUND OVER")/2, SH/2 - _fontBig.LineSpacing/2, new Color(200, 205, 255));
    }

    void DrawGameOver()
    {
        R(0, 0, SW, SH, new Color(0, 0, 0, 175));
        int cx = GameStartX + GameW / 2;
        if (_roundWinner == 0)
        {
            TxtBig("UNENTSCHIEDEN", cx - TxtBigW("UNENTSCHIEDEN")/2, SH/2 - _fontBig.LineSpacing - 4, new Color(200, 200, 100));
        }
        else
        {
            Color wc = _roundWinner == 1 ? _p1.Color : _p2.Color;
            string winStr = $"PLAYER {_roundWinner} WINS!";
            TxtBig(winStr, cx - TxtBigW(winStr)/2, SH/2 - _fontBig.LineSpacing - 4, wc);
        }
        Txt("[R] MENU", cx - TxtW("[R] MENU")/2, SH/2 + 14, new Color(180, 185, 210));
    }

    void DrawDebug()
    {
        R(0, 0, DEBUG_W, SH, new Color(11, 13, 22, 245));
        R(DEBUG_W - 2, 0, 2, SH, new Color(45, 50, 75));

        int y = 8, x = 8, lh = _fontSmall.LineSpacing + 2;
        void Lbl(string t, Color c) { Txt(t, x, y, c); y += lh; }
        void Sep() { R(x, y + 3, DEBUG_W - 16, 1, new Color(38, 43, 65)); y += lh; }
        void Row(string k, string v, Color vc) {
            Txt(k, x, y, new Color(105, 110, 148));
            Txt(v, x + 155, y, vc);
            y += lh;
        }
        void RowF(string k, float v, Color vc) => Row(k, v.ToString("F2"), vc);

        Lbl("DEBUG  [F1 TOGGLE]", new Color(80, 200, 255));
        Sep();
        Lbl("CAMERA", new Color(140, 200, 100));
        RowF("Pos X", _camPos.X, Color.White);
        RowF("Pos Y", _camPos.Y, Color.White);
        RowF("Zoom",  _camZoom,  Color.White);
        RowF("Shake", _camShake, _camShake > 0.5f ? new Color(255,200,80) : Color.White);
        Row ("FPS",   $"{(int)_fps}", _fps >= 55 ? new Color(80,220,100) : _fps >= 30 ? new Color(255,200,50) : new Color(255,70,70));
        Sep();
        Lbl("PLAYER 1  WASD+LCtrl", _p1.Color);
        RowF("Pos X",    _p1.Pos.X, Color.White);
        RowF("Pos Y",    _p1.Pos.Y, Color.White);
        RowF("Vel X",    _p1.Vel.X, MathF.Abs(_p1.Vel.X)>10?new Color(255,200,80):Color.White);
        RowF("Vel Y",    _p1.Vel.Y, MathF.Abs(_p1.Vel.Y)>10?new Color(255,200,80):Color.White);
        RowF("Damage",   _p1.Damage, _p1.Damage>100?new Color(255,80,80):new Color(100,220,100));
        Row ("Stocks",   $"{_p1.Stocks}/{Player.MAX_STOCKS}", Color.White);
        Row ("State",    _p1.State, new Color(140,195,255));
        RowF("Dodge CD", _p1.DodgeCD, _p1.DodgeCD>0?new Color(255,200,80):Color.White);
        Sep();
        Lbl("PLAYER 2  WASD+LCtrl", _p2.Color);
        RowF("Pos X",   _p2.Pos.X, Color.White);
        RowF("Pos Y",   _p2.Pos.Y, Color.White);
        RowF("Vel X",   _p2.Vel.X, MathF.Abs(_p2.Vel.X)>10?new Color(255,200,80):Color.White);
        RowF("Vel Y",   _p2.Vel.Y, MathF.Abs(_p2.Vel.Y)>10?new Color(255,200,80):Color.White);
        RowF("Damage",  _p2.Damage, _p2.Damage>100?new Color(255,80,80):new Color(100,220,100));
        Row ("Stocks",  $"{_p2.Stocks}/{Player.MAX_STOCKS}", Color.White);
        Row ("State",   _p2.State, new Color(140,195,255));
        Sep();
        Lbl("MATCH", new Color(140,200,100));
        Row("Score",     $"P1 {_scoreP1}  P2 {_scoreEnemy}", Color.White);
        Row("Timer",     $"{(int)(_roundTime/60)}:{(int)(_roundTime%60):D2}", Color.White);
        Row("Particles", _particles.Count.ToString(), Color.White);
        Row("State",     _state.ToString(), new Color(200,200,100));
        Row("Net",       _net.Role.ToString(), new Color(200,200,100));
        Sep();
        Row("[F1]",  "Toggle Debug", new Color(80,200,255));
        Row("[ESC]", "Menu",         new Color(80,200,255));
    }
}
