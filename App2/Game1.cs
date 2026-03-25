using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

public class Game1 : Game
{
    GraphicsDeviceManager _graphics;
    SpriteBatch           _spriteBatch;
    Texture2D             _pixel;

    bool _debugOpen = false;
    const int DEBUG_W = 285;

    enum GameState { Playing, GameOver, RoundOver }
    GameState _state = GameState.Playing;
    int       _roundWinner = -1;
    string    _lastKillText = "";

    int  _scoreP1 = 0, _scoreP2 = 0;
    const int SCORE_TO_WIN = 3;

    float _roundTime = 90f; // 1.5 Minuten
    bool  _timerRunning = true;

    float _roundOverTimer = 0f;
    const float ROUND_OVER_DELAY = 2.2f;

    float   _camZoom  = 1f;
    float   _camShake = 0f;
    Vector2 _camPos;
    Vector2 _camShakeOffset;

    float _fps, _fpsTimer;
    int   _fpsFrames;

    const float BL = -1050f, BR = 1050f, BT = -680f, BB = 780f;

    List<Platform> _platforms = new();
    List<Particle> _particles = new();
    Player _p1, _p2;

    KeyboardState _prevKeys;
    MouseState    _prevMouse;

    int SW, SH;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        _graphics.IsFullScreen = true;
        _graphics.PreferredBackBufferWidth  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        _graphics.SynchronizeWithVerticalRetrace = false;
        IsFixedTimeStep = false;
    }

    protected override void Initialize()
    {
        base.Initialize();
        SW = GraphicsDevice.Viewport.Width;
        SH = GraphicsDevice.Viewport.Height;
        BuildMap();
        ResetRound();
    }

    void BuildMap()
    {
        _platforms.Clear();

        _platforms.Add(new Platform(-500,  220,  1000, 30, PlatType.Main));

        _platforms.Add(new Platform(-820,  340,   220, 22, PlatType.Side));
        _platforms.Add(new Platform( 600,  340,   220, 22, PlatType.Side));

        _platforms.Add(new Platform(-680,   80,   230, 22, PlatType.Side));
        _platforms.Add(new Platform( 450,   80,   230, 22, PlatType.Side));

        _platforms.Add(new Platform(-155,  -80,   310, 22, PlatType.Side));

        _platforms.Add(new Platform(-820,  -80,   170, 18, PlatType.Small));
        _platforms.Add(new Platform( 650,  -80,   170, 18, PlatType.Small));

        _platforms.Add(new Platform( -90, -260,   180, 18, PlatType.Small));

        _platforms.Add(new Platform(-390,   70,   110, 16, PlatType.Small));
        _platforms.Add(new Platform( 280,   70,   110, 16, PlatType.Small));
    }

    void ResetRound()
    {
        _p1 = new Player(1, new Vector2(-200, 160), new Color(80, 160, 255),
                         Keys.A, Keys.D, Keys.W, Keys.S, Keys.LeftControl, Keys.LeftShift);
        _p2 = new Player(2, new Vector2( 200, 160), new Color(255, 100, 80),
                         Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.RightControl, Keys.RightShift);
        _state       = GameState.Playing;
        _lastKillText = "";
        _roundTime   = 90f;
        _timerRunning = true;
        _roundOverTimer = 0f;
        _particles.Clear();
    }

    void ResetMatch()
    {
        _scoreP1 = 0; _scoreP2 = 0;
        ResetRound();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gt)
    {
        float dt = Math.Min((float)gt.ElapsedGameTime.TotalSeconds, 0.05f);
        var keys  = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool mouseClick = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

        if (keys.IsKeyDown(Keys.Escape)) Exit();
        if (KeyJustPressed(keys, Keys.F1)) _debugOpen = !_debugOpen;

        if (_state == GameState.Playing)
        {
            if (_timerRunning)
            {
                _roundTime -= dt;
                if (_roundTime <= 0f) { _roundTime = 0f; TimerExpired(); }
            }

            _p1.Update(dt, keys, _prevKeys, _platforms, _p2, this, mouseClick);
            _p2.Update(dt, keys, _prevKeys, _platforms, _p1, this, mouseClick);
            CheckBlast(_p1); CheckBlast(_p2);
        }
        else if (_state == GameState.RoundOver)
        {
            _roundOverTimer -= dt;
            if (_roundOverTimer <= 0f)
            {
                if (_scoreP1 >= SCORE_TO_WIN || _scoreP2 >= SCORE_TO_WIN)
                    _state = GameState.GameOver;
                else
                    ResetRound();
            }
        }

        if (_state == GameState.GameOver && KeyJustPressed(keys, Keys.R))
            ResetMatch();

        for (int i = _particles.Count - 1; i >= 0; i--)
        { _particles[i].Update(dt); if (!_particles[i].Alive) _particles.RemoveAt(i); }

        UpdateCamera(dt);

        _fpsFrames++;
        _fpsTimer += dt;
        if (_fpsTimer >= 0.5f) { _fps = _fpsFrames / _fpsTimer; _fpsFrames = 0; _fpsTimer = 0f; }

        _prevKeys  = keys;
        _prevMouse = mouse;
        base.Update(gt);
    }

    void TimerExpired()
    {
        _timerRunning = false;
        int winner = (_p2.Stocks > _p1.Stocks) ? 2 : 1; // Tie → P1
        if (winner == 1) _scoreP1++; else _scoreP2++;
        AwardRound(winner);
    }

    void AwardRound(int winner)
    {
        _roundWinner = winner;
        _lastKillText = winner == 1 ? "P1 WINS ROUND!" : "P2 WINS ROUND!";
        _state = GameState.RoundOver;
        _roundOverTimer = ROUND_OVER_DELAY;
        _timerRunning = false;
    }

    void CheckBlast(Player p)
    {
        if (p.Dead) return;
        if (p.Pos.X < BL || p.Pos.X > BR || p.Pos.Y < BT || p.Pos.Y > BB)
        {
            Player killer = p == _p1 ? _p2 : _p1;
            _lastKillText = $"P{killer.Id} KO'd P{p.Id}!  ({(int)p.Damage}%)";
            SpawnBurst(p.Pos, p.Color, 50);
            AddShake(10f);
            p.LoseStock();
            if (killer.Id == 1) _scoreP1++; else _scoreP2++;
            if (_scoreP1 >= SCORE_TO_WIN || _scoreP2 >= SCORE_TO_WIN)
                AwardRound(killer.Id);
            else
                p.Respawn();
        }
    }

    void UpdateCamera(float dt)
    {
        Vector2 mid = (_p1.Pos + _p2.Pos) * 0.5f;
        _camPos += (mid - _camPos) * 5f * dt;
        float dist = Vector2.Distance(_p1.Pos, _p2.Pos);
        float tz = MathHelper.Clamp(750f / (dist + 480f), 0.45f, 1.05f);
        _camZoom += (tz - _camZoom) * 3f * dt;
        if (_camShake > 0f)
        {
            _camShake -= dt * 22f;
            var r = new Random();
            _camShakeOffset = new Vector2(
                (float)(r.NextDouble() * 2 - 1) * _camShake,
                (float)(r.NextDouble() * 2 - 1) * _camShake);
        }
        else { _camShake = 0f; _camShakeOffset = Vector2.Zero; }
    }

    public void AddShake(float a) => _camShake = Math.Max(_camShake, a);

    public void SpawnHit(Vector2 pos, Color col, int n)  => SpawnBurst(pos, col, n);
    public void SpawnBurst(Vector2 pos, Color col, int n)
    {
        var r = new Random();
        for (int i = 0; i < n; i++)
        {
            float a    = (float)(r.NextDouble() * MathHelper.TwoPi);
            float s    = (float)(r.NextDouble() * 400 + 60);
            float life = (float)(r.NextDouble() * 0.5f + 0.15f);
            _particles.Add(new Particle(pos,
                new Vector2(MathF.Cos(a), MathF.Sin(a)) * s, col, life, (float)(r.NextDouble() * 5 + 2)));
        }
    }

    int GameStartX => _debugOpen ? DEBUG_W : 0;
    int GameW      => SW - GameStartX;

    Vector2 W2S(Vector2 world)
    {
        Vector2 center = new Vector2(GameStartX + GameW / 2f, SH / 2f);
        return (world - _camPos) * _camZoom + center + _camShakeOffset;
    }

    bool KeyJustPressed(KeyboardState cur, Keys k) =>
        cur.IsKeyDown(k) && !_prevKeys.IsKeyDown(k);

    protected override void Draw(GameTime gt)
    {
        GraphicsDevice.Clear(new Color(18, 20, 35));
        _spriteBatch.Begin();

        DrawBackground();
        foreach (var p in _platforms) DrawPlatform(p);
        foreach (var p in _particles) DrawParticle(p);
        DrawPlayer(_p1); DrawPlayer(_p2);
        if (_debugOpen) { DrawAttackHitbox(_p1); DrawAttackHitbox(_p2); }
        DrawHUD();
        if (_state == GameState.RoundOver) DrawRoundOver();
        if (_state == GameState.GameOver)  DrawGameOver();
        if (_debugOpen) DrawDebug();

        _spriteBatch.End();
        base.Draw(gt);
    }

    void DrawBackground()
    {
        R(GameStartX, 0, GameW, SH, new Color(22, 25, 42));
        for (int x = GameStartX; x < SW; x += 80) R(x, 0, 1, SH, new Color(27, 30, 50));
        for (int y = 0; y < SH; y += 80)           R(GameStartX, y, GameW, 1, new Color(27, 30, 50));

        Txt("BRAWLHAVEN", GameStartX + 10, 10, new Color(45, 50, 80));

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
            default: // Small
                body = new Color(40, 46, 78);
                edge = new Color(78, 95, 155);
                bot  = new Color(0, 0, 0, 50);
                break;
        }

        // Schatten
        R((int)s.X + 4, (int)s.Y + 5, w, h, new Color(0, 0, 0, 60));
        // Körper
        R((int)s.X, (int)s.Y, w, h, body);
        // Innere Füllung leicht heller
        R((int)s.X + 2, (int)s.Y + 2, w - 4, h - 4, new Color(body.R + 10, body.G + 10, body.B + 12, 255));
        // Oberkante (Highlight)
        R((int)s.X, (int)s.Y, w, Math.Max(2, (int)(3 * _camZoom)), edge);
        // Unterkante (Schatten)
        R((int)s.X, (int)s.Y + h - 2, w, 2, bot);
        // Seitliche Linien
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

    void DrawPlayer(Player p)
    {
        if (p.Dead) return;
        if (p.Invincible && (int)(p.InvTime * 18) % 2 == 0) return;

        var   feet = W2S(p.Pos);
        float z    = _camZoom;
        Color col  = p.Hitstun > 0 ? Color.White : p.Color;

        int cx  = (int)feet.X;
        int fy  = (int)feet.Y;

        int rx = Math.Max(3, (int)(22f * z * p.SX));
        int ry = Math.Max(3, (int)(19f * z * p.SY));

        float wobble = MathF.Sin(p.WalkTimer * 2.2f) * 1.8f * z;
        rx = Math.Max(3, (int)(rx + wobble));
        ry = Math.Max(3, (int)(ry - wobble * 0.6f));

        int bcy = fy - ry;

        R(cx - rx, fy, rx * 2, Math.Max(1, (int)(5 * z)), new Color(0, 0, 0, 40));
        DrawEllipse(cx, bcy, rx, ry, new Color(0, 0, 0, 65), 3, 3);
        DrawEllipse(cx, bcy, rx, ry, col);

        // Augen
        int es        = Math.Max(2, (int)(4.5f * z));
        int eyeY      = bcy - ry / 6;
        int eyeSpread = Math.Max(2, rx / 3);
        int pupilOff  = p.FacingRight ? Math.Max(1, (int)(1.2f * z)) : -Math.Max(1, (int)(1.2f * z));
        int ps        = Math.Max(1, es / 2);

        DrawEllipse(cx - eyeSpread, eyeY, es, es, Color.White);
        DrawEllipse(cx + eyeSpread, eyeY, es, es, Color.White);
        DrawEllipse(cx - eyeSpread + pupilOff, eyeY + Math.Max(1, (int)z), ps, ps, new Color(15, 30, 100));
        DrawEllipse(cx + eyeSpread + pupilOff, eyeY + Math.Max(1, (int)z), ps, ps, new Color(15, 30, 100));

        if (p.Hitstun > 0)
        {
            int mw = Math.Max(3, (int)(7 * z));
            int my = eyeY + es + Math.Max(1, (int)(3 * z));
            R(cx - mw / 2, my, mw, Math.Max(1, (int)(1.5f * z)), new Color(0, 0, 0, 130));
        }

        // Damage % über Slime
        float pct = p.Damage;
        Color dc  = pct < 50 ? new Color(100, 220, 100) : pct < 100 ? new Color(255, 200, 50) : new Color(255, 70, 70);
        TxtBig($"{(int)pct}%", cx - 12, bcy - ry - 30, dc);

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
        if (!p.AttackActive) return;
        var s = W2S(new Vector2(p.HitboxWorld.X, p.HitboxWorld.Y));
        int w = (int)(p.HitboxWorld.Width  * _camZoom);
        int h = (int)(p.HitboxWorld.Height * _camZoom);
        Color fill = p.IsHeavy ? new Color(255, 100, 50, 55) : new Color(255, 220, 50, 55);
        Color edge = p.IsHeavy ? new Color(255, 130, 60, 210) : new Color(255, 230, 60, 210);
        R((int)s.X, (int)s.Y, w, h, fill);
        R((int)s.X, (int)s.Y, w, 1, edge);
        R((int)s.X, (int)s.Y + h - 1, w, 1, edge);
        R((int)s.X, (int)s.Y, 1, h, edge);
        R((int)s.X + w - 1, (int)s.Y, 1, h, edge);
    }

    void DrawParticle(Particle p)
    {
        var s = W2S(p.Pos);
        int sz = Math.Max(1, (int)(p.Size * _camZoom));
        byte a = (byte)(p.Life / p.MaxLife * 255);
        R((int)s.X - sz/2, (int)s.Y - sz/2, sz, sz, new Color(p.Col.R, p.Col.G, p.Col.B, a));
    }

    void DrawHUD()
    {
        int cx = GameStartX + GameW / 2;
        int hudY = 18;

        int mins = (int)(_roundTime / 60f);
        int secs = (int)(_roundTime % 60f);
        string timeStr = $"{mins}:{secs:D2}";
        Color timeCol = _roundTime > 30f ? new Color(200, 205, 240)
                      : _roundTime > 10f ? new Color(255, 200, 50)
                                         : new Color(255, 70, 70);
        // Timer-Hintergrundpanel
        int tpW = 90, tpH = 28;
        R(cx - tpW/2 - 4, hudY - 4, tpW + 8, tpH + 8, new Color(0, 0, 0, 140));
        R(cx - tpW/2 - 2, hudY - 2, tpW + 4, tpH + 4, new Color(35, 40, 68));
        TxtBig(timeStr, cx - timeStr.Length * 6, hudY + 4, timeCol);

        int scoreOffX = 160;
        DrawScoreBlock(cx - scoreOffX - 80, hudY, _scoreP1, _p1.Color, "P1", true);

        DrawScoreBlock(cx + scoreOffX,       hudY, _scoreP2, _p2.Color, "P2", false);

        if (_lastKillText != "")
            Txt(_lastKillText, cx - _lastKillText.Length * 3, hudY + 48, new Color(255, 190, 80));
    }

    void DrawScoreBlock(int x, int y, int score, Color col, string label, bool leftAlign)
    {
        // Panel
        int pw = 130, ph = 36;
        R(x - 2, y - 2, pw + 4, ph + 4, new Color(0, 0, 0, 140));
        R(x,     y,     pw,     ph,     new Color(22, 26, 45));

        // Label
        Txt(label, leftAlign ? x + 6 : x + pw - 20, y + 4, col);

        // Score-Punkte als Kreise
        int dotR = 7, gap = 20;
        int dotStartX = leftAlign ? x + 30 : x + 8;
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
        Color wc = _roundWinner == 1 ? _p1.Color : _p2.Color;
        TxtBig($"P{_roundWinner} WINS ROUND!", cx - 150, SH/2 - 22, wc);
        string scoreStr = $"SCORE  P1 {_scoreP1} - {_scoreP2} P2";
        Txt(scoreStr, cx - scoreStr.Length * 3, SH/2 + 18, new Color(180, 185, 215));
    }

    void DrawGameOver()
    {
        R(0, 0, SW, SH, new Color(0, 0, 0, 175));
        int cx = GameStartX + GameW / 2;
        Color wc = _scoreP1 >= SCORE_TO_WIN ? _p1.Color : _p2.Color;
        int   pw = _scoreP1 >= SCORE_TO_WIN ? 1 : 2;
        TxtBig($"PLAYER {pw} WINS!", cx - 110, SH/2 - 36, wc);
        string scoreStr = $"FINAL  P1 {_scoreP1} - {_scoreP2} P2";
        Txt(scoreStr, cx - scoreStr.Length * 3, SH/2 + 6, new Color(180, 185, 215));
        Txt("[R] REMATCH", cx - 46, SH/2 + 34, new Color(180, 185, 210));
    }

    void DrawDebug()
    {
        R(0, 0, DEBUG_W, SH, new Color(11, 13, 22, 245));
        R(DEBUG_W - 2, 0, 2, SH, new Color(45, 50, 75));

        int y = 8, x = 8, lh = 17;
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
        Lbl("PLAYER 1  WASD", _p1.Color);
        RowF("Pos X",     _p1.Pos.X,  Color.White);
        RowF("Pos Y",     _p1.Pos.Y,  Color.White);
        RowF("Vel X",     _p1.Vel.X,  MathF.Abs(_p1.Vel.X)>10?new Color(255,200,80):Color.White);
        RowF("Vel Y",     _p1.Vel.Y,  MathF.Abs(_p1.Vel.Y)>10?new Color(255,200,80):Color.White);
        RowF("Damage",    _p1.Damage, _p1.Damage>100?new Color(255,80,80):new Color(100,220,100));
        Row ("Stocks",    $"{_p1.Stocks}/{Player.MAX_STOCKS}", Color.White);
        Row ("State",     _p1.State,  new Color(140,195,255));
        Row ("Grounded",  _p1.Grounded?"YES":"NO", _p1.Grounded?new Color(100,220,100):new Color(255,150,80));
        RowF("Hitstun",   _p1.Hitstun,  _p1.Hitstun>0?new Color(255,80,80):Color.White);
        RowF("Dodge CD",  _p1.DodgeCD,  _p1.DodgeCD>0?new Color(255,200,80):Color.White);
        RowF("Attack CD", _p1.AtkCD,    _p1.AtkCD>0?new Color(255,150,80):Color.White);
        Row ("Invincible",_p1.Invincible?$"YES {_p1.InvTime:F1}s":"NO", _p1.Invincible?new Color(100,220,255):Color.White);
        Sep();
        Lbl("PLAYER 2  ARROWS", _p2.Color);
        RowF("Pos X",     _p2.Pos.X,  Color.White);
        RowF("Pos Y",     _p2.Pos.Y,  Color.White);
        RowF("Vel X",     _p2.Vel.X,  MathF.Abs(_p2.Vel.X)>10?new Color(255,200,80):Color.White);
        RowF("Vel Y",     _p2.Vel.Y,  MathF.Abs(_p2.Vel.Y)>10?new Color(255,200,80):Color.White);
        RowF("Damage",    _p2.Damage, _p2.Damage>100?new Color(255,80,80):new Color(100,220,100));
        Row ("Stocks",    $"{_p2.Stocks}/{Player.MAX_STOCKS}", Color.White);
        Row ("State",     _p2.State,  new Color(255,175,150));
        Row ("Grounded",  _p2.Grounded?"YES":"NO", _p2.Grounded?new Color(100,220,100):new Color(255,150,80));
        RowF("Hitstun",   _p2.Hitstun,  _p2.Hitstun>0?new Color(255,80,80):Color.White);
        RowF("Dodge CD",  _p2.DodgeCD,  _p2.DodgeCD>0?new Color(255,200,80):Color.White);
        RowF("Attack CD", _p2.AtkCD,    _p2.AtkCD>0?new Color(255,150,80):Color.White);
        Row ("Invincible",_p2.Invincible?$"YES {_p2.InvTime:F1}s":"NO", _p2.Invincible?new Color(100,220,255):Color.White);
        Sep();
        Lbl("MATCH", new Color(140,200,100));
        Row("Score",      $"P1 {_scoreP1} - {_scoreP2} P2", Color.White);
        Row("Timer",      $"{(int)(_roundTime/60)}:{(int)(_roundTime%60):D2}", Color.White);
        Row("Particles",  _particles.Count.ToString(), Color.White);
        Row("Game State", _state.ToString(), new Color(200,200,100));
        Sep();
        Lbl("CONTROLS", new Color(140,200,100));
        Row("P1 Move",  "A / D",       new Color(145,150,185));
        Row("P1 Jump",  "W  (x2=DJ)",  new Color(145,150,185));
        Row("P1 Dodge", "S + LCtrl",   new Color(145,150,185));
        Sep();
        Row("P2 Move",  "< / >",       new Color(145,150,185));
        Row("P2 Jump",  "Up (x2=DJ)",  new Color(145,150,185));
        Row("P2 Dodge", "Down+RCtrl",  new Color(145,150,185));
        Sep();
        Row("Attack",   "Left Mouse",  new Color(145,150,185));
        Sep();
        Row("[F1]",  "Toggle Debug",   new Color(80,200,255));
        Row("[R]",   "Rematch",        new Color(80,200,255));
        Row("[ESC]", "Exit",           new Color(80,200,255));
    }
    
    void R(int x, int y, int w, int h, Color c)
    { if (w > 0 && h > 0) _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), c); }

    void Txt(string t, int x, int y, Color c)
    { DrawPx(t, x+1, y+1, new Color(0,0,0,100), 1); DrawPx(t, x, y, c, 1); }

    void TxtBig(string t, int x, int y, Color c)
    { DrawPx(t, x+2, y+2, new Color(0,0,0,100), 2); DrawPx(t, x, y, c, 2); }

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

public class Player
{
    public const float W = 42f, H = 58f;
    public const int   MAX_STOCKS = 3;
    public const float DODGE_CD   = 1.2f;
    public const float GRAVITY    = 2100f;
    public const float JUMP_F     = -790f;
    public const float DJ_F       = -690f;
    public const float SPEED      = 385f;

    public int     Id;
    public Vector2 Pos, Vel;
    public Color   Color;
    public float   Damage;
    public int     Stocks = MAX_STOCKS;
    public int     Jumps  = 2;
    public bool    Grounded, FacingRight = true;
    public float   Hitstun, DodgeCD, AtkCD, InvTime;
    public bool    Invincible => InvTime > 0f;
    public bool    Dead;
    public float   SX = 1f, SY = 1f;
    public float   WalkTimer;
    public string  State = "IDLE";

    public bool    AttackActive;
    public float   AtkTimer;
    public bool    IsHeavy;
    public RectF   HitboxWorld;

    Vector2 _spawn;
    float   _coyote, _jBuf;
    Keys    _kL, _kR, _kU, _kD, _kA, _kH;

    public Player(int id, Vector2 spawn, Color col,
                  Keys l, Keys r, Keys u, Keys d, Keys a, Keys h)
    {
        Id=id; Pos=_spawn=spawn; Color=col;
        _kL=l; _kR=r; _kU=u; _kD=d; _kA=a; _kH=h;
    }

    public void LoseStock()  { Stocks--; Dead = Stocks <= 0; Damage = 0; }
    public void ResetFull()  { Stocks = MAX_STOCKS; Respawn(); }
    public void Respawn()
    {
        Pos=_spawn; Vel=Vector2.Zero; Damage=0;
        Jumps=2; InvTime=2f; Hitstun=0;
        SX=SY=1f; WalkTimer=0f; Dead=false; State="SPAWN";
    }

    public void Update(float dt, KeyboardState keys, KeyboardState prev,
                       List<Platform> plats, Player other, Game1 game, bool mouseAttack)
    {
        if (Dead) return;

        bool l  = keys.IsKeyDown(_kL), r  = keys.IsKeyDown(_kR);
        bool u  = keys.IsKeyDown(_kU), d  = keys.IsKeyDown(_kD);
        bool a  = keys.IsKeyDown(_kA) && !prev.IsKeyDown(_kA);
        bool uj = keys.IsKeyDown(_kU) && !prev.IsKeyDown(_kU);

        if (InvTime > 0) InvTime -= dt;
        if (DodgeCD > 0) DodgeCD -= dt;
        if (AtkCD   > 0) AtkCD   -= dt;
        if (Hitstun > 0) Hitstun -= dt;
        SX += (1f-SX) * 14f*dt;
        SY += (1f-SY) * 14f*dt;

        if (MathF.Abs(Vel.X) > 20f && Grounded) WalkTimer += dt*9f;

        if (AttackActive)
        {
            AtkTimer -= dt;
            if (AtkTimer <= 0) { AttackActive = false; }
            else
            {
                float hw = IsHeavy ? 85f : 58f, hh = IsHeavy ? 52f : 40f;
                float hx = FacingRight ? Pos.X : Pos.X-hw;
                float hy = Pos.Y - H*0.6f;
                HitboxWorld = new RectF(hx, hy, hw, hh);

                if (!other.Invincible && !other.Dead && !other.Hitstun.Equals(0)==false || true)
                {
                    var oBox = new RectF(other.Pos.X-W/2f, other.Pos.Y-H, W, H);
                    if (HitboxWorld.Intersects(oBox) && other.Hitstun<=0 && !other.Invincible)
                    {
                        float dmg    = IsHeavy ? 19f : 8f;
                        float kbBase = IsHeavy ? 620f : 360f;
                        float kb     = kbBase * (1f + other.Damage/55f);
                        float dir    = FacingRight ? 1f : -1f;
                        other.Vel    = new Vector2(dir*kb, kb*(IsHeavy?-0.75f:-0.52f));
                        other.Damage += dmg;
                        other.Hitstun = IsHeavy ? 0.42f : 0.22f;
                        other.SX=0.62f; other.SY=1.42f;
                        game.SpawnHit(new Vector2(other.Pos.X, other.Pos.Y-H/2f), other.Color, IsHeavy?18:9);
                        game.AddShake(IsHeavy?7f:3.5f);
                        AttackActive=false;
                    }
                }
            }
        }

        if (Hitstun > 0)
        {
            Vel.Y += GRAVITY*dt; Pos += Vel*dt;
            DoPhysics(plats); UpdateState(); return;
        }

        float tVX = l ? -SPEED : r ? SPEED : 0f;
        if (l) FacingRight=false;
        if (r) FacingRight=true;

        if (Grounded) Vel.X += (tVX-Vel.X)*0.88f;
        else          Vel.X += (tVX-Vel.X)*dt*7f;

        if (d && a && DodgeCD<=0 && Grounded)
        {
            float dir = r?1f:l?-1f:FacingRight?1f:-1f;
            Vel.X=dir*950f; InvTime=0.32f; DodgeCD=DODGE_CD;
            SX=dir>0?1.5f:0.65f; SY=0.65f;
        }
        else if (mouseAttack && !AttackActive && AtkCD<=0)
        {
            AttackActive=true; IsHeavy=true; AtkTimer=0.30f; AtkCD=0.58f;
            SX=FacingRight?1.52f:0.58f; SY=0.80f;
        }

        if (Grounded) _coyote=0.12f; else _coyote-=dt;
        if (uj) _jBuf=0.10f;         else _jBuf-=dt;

        if (uj)
        {
            if (_coyote>0f)
            { Vel.Y=JUMP_F; Jumps=1; _coyote=0; _jBuf=0; SX=0.70f; SY=1.40f; }
            else if (Jumps>0)
            { Vel.Y=DJ_F; Jumps--; SX=0.70f; SY=1.40f;
              game.SpawnHit(new Vector2(Pos.X,Pos.Y), new Color(160,210,255,160), 5); }
        }

        Vel.Y += GRAVITY*dt; Pos += Vel*dt;
        DoPhysics(plats); UpdateState();
    }

    void DoPhysics(List<Platform> plats)
    {
        bool was=Grounded; Grounded=false;
        foreach (var p in plats)
        {
            var pr = new RectF(Pos.X-W/2f, Pos.Y-H, W, H);
            if (!pr.Intersects(new RectF(p.X,p.Y,p.W,p.H))) continue;
            float oT=pr.Bottom-p.Y, oB=(p.Y+p.H)-pr.Top;
            float oL=pr.Right-p.X,  oR=(p.X+p.W)-pr.Left;
            float mn=Math.Min(Math.Min(oT,oB),Math.Min(oL,oR));
            if      (mn==oT && Vel.Y>=0) { Pos.Y=p.Y;         Vel.Y=0; Grounded=true; Jumps=2; if(!was){SX=1.35f;SY=0.65f;} }
            else if (mn==oB && Vel.Y<0)  { Pos.Y=p.Y+p.H+H;  Vel.Y=0; }
            else if (mn==oL && Vel.X>0)  { Pos.X=p.X-W/2f;   Vel.X=0; }
            else if (mn==oR && Vel.X<0)  { Pos.X=p.X+p.W+W/2f; Vel.X=0; }
        }
    }

    void UpdateState()
    {
        if      (Hitstun>0)       State="HIT";
        else if (AttackActive)    State=IsHeavy?"ATK-HEAVY":"ATK-LIGHT";
        else if (Invincible && DodgeCD>DODGE_CD-0.38f) State="DODGE";
        else if (!Grounded && Vel.Y<0) State=Jumps==0?"DJ":"JUMP";
        else if (!Grounded)       State="FALL";
        else if (MathF.Abs(Vel.X)>20) State="RUN";
        else                      State="IDLE";
    }
}

public struct RectF
{
    public float X,Y,W,H;
    public float Left=>X; public float Right=>X+W;
    public float Top=>Y;  public float Bottom=>Y+H;
    public float Width=>W; public float Height=>H;
    public RectF(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;}
    public bool Intersects(RectF o)=>Left<o.Right&&Right>o.Left&&Top<o.Bottom&&Bottom>o.Top;
}

public enum PlatType { Main, Side, Small }
public class Platform
{
    public float X,Y,W,H; public PlatType Type;
    public Platform(float x,float y,float w,float h,PlatType t){X=x;Y=y;W=w;H=h;Type=t;}
}
public class Particle
{
    public Vector2 Pos,Vel; public Color Col; public float Life,MaxLife,Size;
    public bool Alive=>Life>0;
    public Particle(Vector2 p,Vector2 v,Color c,float l,float s){Pos=p;Vel=v;Col=c;Life=MaxLife=l;Size=s;}
    public void Update(float dt){Pos+=Vel*dt;Vel*=0.88f;Life-=dt;}
}

public static class Glyphs
{
    public static readonly Dictionary<char, ulong> Map = new()
    {
        ['0']=0b_01110_10001_10011_10101_11001_10001_01110UL,
        ['1']=0b_00100_01100_00100_00100_00100_00100_01110UL,
        ['2']=0b_01110_10001_00001_00110_01000_10000_11111UL,
        ['3']=0b_11110_00001_00001_01110_00001_00001_11110UL,
        ['4']=0b_00010_00110_01010_10010_11111_00010_00010UL,
        ['5']=0b_11111_10000_10000_11110_00001_00001_11110UL,
        ['6']=0b_01110_10000_10000_11110_10001_10001_01110UL,
        ['7']=0b_11111_00001_00010_00100_01000_01000_01000UL,
        ['8']=0b_01110_10001_10001_01110_10001_10001_01110UL,
        ['9']=0b_01110_10001_10001_01111_00001_00001_01110UL,
        ['A']=0b_01110_10001_10001_11111_10001_10001_10001UL,
        ['B']=0b_11110_10001_10001_11110_10001_10001_11110UL,
        ['C']=0b_01110_10001_10000_10000_10000_10001_01110UL,
        ['D']=0b_11100_10010_10001_10001_10001_10010_11100UL,
        ['E']=0b_11111_10000_10000_11110_10000_10000_11111UL,
        ['F']=0b_11111_10000_10000_11110_10000_10000_10000UL,
        ['G']=0b_01110_10001_10000_10111_10001_10001_01111UL,
        ['H']=0b_10001_10001_10001_11111_10001_10001_10001UL,
        ['I']=0b_01110_00100_00100_00100_00100_00100_01110UL,
        ['J']=0b_00111_00010_00010_00010_00010_10010_01100UL,
        ['K']=0b_10001_10010_10100_11000_10100_10010_10001UL,
        ['L']=0b_10000_10000_10000_10000_10000_10000_11111UL,
        ['M']=0b_10001_11011_10101_10101_10001_10001_10001UL,
        ['N']=0b_10001_11001_10101_10011_10001_10001_10001UL,
        ['O']=0b_01110_10001_10001_10001_10001_10001_01110UL,
        ['P']=0b_11110_10001_10001_11110_10000_10000_10000UL,
        ['Q']=0b_01110_10001_10001_10001_10101_10010_01101UL,
        ['R']=0b_11110_10001_10001_11110_10100_10010_10001UL,
        ['S']=0b_01111_10000_10000_01110_00001_00001_11110UL,
        ['T']=0b_11111_00100_00100_00100_00100_00100_00100UL,
        ['U']=0b_10001_10001_10001_10001_10001_10001_01110UL,
        ['V']=0b_10001_10001_10001_10001_10001_01010_00100UL,
        ['W']=0b_10001_10001_10001_10101_10101_11011_10001UL,
        ['X']=0b_10001_10001_01010_00100_01010_10001_10001UL,
        ['Y']=0b_10001_10001_01010_00100_00100_00100_00100UL,
        ['Z']=0b_11111_00001_00010_00100_01000_10000_11111UL,
        [' ']=0b_00000_00000_00000_00000_00000_00000_00000UL,
        ['%']=0b_11000_11001_00010_00100_01000_10011_00011UL,
        ['.']=0b_00000_00000_00000_00000_00000_00000_00110UL,
        ['-']=0b_00000_00000_00000_11111_00000_00000_00000UL,
        ['!']=0b_00100_00100_00100_00100_00100_00000_00100UL,
        ['/']=0b_00001_00010_00010_00100_01000_01000_10000UL,
        ['<']=0b_00001_00010_00100_01000_00100_00010_00001UL,
        ['>']=0b_10000_01000_00100_00010_00100_01000_10000UL,
        ['(']=0b_00010_00100_01000_01000_01000_00100_00010UL,
        [')']=0b_01000_00100_00010_00010_00010_00100_01000UL,
        ['+']=0b_00000_00100_00100_11111_00100_00100_00000UL,
        [':']=0b_00000_00110_00110_00000_00110_00110_00000UL,
        ['[']=0b_01110_01000_01000_01000_01000_01000_01110UL,
        [']']=0b_01110_00010_00010_00010_00010_00010_01110UL,
        ['#']=0b_01010_01010_11111_01010_11111_01010_01010UL,
    };
}
