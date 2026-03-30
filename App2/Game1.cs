using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

public class Game1 : Game
{
    GraphicsDeviceManager _graphics;
    SpriteBatch           _spriteBatch;
    Texture2D             _pixel;

    bool _debugOpen = false;
    const int DEBUG_W = 285;

    enum GameState { Menu, NameEntry, PlayMenu, Shop, Challenges, SkinConfig, Lobby, Playing, GameOver, RoundOver }
    GameState _state = GameState.Menu;
    int       _roundWinner = -1;
    string    _lastKillText = "";

    int  _scoreP1    = 0;
    int  _scoreEnemy = 0;
    const int SCORE_TO_WIN = 3;

    static readonly (string Name, Color Col, int Price)[] SKINS =
    {
        ("DEFAULT", new Color( 80, 160, 255),   0),
        ("CRIMSON", new Color(220,  50,  60), 200),
        ("EMERALD", new Color( 50, 200, 100), 300),
        ("GOLD",    new Color(255, 200,  50), 500),
        ("PURPLE",  new Color(160,  80, 220), 300),
        ("ORANGE",  new Color(255, 140,  30), 200),
        ("PINK",    new Color(255, 100, 180), 500),
        ("CYAN",    new Color( 50, 220, 220), 300),
        ("SCARLET", new Color(255, 100,  80),   0),
    };

    static readonly (string Title, string Desc, int Target, int Coins)[] CHALLENGES =
    {
        ("FIRST WIN",   "Win your first match",        1,  100),
        ("HIT MACHINE", "Land 30 hits",               30,  200),
        ("VETERAN",     "Win 10 matches",             10,  500),
        ("UNTOUCHABLE", "Win without getting blasted",  1,  300),
        ("POWERHOUSE",  "Deal 500 total damage",      500,  400),
        ("RELENTLESS",  "Win 3 times in a row",         3,  350),
        ("SPEED RUN",   "Win in under 60 seconds",      1,  250),
    };

    string _playerName      = "";
    int    _mySkin          = 0;
    int    _coins           = 0;
    int    _ownedSkins      = (1 << 0) | (1 << 8);  // DEFAULT + SCARLET always owned
    int    _chalClaimed     = 0;
    int    _statWins        = 0;
    int    _statHits        = 0;
    int    _statDmgDealt    = 0;
    int    _statBestStreak  = 0;
    int    _statPerfectWins = 0;
    int    _statFastWin     = 0;
    int    _statCurStreak   = 0;

    Vector2 _mousePos;

    float _roundTime = 120f;
    bool  _timerRunning = true;

    float _roundOverTimer = 0f;
    const float ROUND_OVER_DELAY = 2.2f;
    float _killTextTimer = 0f;

    float   _camZoom  = 1f;
    float   _camShake = 0f;
    Vector2 _camPos;
    Vector2 _camShakeOffset;

    float _fps, _fpsTimer;
    int   _fpsFrames;

    const float BL = -1050f, BR = 1050f, BT = -680f, BB = 780f;

    List<Platform> _platforms = new();
    List<Particle> _particles = new();
    Player _p1;
    Player _p2;

    GameNet       _net          = new GameNet();
    bool          _isLocalMode  = false;
    BotController _bot          = new BotController();
    int           _myCode       = 0;
    string        _codeInput    = "";

    float _syncTimer = 0f;
    static readonly Random _rng = new Random();

    KeyboardState _prevKeys;
    MouseState    _prevMouse;

    int SW, SH;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.IsFullScreen = true;
        _graphics.PreferredBackBufferWidth  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        _graphics.SynchronizeWithVerticalRetrace = false;
        IsFixedTimeStep = false;

        Window.TextInput += OnTextInput;
    }

    protected override void Initialize()
    {
        Logger.Init();
        base.Initialize();
        SW = GraphicsDevice.Viewport.Width;
        SH = GraphicsDevice.Viewport.Height;
        Logger.Log($"Viewport: {SW}x{SH}");
        Logger.Log($"Adapter:  {GraphicsDevice.Adapter.Description}");
        LoadSave();
        BuildMap();
        ResetRound();
        _state = _playerName == "" ? GameState.NameEntry : GameState.Menu;
        Logger.Log("Initialize abgeschlossen – Zustand: Menu");
    }

    void BuildMap()
    {
        _platforms.Clear();

        _platforms.Add(new Platform(-500,  220, 1000, 30, PlatType.Main));

        _platforms.Add(new Platform(-820,  340,  220, 22, PlatType.Side));
        _platforms.Add(new Platform( 600,  340,  220, 22, PlatType.Side));

        _platforms.Add(new Platform(-680,   80,  230, 22, PlatType.Side));
        _platforms.Add(new Platform( 450,   80,  230, 22, PlatType.Side));

        _platforms.Add(new Platform(-155,  -80,  310, 22, PlatType.Side));

        _platforms.Add(new Platform(-820,  -80,  170, 18, PlatType.Small));
        _platforms.Add(new Platform( 650,  -80,  170, 18, PlatType.Small));

        _platforms.Add(new Platform( -90, -260,  180, 18, PlatType.Small));

        _platforms.Add(new Platform(-390,   70,  110, 16, PlatType.Small));
        _platforms.Add(new Platform( 280,   70,  110, 16, PlatType.Small));
    }

    void ResetRound()
    {
        _p1 = new Player(1, new Vector2(-200, 160), SKINS[_mySkin].Col);
        _p2 = new Player(2, new Vector2( 200, 160), SKINS[8].Col);
        _state          = GameState.Playing;
        _lastKillText   = "";
        _roundTime      = 120f;
        _timerRunning   = true;
        _roundOverTimer = 0f;
        _syncTimer      = 0f;
        _particles.Clear();
    }

    void ResetMatch()
    {
        _scoreP1    = 0;
        _scoreEnemy = 0;
        ResetRound();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        SaveGame();
        Logger.Close();
        base.OnExiting(sender, args);
    }

    protected override void Update(GameTime gt)
    {
        try { UpdateInner(gt); }
        catch (Exception ex)
        {
            Logger.LogException("Update", ex);
            if (_p1 != null) Logger.Log($"P1 Pos={_p1.Pos} Vel={_p1.Vel} State={_p1.State}");
            if (_p2 != null) Logger.Log($"P2 Pos={_p2.Pos} Vel={_p2.Vel} State={_p2.State}");
            Logger.Log($"GameState={_state} Particles={_particles.Count}");
            Logger.Close();
            throw;
        }
    }

    void UpdateInner(GameTime gt)
    {
        float dt = Math.Min((float)gt.ElapsedGameTime.TotalSeconds, 0.05f);
        var keys  = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool mouseClick = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

        _mousePos = new Vector2(mouse.X, mouse.Y);

        // Escape → immer zurück ins Menü, Spiel nie schließen
        if (KeyJustPressed(keys, Keys.Escape) && _state != GameState.Menu && _state != GameState.NameEntry)
        {
            if (_state == GameState.PlayMenu || _state == GameState.Shop || _state == GameState.Challenges || _state == GameState.SkinConfig)
            {
                _state = GameState.Menu;
            }
            else
            {
                if (!_isLocalMode) { _net.Dispose(); _net = new GameNet(); }
                _isLocalMode = false;
                _state = GameState.Menu;
            }
        }
        if (KeyJustPressed(keys, Keys.F1)) _debugOpen = !_debugOpen;

        if (_state == GameState.NameEntry)
        {
            if (KeyJustPressed(keys, Keys.Enter) && _playerName.Length >= 1)
            {
                SaveGame();
                _state = GameState.Menu;
            }
        }
        else if (_state == GameState.Menu)
        {
            HandleMenuClick(mouseClick);
        }
        else if (_state == GameState.PlayMenu)
        {
            HandlePlayMenuClick(mouseClick);
        }
        else if (_state == GameState.Shop)
        {
            HandleShopClick(mouseClick);
        }
        else if (_state == GameState.Challenges)
        {
            HandleChallengesClick(mouseClick);
        }
        else if (_state == GameState.SkinConfig)
        {
            HandleSkinConfigClick(mouseClick);
        }
        else if (_state == GameState.Lobby)
        {
            _net.Update(dt);
            _net.ReceiveInput();

            // Press Enter with 4-digit code → start searching
            if (!_net.IsSeeking && KeyJustPressed(keys, Keys.Enter) && _codeInput.Length == 4)
            {
                if (int.TryParse(_codeInput, out int joinCode))
                    _net.TryJoin(joinCode);
            }

            if (_net.Connected) ResetRound();
        }
        else if (_state == GameState.Playing)
        {
            if (_timerRunning)
            {
                _roundTime -= dt;
                if (_roundTime <= 0f) { _roundTime = 0f; TimerExpired(); }
            }

            // P1: WASD + Mausklick (Angriff) + LCtrl (Dodge)
            byte p1AimAngle = MouseAimAngle(_p1);
            PlayerInput p1Local = PlayerInput.FromKeyboard(keys, _prevKeys, mouseClick, p1AimAngle,
                Keys.A, Keys.D, Keys.W, Keys.S, Keys.LeftControl);

            PlayerInput p1Input, p2Input;
            if (_isLocalMode)
            {
                // P2 = Bot
                p1Input = p1Local;
                p2Input = _bot.Update(dt, _p2, _p1);
            }
            else
            {
                // Multiplayer via Netz
                byte p2AimAngle = MouseAimAngle(_p2);
                PlayerInput p2Local = PlayerInput.FromKeyboard(keys, _prevKeys, mouseClick, p2AimAngle,
                    Keys.A, Keys.D, Keys.W, Keys.S, Keys.LeftControl);

                _net.Update(dt);
                PlayerInput myInput = _net.Role == NetRole.Host ? p1Local : p2Local;
                _net.SendInput(myInput);
                PlayerInput remote = _net.ReceiveInput();
                p1Input = _net.Role == NetRole.Host ? p1Local : remote;
                p2Input = _net.Role == NetRole.Host ? remote  : p2Local;

                if (_net.ConsumePendingSync(out var syncData))
                    ApplySyncFromBytes(syncData);

                // Hit-Event vom anderen Gerät empfangen und anwenden
                if (_net.ConsumePendingHit(out int hitTarget, out Vector2 hitVel,
                                           out float hitDmg, out float hitHistun))
                {
                    var victim = hitTarget == 1 ? _p1 : _p2;
                    if (!victim.Invincible && victim.Hitstun <= 0)
                    {
                        victim.Vel     = hitVel;
                        victim.Damage += hitDmg;
                        victim.Hitstun = hitHistun;
                        victim.SX = 0.62f; victim.SY = 1.42f;
                        SpawnHitFlash(new Vector2(victim.Pos.X, victim.Pos.Y - Player.H / 2f),
                                      victim.Color, hitDmg >= 15f);
                        AddShake(hitDmg >= 15f ? 7f : 3.5f);
                    }
                }

                // Beide Seiten senden ihren eigenen Spielerzustand alle 0.1s
                _syncTimer -= dt;
                if (_syncTimer <= 0f)
                {
                    _net.SendStateSync(
                        _p1.Pos, _p1.Vel, _p1.Damage, _p1.Stocks,
                        _p2.Pos, _p2.Vel, _p2.Damage, _p2.Stocks,
                        _scoreP1, _scoreEnemy);
                    _syncTimer = 0.1f;
                }

                // OnHitLanded einmalig setzen (neue Spielerinstanzen nach ResetRound haben null)
                if (_p1.OnHitLanded == null)
                {
                    _p1.OnHitLanded = (v, vel, dmg, hs) =>
                    {
                        _statHits++; _statDmgDealt += (int)dmg;
                        _net.SendHit(v.Id, vel, dmg, hs);
                    };
                    _p2.OnHitLanded = (v, vel, dmg, hs) => _net.SendHit(v.Id, vel, dmg, hs);
                }
            }

            // Local mode: track hits for stats
            if (_isLocalMode && _p1.OnHitLanded == null)
                _p1.OnHitLanded = (v, vel, dmg, hs) => { _statHits++; _statDmgDealt += (int)dmg; };

            // Host: p1 ist lokal → p1 Hits autoritativ; p2 Hits kommen als Hit-Event vom Client
            // Client: p2 ist lokal → p2 Hits autoritativ; p1 Hits kommen als Hit-Event vom Host
            bool p1Auth = _isLocalMode || _net.Role == NetRole.Host;
            bool p2Auth = _isLocalMode || _net.Role == NetRole.Client;
            _p1.Update(dt, p1Input, _platforms, _p2, this, p1Auth);
            _p2.Update(dt, p2Input, _platforms, _p1, this, p2Auth);
            CheckBlast(_p1);
            CheckBlast(_p2);
            if (_killTextTimer > 0f) { _killTextTimer -= dt; if (_killTextTimer <= 0f) _lastKillText = ""; }
        }
        else if (_state == GameState.RoundOver)
        {
            _roundOverTimer -= dt;
            if (_roundOverTimer <= 0f) ResetRound();
        }

        if (_state == GameState.GameOver && KeyJustPressed(keys, Keys.R))
        {
            ResetMatch();
            _state = GameState.Menu;
        }

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

    void OnTextInput(object sender, TextInputEventArgs e)
    {
        char c = e.Character;
        if (_state == GameState.NameEntry)
        {
            if (c == '\b' && _playerName.Length > 0)
                _playerName = _playerName[..^1];
            else if (!char.IsControl(c) && _playerName.Length < 12)
                _playerName += c;
            return;
        }
        if (_state != GameState.Lobby) return;
        if (_net.IsSeeking) return;
        if (c == '\b' && _codeInput.Length > 0)
            _codeInput = _codeInput[..^1];
        else if (char.IsDigit(c) && _codeInput.Length < 4)
            _codeInput += c;
    }

    void ApplySyncFromBytes(byte[] d)
    {
        if (d == null || d.Length < 40 || _p1 == null || _p2 == null) return;
        int i = 2;
        float RF() { float v = BitConverter.ToSingle(d, i); i += 4; return v; }
        float ax = RF(), ay = RF(), avx = RF(), avy = RF();
        byte p1Dmg = d[i++]; byte p1Stocks = d[i++];
        float bx = RF(), by = RF(), bvx = RF(), bvy = RF();
        byte p2Dmg = d[i++]; byte p2Stocks = d[i++];
        int sP1 = d[i++]; int sP2 = d[i];

        if (_net.Role == NetRole.Host)
        {
            // Empfangen vom Client: nur p2 (der Spieler des Clients) korrigieren
            _p2.Pos = Vector2.Lerp(_p2.Pos, new Vector2(bx, by), 0.6f);
            _p2.Vel = Vector2.Lerp(_p2.Vel, new Vector2(bvx, bvy), 0.5f);
            _p2.Damage = p2Dmg;
            _p2.Stocks = p2Stocks;
        }
        else
        {
            // Empfangen vom Host: nur p1 (der Spieler des Hosts) korrigieren
            _p1.Pos = Vector2.Lerp(_p1.Pos, new Vector2(ax, ay), 0.6f);
            _p1.Vel = Vector2.Lerp(_p1.Vel, new Vector2(avx, avy), 0.5f);
            _p1.Damage = p1Dmg;
            _p1.Stocks = p1Stocks;
            _scoreP1    = sP1;
            _scoreEnemy = sP2;
        }
    }

    void TimerExpired()
    {
        _timerRunning = false;
        if      (_scoreP1 > _scoreEnemy) EndGame(1);
        else if (_scoreEnemy > _scoreP1) EndGame(2);
        else                             EndGame(0);
    }

    void EndGame(int winner)
    {
        _roundWinner = winner;
        _state       = GameState.GameOver;

        bool localWon = _isLocalMode ? winner == 1
                      : _net.Role == NetRole.Host ? winner == 1 : winner == 2;

        if (localWon)
        {
            _statWins++;
            _statCurStreak++;
            if (_statCurStreak > _statBestStreak) _statBestStreak = _statCurStreak;
            if (_scoreEnemy == 0)   _statPerfectWins++;
            if (_roundTime > 60f)   _statFastWin = 1;
            _coins += 30;
        }
        else if (winner != 0)
        {
            _statCurStreak = 0;
        }

        SaveGame();
    }

    void CheckBlast(Player p)
    {
        // Spieler gerade gespawnt (InvTime frisch = 2f) → kein Doppelwertung
        if (p.Dead || p.InvTime > 1.8f) return;
        if (p.Pos.X < BL || p.Pos.X > BR || p.Pos.Y < BT || p.Pos.Y > BB)
        {
            SpawnBurst(p.Pos, p.Color, 40);
            AddShake(8f);

            int winner = p.Id == 1 ? 2 : 1;
            if (winner == 1) _scoreP1++;
            else             _scoreEnemy++;

            _lastKillText  = winner == 1 ? "P1 SCORES!" : "P2 SCORES!";
            _killTextTimer = 2.5f;
            _coins += 10;

            if (_scoreP1 >= SCORE_TO_WIN)    { p.Dead = true; EndGame(1); return; }
            if (_scoreEnemy >= SCORE_TO_WIN) { p.Dead = true; EndGame(2); return; }

            p.Respawn();
        }
    }

    void UpdateCamera(float dt)
    {
        Vector2 target = _p1 != null && _p2 != null
            ? (_p1.Pos + _p2.Pos) * 0.5f
            : _p1?.Pos ?? Vector2.Zero;
        _camPos += (target - _camPos) * 5f * dt;

        float tz = 1.2f;
        if (_p1 != null && _p2 != null)
        {
            float sep = Vector2.Distance(_p1.Pos, _p2.Pos);
            tz = MathHelper.Clamp(1.2f - sep / 2200f, 0.55f, 1.2f);
        }
        _camZoom += (tz - _camZoom) * 3f * dt;

        if (_camShake > 0f)
        {
            _camShake -= dt * 22f;
            _camShakeOffset = new Vector2(
                (float)(_rng.NextDouble() * 2 - 1) * _camShake,
                (float)(_rng.NextDouble() * 2 - 1) * _camShake);
        }
        else { _camShake = 0f; _camShakeOffset = Vector2.Zero; }
    }

    public void AddShake(float a) => _camShake = Math.Max(_camShake, a);

    public void SpawnHit(Vector2 pos, Color col, int n)  => SpawnBurst(pos, col, n);

    // Sichtbarer Treffereffekt: farbige Partikel + weißer Flash
    public void SpawnHitFlash(Vector2 pos, Color col, bool heavy)
    {
        int n = heavy ? 28 : 15;
        SpawnBurst(pos, col, n);
        int flashCount = heavy ? 10 : 5;
        for (int i = 0; i < flashCount; i++)
        {
            float a    = (float)(_rng.NextDouble() * MathHelper.TwoPi);
            float s    = (float)(_rng.NextDouble() * 250 + 80);
            _particles.Add(new Particle(pos,
                new Vector2(MathF.Cos(a), MathF.Sin(a)) * s,
                Color.White, heavy ? 0.22f : 0.14f, heavy ? 9f : 6f));
        }
    }

    public void SpawnBurst(Vector2 pos, Color col, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a    = (float)(_rng.NextDouble() * MathHelper.TwoPi);
            float s    = (float)(_rng.NextDouble() * 400 + 60);
            float life = (float)(_rng.NextDouble() * 0.5f + 0.15f);
            _particles.Add(new Particle(pos,
                new Vector2(MathF.Cos(a), MathF.Sin(a)) * s, col, life, (float)(_rng.NextDouble() * 5 + 2)));
        }
    }

    // Aim-Winkel aus Mausposition relativ zum Spieler (Multiplayer)
    byte MouseAimAngle(Player p)
    {
        Vector2 pScreen = W2S(p.Pos);
        float dx = _mousePos.X - pScreen.X;
        float dy = _mousePos.Y - pScreen.Y;
        if (dx == 0f && dy == 0f) dx = 1f;
        float angle = MathF.Atan2(dy, dx);
        return (byte)((int)((angle / (MathF.PI * 2f) + 1f) * 256f) & 0xFF);
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

    int ChalProgress(int i) => i switch
    {
        0 => _statWins,
        1 => _statHits,
        2 => _statWins,
        3 => _statPerfectWins,
        4 => _statDmgDealt,
        5 => _statBestStreak,
        6 => _statFastWin,
        _ => 0
    };

    bool SkinOwned(int skin) => skin == 0 || skin == 8 || (_ownedSkins & (1 << skin)) != 0;

    protected override void Draw(GameTime gt)
    {
        GraphicsDevice.Clear(new Color(18, 20, 35));
        _spriteBatch.Begin();

        if (_state == GameState.NameEntry)
        {
            DrawNameEntry();
        }
        else if (_state == GameState.Menu)
        {
            DrawMenu();
        }
        else if (_state == GameState.PlayMenu)
        {
            DrawPlayMenu();
        }
        else if (_state == GameState.Shop)
        {
            DrawShop();
        }
        else if (_state == GameState.Challenges)
        {
            DrawChallenges();
        }
        else if (_state == GameState.SkinConfig)
        {
            DrawSkinConfig();
        }
        else if (_state == GameState.Lobby)
        {
            DrawLobby();
        }
        else
        {
            DrawBackground();
            foreach (var p in _platforms) DrawPlatform(p);
            foreach (var p in _particles) DrawParticle(p);
            DrawPlayer(_p1);
            DrawPlayer(_p2);
            DrawAttackAnim(_p1);
            DrawAttackAnim(_p2);
            if (_debugOpen) { DrawAttackHitbox(_p1); DrawAttackHitbox(_p2); }
            DrawHUD();
            if (_state == GameState.RoundOver) DrawRoundOver();
            if (_state == GameState.GameOver)  DrawGameOver();
            if (_debugOpen) DrawDebug();
        }

        _spriteBatch.End();
        base.Draw(gt);
    }

    void DrawBackground()
    {
        R(GameStartX, 0, GameW, SH, new Color(22, 25, 42));
        for (int x = GameStartX; x < SW; x += 80) R(x, 0, 1, SH, new Color(27, 30, 50));
        for (int y = 0; y < SH; y += 80)           R(GameStartX, y, GameW, 1, new Color(27, 30, 50));

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
        DrawEllipse(cx, bcy, rx, ry, col);
        DrawEllipse(cx, bcy + ry / 3, Math.Max(1, rx - (int)(3*z)), Math.Max(1, ry / 2),
                    new Color(0, 0, 0, 30));

        int hlRx = Math.Max(1, (int)(rx * 0.42f));
        int hlRy = Math.Max(1, (int)(ry * 0.35f));
        DrawEllipse(cx - (int)(rx * 0.25f), bcy - (int)(ry * 0.32f), hlRx, hlRy,
                    new Color(255, 255, 255, 60));

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

        if (p.Hitstun > 0)
        {
            int mw = Math.Max(3, (int)(7 * z));
            int my = eyeY + es + Math.Max(1, (int)(3 * z));
            R(cx - mw / 2, my, mw, Math.Max(1, (int)(1.5f * z)), new Color(0, 0, 0, 130));
        }

        string nameTag = p.Id == 1 ? _playerName : (_isLocalMode ? "BOT" : "P2");
        if (nameTag.Length > 0)
            TxtBig(nameTag, cx - nameTag.Length * 6, bcy - ry - 30, p.Color);

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
        int tpW = 90, tpH = 28;
        R(cx - tpW/2 - 4, hudY - 4, tpW + 8, tpH + 8, new Color(0, 0, 0, 140));
        R(cx - tpW/2 - 2, hudY - 2, tpW + 4, tpH + 4, new Color(35, 40, 68));
        TxtBig(timeStr, cx - timeStr.Length * 6, hudY + 4, timeCol);

        // ── Score dots ────────────────────────────────────────────────────
        DrawScoreBlock(cx - 59 - 130, hudY, _scoreP1,    _p1.Color, "P1", true);
        DrawScoreBlock(cx + 59,       hudY, _scoreEnemy, _p2.Color, "P2", false);

        // ── Damage panels (top-left P1, top-right P2) ─────────────────────
        DrawDamagePanel(GameStartX + 10,          10, _p1, true);
        DrawDamagePanel(GameStartX + GameW - 10,  10, _p2, false);

        if (_lastKillText != "")
            Txt(_lastKillText, cx - _lastKillText.Length * 3, hudY + 48, new Color(255, 190, 80));
    }

    void DrawDamagePanel(int edgeX, int y, Player p, bool leftSide)
    {
        string pctStr = $"{(int)p.Damage}%";
        int    panW   = Math.Max(80, pctStr.Length * 12 + 20);
        int    panH   = 44;
        int    panX   = leftSide ? edgeX : edgeX - panW;

        R(panX - 2, y - 2, panW + 4, panH + 4, new Color(0, 0, 0, 150));
        R(panX,     y,     panW,     panH,     new Color(18, 22, 40));
        R(panX, y, panW, 2, p.Color);  // colored top border

        Color dc = p.Damage < 50  ? new Color(100, 230, 100)
                 : p.Damage < 100 ? new Color(255, 210, 60)
                                  : new Color(255, 80,  80);
        int txtX = leftSide ? panX + 8 : panX + panW - pctStr.Length * 12 - 8;
        TxtBig(pctStr, txtX, y + 6, dc);

        // Stock dots at bottom of panel
        int dotR = 5, dotGap = 14;
        int dotsW = Player.MAX_STOCKS * dotGap - (dotGap - dotR * 2);
        int dotsX = panX + panW / 2 - dotsW / 2;
        for (int i = 0; i < Player.MAX_STOCKS; i++)
        {
            Color dc2 = i < p.Stocks ? p.Color : new Color(35, 40, 60);
            DrawEllipse(dotsX + i * dotGap, y + panH - 8, dotR, dotR, dc2);
        }
    }

    void DrawScoreBlock(int x, int y, int score, Color col, string label, bool leftAlign)
    {
        int pw = 130, ph = 36;
        R(x - 2, y - 2, pw + 4, ph + 4, new Color(0, 0, 0, 140));
        R(x,     y,     pw,     ph,     new Color(22, 26, 45));

        Txt(label, leftAlign ? x + 6 : x + pw - 20, y + 4, col);

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
        TxtBig("ROUND OVER", cx - 90, SH/2 - 22, new Color(200, 205, 255));
    }

    void DrawGameOver()
    {
        R(0, 0, SW, SH, new Color(0, 0, 0, 175));
        int cx = GameStartX + GameW / 2;
        if (_roundWinner == 0)
        {
            TxtBig("UNENTSCHIEDEN", cx - 104, SH/2 - 36, new Color(200, 200, 100));
        }
        else
        {
            Color wc = _roundWinner == 1 ? _p1.Color : _p2.Color;
            TxtBig($"PLAYER {_roundWinner} WINS!", cx - 110, SH/2 - 36, wc);
        }
        Txt("[R] MENU", cx - 36, SH/2 + 18, new Color(180, 185, 210));
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

    // ── Menu ─────────────────────────────────────────────────────────────────

    const int MENU_BW = 220, MENU_BH = 56, MENU_GAP = 22;

    void DrawMenuBg()
    {
        R(0, 0, SW, SH, new Color(18, 20, 35));
        for (int x = 0; x < SW; x += 80) R(x, 0, 1, SH, new Color(27, 30, 50));
        for (int y = 0; y < SH; y += 80) R(0, y, SW, 1, new Color(27, 30, 50));
    }

    bool DrawButton(int bx, int by, int bw, int bh, string label)
    {
        bool hover = _mousePos.X >= bx && _mousePos.X <= bx + bw &&
                     _mousePos.Y >= by && _mousePos.Y <= by + bh;
        Color bg     = hover ? new Color(50, 70, 130)   : new Color(26, 30, 58);
        Color border = hover ? new Color(110, 150, 255)  : new Color(50, 60, 100);
        Color tc     = hover ? Color.White               : new Color(160, 170, 210);
        R(bx + 3, by + 3, bw, bh, new Color(0, 0, 0, 60));
        R(bx, by, bw, bh, bg);
        R(bx, by, bw, 2, border);
        R(bx, by + bh - 2, bw, 2, border);
        R(bx, by, 2, bh, border);
        R(bx + bw - 2, by, 2, bh, border);
        TxtBig(label, bx + bw / 2 - label.Length * 6, by + bh / 2 - 7, tc);
        return hover;
    }

    bool Clicked(bool click, int bx, int by, int bw, int bh) =>
        click && _mousePos.X >= bx && _mousePos.X <= bx + bw &&
                 _mousePos.Y >= by && _mousePos.Y <= by + bh;

    void DrawMenu()
    {
        DrawMenuBg();
        int cx = SW / 2, cy = SH / 2;
        string title = "BRAWLHAVEN";
        TxtBig(title, cx - title.Length * 6, cy - 160, new Color(100, 140, 255));

        string[] labels = { "PLAY", "SHOP", "CHALLENGES", "CONFIG" };
        int totalH = labels.Length * (MENU_BH + MENU_GAP) - MENU_GAP;
        int startY = cy - totalH / 2 + 10;
        for (int i = 0; i < labels.Length; i++)
            DrawButton(cx - MENU_BW / 2, startY + i * (MENU_BH + MENU_GAP), MENU_BW, MENU_BH, labels[i]);

        string coinsStr = $"COINS: {_coins}";
        Txt(coinsStr, cx - coinsStr.Length * 3, startY + labels.Length * (MENU_BH + MENU_GAP) + 16, new Color(255, 200, 50));

        string greet = $"WELCOME, {_playerName}";
        Txt(greet, cx - greet.Length * 3, startY - 30, new Color(140, 150, 200));

        string fps = $"FPS {(int)_fps}";
        Txt(fps, SW - fps.Length * 6 - 10, 10, new Color(60, 70, 100));
    }

    void HandleMenuClick(bool click)
    {
        if (!click) return;
        int cx = SW / 2, cy = SH / 2;
        string[] labels = { "PLAY", "SHOP", "CHALLENGES", "CONFIG" };
        int totalH = labels.Length * (MENU_BH + MENU_GAP) - MENU_GAP;
        int startY = cy - totalH / 2 + 10;
        for (int i = 0; i < labels.Length; i++)
        {
            int bx = cx - MENU_BW / 2, by = startY + i * (MENU_BH + MENU_GAP);
            if (!Clicked(click, bx, by, MENU_BW, MENU_BH)) continue;
            if      (i == 0) _state = GameState.PlayMenu;
            else if (i == 1) _state = GameState.Shop;
            else if (i == 2) _state = GameState.Challenges;
            else             _state = GameState.SkinConfig;
            break;
        }
    }

    void DrawPlayMenu()
    {
        DrawMenuBg();
        int cx = SW / 2, cy = SH / 2;
        string title = "PLAY";
        TxtBig(title, cx - title.Length * 6, cy - 160, new Color(100, 140, 255));

        string[] labels = { "LOCAL VS BOT", "MULTIPLAYER" };
        int totalH = labels.Length * (MENU_BH + MENU_GAP) - MENU_GAP;
        int startY = cy - totalH / 2 + 20;
        for (int i = 0; i < labels.Length; i++)
            DrawButton(cx - MENU_BW / 2, startY + i * (MENU_BH + MENU_GAP), MENU_BW, MENU_BH, labels[i]);

        Txt("[ESC] BACK", cx - 30, cy + 120, new Color(100, 110, 160));
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
        TxtBig(title, cx - title.Length * 6, cy - 180, new Color(100, 140, 255));

        Txt("ENTER YOUR NAME", cx - 45, cy - 100, new Color(160, 170, 210));

        int bw = 200, bh = 40;
        int bx = cx - bw / 2, by = cy - 60;
        Color boxEdge = _playerName.Length >= 12 ? new Color(255, 160, 50) : new Color(80, 140, 255, 200);
        R(bx - 2, by - 2, bw + 4, bh + 4, boxEdge);
        R(bx, by, bw, bh, new Color(22, 26, 45));
        string display = _playerName.Length > 0 ? _playerName : "_";
        TxtBig(display, bx + 10, by + bh / 2 - 7, Color.White);

        if (_playerName.Length >= 1)
            Txt("[ENTER] CONFIRM", cx - 45, cy + 10, new Color(100, 220, 100));
        else
            Txt("TYPE A NAME  (MAX 12 CHARS)", cx - 81, cy + 10, new Color(70, 80, 120));

        Txt("YOUR NAME APPEARS UNDER YOUR CHARACTER", cx - 111, cy + 32, new Color(60, 70, 110));
    }

    void DrawChallenges()
    {
        DrawMenuBg();
        int cx = SW / 2;
        string title = "CHALLENGES";
        TxtBig(title, cx - title.Length * 6, 36, new Color(100, 140, 255));

        const int tileW = 560, tileH = 68, gapY = 10;
        int startX = cx - tileW / 2;
        int startY = 90;

        for (int i = 0; i < CHALLENGES.Length; i++)
        {
            var  ch       = CHALLENGES[i];
            int  ty       = startY + i * (tileH + gapY);
            int  progress = ChalProgress(i);
            bool complete = progress >= ch.Target;
            bool claimed  = (_chalClaimed & (1 << i)) != 0;
            Color bg   = claimed ? new Color(22, 35, 22) : complete ? new Color(28, 40, 55) : new Color(22, 24, 42);
            Color edge = claimed ? new Color(60, 140, 60) : complete ? new Color(100, 160, 255) : new Color(45, 50, 80);
            R(startX + 3, ty + 3, tileW, tileH, new Color(0, 0, 0, 60));
            R(startX, ty, tileW, tileH, bg);
            R(startX, ty, tileW, 2, edge);
            R(startX, ty + tileH - 2, tileW, 2, edge);
            R(startX, ty, 2, tileH, edge);
            R(startX + tileW - 2, ty, 2, tileH, edge);

            // Coin icon (gold circle)
            DrawEllipse(startX + 22, ty + tileH / 2, 14, 14, new Color(255, 200, 50));

            Color titleCol = claimed ? new Color(80, 160, 80) : complete ? Color.White : new Color(160, 170, 210);
            Txt(ch.Title, startX + 44, ty + 10, titleCol);
            Txt(ch.Desc,  startX + 44, ty + 26, new Color(100, 110, 150));

            int barX = startX + 44, barY = ty + 44, barW = 300, barH = 8;
            R(barX, barY, barW, barH, new Color(30, 32, 50));
            int fill = (int)(barW * Math.Min(1f, (float)progress / ch.Target));
            Color barCol = claimed ? new Color(60, 160, 60) : complete ? new Color(100, 200, 255) : new Color(80, 130, 220);
            R(barX, barY, fill, barH, barCol);
            string progStr = $"{Math.Min(progress, ch.Target)}/{ch.Target}";
            Txt(progStr, barX + barW + 6, barY - 2, new Color(120, 130, 170));

            int claimX = startX + tileW - 130, claimY = ty + tileH / 2 - 10;
            string rewardStr = $"+{ch.Coins} COINS";
            if (claimed)
            {
                Txt("CLAIMED", claimX + 10, claimY, new Color(70, 160, 70));
            }
            else if (complete)
            {
                bool hover = _mousePos.X >= claimX && _mousePos.X <= claimX + 120 &&
                             _mousePos.Y >= ty     && _mousePos.Y <= ty + tileH;
                R(claimX, claimY - 3, 120, 24, hover ? new Color(50, 130, 50) : new Color(28, 80, 28));
                Txt(rewardStr, claimX + 6, claimY + 3, new Color(255, 220, 60));
            }
            else
            {
                Txt(rewardStr, claimX + 10, claimY, new Color(80, 90, 60));
            }
        }

        Txt("[ESC] BACK", cx - 30, SH - 36, new Color(100, 110, 160));
    }

    void HandleChallengesClick(bool click)
    {
        if (!click) return;
        const int tileW = 560, tileH = 68, gapY = 10;
        int startX = SW / 2 - tileW / 2;
        int startY = 90;

        for (int i = 0; i < CHALLENGES.Length; i++)
        {
            int  ty      = startY + i * (tileH + gapY);
            bool complete = ChalProgress(i) >= CHALLENGES[i].Target;
            bool claimed  = (_chalClaimed & (1 << i)) != 0;
            if (!complete || claimed) continue;
            int claimX = startX + tileW - 130;
            if (Clicked(click, claimX, ty, 120, tileH))
            {
                _chalClaimed |= (1 << i);
                _coins += CHALLENGES[i].Coins;
                SaveGame();
                return;
            }
        }
    }

    void DrawShop()
    {
        DrawMenuBg();
        int cx = SW / 2, cy = SH / 2;
        string title = "SHOP";
        TxtBig(title, cx - title.Length * 6, 36, new Color(100, 140, 255));
        string coinsStr = $"COINS: {_coins}";
        Txt(coinsStr, cx - coinsStr.Length * 3, 68, new Color(255, 200, 50));
        Txt("EARN COINS VIA CHALLENGES & MATCHES", cx - 105, 84, new Color(70, 80, 120));

        const int cols = 4, tileW = 190, tileH = 110, gapX = 14, gapY = 14;
        int rows   = (SKINS.Length + cols - 1) / cols;
        int gridW  = cols * tileW + (cols - 1) * gapX;
        int gridH  = rows * tileH + (rows - 1) * gapY;
        int startX = cx - gridW / 2;
        int startY = cy - gridH / 2 + 30;

        for (int i = 0; i < SKINS.Length; i++)
        {
            int tx = startX + (i % cols) * (tileW + gapX);
            int ty = startY + (i / cols) * (tileH + gapY);
            bool owned  = SkinOwned(i);
            bool canBuy = !owned && _coins >= SKINS[i].Price;

            Color bg   = owned   ? new Color(26, 40, 26) : new Color(24, 28, 50);
            Color edge = owned   ? new Color(65, 150, 65)
                       : canBuy  ? new Color(90, 110, 180)
                                 : new Color(44, 50, 80);

            R(tx + 3, ty + 3, tileW, tileH, new Color(0, 0, 0, 60));
            R(tx, ty, tileW, tileH, bg);
            R(tx, ty, tileW, 2, edge);
            R(tx, ty + tileH - 2, tileW, 2, edge);
            R(tx, ty, 2, tileH, edge);
            R(tx + tileW - 2, ty, 2, tileH, edge);

            DrawEllipse(tx + tileW / 2, ty + 36, 20, 20, SKINS[i].Col);
            string name = SKINS[i].Name;
            Txt(name, tx + tileW / 2 - name.Length * 3, ty + 62, SKINS[i].Col);

            if (owned)
                Txt("OWNED", tx + tileW / 2 - 15, ty + 78, new Color(65, 170, 65));
            else if (SKINS[i].Price == 0)
                Txt("FREE", tx + tileW / 2 - 12, ty + 78, new Color(100, 200, 100));
            else
            {
                string pr = $"{SKINS[i].Price}c";
                Color  pc = canBuy ? new Color(255, 200, 50) : new Color(90, 90, 100);
                Txt(pr, tx + tileW / 2 - pr.Length * 3, ty + 78, pc);
            }
        }

        Txt("[ESC] BACK", cx - 30, SH - 36, new Color(100, 110, 160));
    }

    void HandleShopClick(bool click)
    {
        if (!click) return;
        const int cols = 4, tileW = 190, tileH = 110, gapX = 14, gapY = 14;
        int cx = SW / 2, cy = SH / 2;
        int rows   = (SKINS.Length + cols - 1) / cols;
        int gridW  = cols * tileW + (cols - 1) * gapX;
        int gridH  = rows * tileH + (rows - 1) * gapY;
        int startX = cx - gridW / 2;
        int startY = cy - gridH / 2 + 30;

        for (int i = 0; i < SKINS.Length; i++)
        {
            int tx = startX + (i % cols) * (tileW + gapX);
            int ty = startY + (i / cols) * (tileH + gapY);
            if (!Clicked(click, tx, ty, tileW, tileH)) continue;
            if (!SkinOwned(i) && _coins >= SKINS[i].Price)
            {
                _coins -= SKINS[i].Price;
                _ownedSkins |= (1 << i);
                SaveGame();
            }
            break;
        }
    }

    void DrawSkinConfig()
    {
        DrawMenuBg();
        int cx = SW / 2;
        string title = "YOUR SKIN";
        TxtBig(title, cx - title.Length * 6, 36, new Color(100, 140, 255));
        string sub = "SELECT THE COLOR OF YOUR CHARACTER";
        Txt(sub, cx - sub.Length * 3, 66, new Color(100, 110, 160));

        const int tileW = 260, tileH = 54, gapY = 10;
        int startX = cx - tileW / 2;
        int startY = 100;
        int idx = 0;

        for (int i = 0; i < SKINS.Length; i++)
        {
            if (!SkinOwned(i)) continue;
            int ty       = startY + idx * (tileH + gapY);
            bool selected = i == _mySkin;
            Color bg   = selected ? new Color(38, 58, 110) : new Color(26, 30, 58);
            Color edge = selected ? new Color(110, 150, 255) : new Color(50, 60, 100);

            R(startX + 3, ty + 3, tileW, tileH, new Color(0, 0, 0, 60));
            R(startX, ty, tileW, tileH, bg);
            R(startX, ty, tileW, 2, edge);
            R(startX, ty + tileH - 2, tileW, 2, edge);
            R(startX, ty, 2, tileH, edge);
            R(startX + tileW - 2, ty, 2, tileH, edge);

            DrawEllipse(startX + 28, ty + tileH / 2, 16, 16, SKINS[i].Col);
            Txt(SKINS[i].Name, startX + 52, ty + tileH / 2 - 4, SKINS[i].Col);
            if (selected)
                R(startX + tileW - 16, ty + tileH / 2 - 5, 10, 10, new Color(110, 150, 255));
            idx++;
        }

        if (idx == 0)
            Txt("COMPLETE CHALLENGES TO UNLOCK SKINS", cx - 105, startY + 20, new Color(80, 90, 130));

        Txt("[ESC] BACK", cx - 30, SH - 36, new Color(100, 110, 160));
    }

    void HandleSkinConfigClick(bool click)
    {
        if (!click) return;
        const int tileW = 260, tileH = 54, gapY = 10;
        int startX = SW / 2 - tileW / 2;
        int startY = 100, idx = 0;

        for (int i = 0; i < SKINS.Length; i++)
        {
            if (!SkinOwned(i)) continue;
            int ty = startY + idx * (tileH + gapY);
            if (Clicked(click, startX, ty, tileW, tileH)) { _mySkin = i; SaveGame(); return; }
            idx++;
        }
    }

    void LoadSave()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "save.dat");
            if (!File.Exists(path)) return;
            using var br = new BinaryReader(File.OpenRead(path));
            byte ver = br.ReadByte();
            if (ver < 2) return;
            _playerName      = br.ReadString();
            _mySkin          = Math.Clamp(br.ReadInt32(), 0, SKINS.Length - 1);
            _chalClaimed     = br.ReadInt32();
            _statWins        = br.ReadInt32();
            _statHits        = br.ReadInt32();
            _statDmgDealt    = br.ReadInt32();
            _statBestStreak  = br.ReadInt32();
            _statPerfectWins = br.ReadInt32();
            _statFastWin     = br.ReadInt32();
            _statCurStreak   = br.ReadInt32();
            if (ver >= 3)
            {
                _coins      = br.ReadInt32();
                _ownedSkins = br.ReadInt32();
                _ownedSkins |= (1 << 0) | (1 << 8);   // ensure defaults always owned
            }
        }
        catch { }
    }

    void SaveGame()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "save.dat");
            using var bw = new BinaryWriter(File.Create(path));
            bw.Write((byte)3);
            bw.Write(_playerName);
            bw.Write(_mySkin);
            bw.Write(_chalClaimed);
            bw.Write(_statWins);
            bw.Write(_statHits);
            bw.Write(_statDmgDealt);
            bw.Write(_statBestStreak);
            bw.Write(_statPerfectWins);
            bw.Write(_statFastWin);
            bw.Write(_statCurStreak);
            bw.Write(_coins);
            bw.Write(_ownedSkins);
        }
        catch { }
    }

    // ── Multiplayer lobby ─────────────────────────────────────────────────────

    void DrawLobby()
    {
        R(0, 0, SW, SH, new Color(18, 20, 35));
        for (int x = 0; x < SW; x += 80) R(x, 0, 1, SH, new Color(27, 30, 50));
        for (int y = 0; y < SH; y += 80) R(0, y, SW, 1, new Color(27, 30, 50));

        int cx = SW / 2, cy = SH / 2;

        TxtBig("MULTIPLAYER", cx - 66, cy - 200, new Color(100, 140, 255));

        // ── Your code (top section) ───────────────────────────────────────
        Txt("YOUR CODE", cx - 27, cy - 155, new Color(120, 130, 180));
        string myCodeStr = _myCode.ToString();
        TxtBig(myCodeStr, cx - myCodeStr.Length * 6, cy - 130, new Color(100, 220, 130));
        Txt("SHARE THIS WITH A FRIEND", cx - 72, cy - 104, new Color(70, 80, 120));

        // Divider
        R(cx - 100, cy - 80, 200, 1, new Color(40, 48, 80));

        // ── Join section (bottom section) ─────────────────────────────────
        if (!_net.IsSeeking)
        {
            Txt("ENTER FRIEND'S CODE:", cx - 60, cy - 62, new Color(160, 170, 210));

            // Input box
            int bw = 108, bh = 30;
            R(cx - bw/2 - 2, cy - 36, bw + 4, bh + 4, new Color(80, 140, 255, 120));
            R(cx - bw/2,     cy - 34, bw,     bh,     new Color(22, 26, 45));
            string display = _codeInput.PadRight(4, '_');
            TxtBig(display, cx - bw/2 + 10, cy - 28, Color.White);

            if (_codeInput.Length == 4)
                Txt("[ENTER] SEARCH", cx - 42, cy + 12, new Color(100, 220, 100));
            else
                Txt("[TYPE 4 DIGITS]", cx - 45, cy + 12, new Color(70, 80, 120));
        }
        else
        {
            // Searching status
            string status = $"SEARCHING FOR {_codeInput}...";
            Txt(status, cx - status.Length * 3, cy - 50, new Color(255, 200, 50));
            Txt("MAKE SURE YOUR FRIEND IS IN THE LOBBY", cx - 111, cy - 24, new Color(70, 80, 120));
        }

        // ── Always-visible: waiting indicator ────────────────────────────
        R(cx - 100, cy + 40, 200, 1, new Color(40, 48, 80));
        Txt("WAITING FOR FRIEND TO ENTER YOUR CODE",
            cx - 111, cy + 50, new Color(60, 70, 110));

        Txt("[ESC] BACK TO MENU", cx - 54, cy + 80, new Color(100, 110, 160));
    }

    // ── Attack animation ─────────────────────────────────────────────────────

    void DrawAttackAnim(Player p)
    {
        if (p == null || !p.AttackActive) return;

        var   feet = W2S(p.Pos);
        float z    = _camZoom;
        float t    = Math.Clamp(p.AtkTimer / Player.ATK_DUR, 0f, 1f);
        byte  alpha = (byte)(t * 230 + 40);

        Color col = new Color(
            Math.Min(255, p.Color.R + 50),
            Math.Min(255, p.Color.G + 50),
            Math.Min(255, p.Color.B + 50),
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

        // Arm als Kreis-Kette in beliebiger Richtung
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

        // Energie-Funken am Angriffsende
        if (t > 0.55f)
        {
            byte sa = (byte)((t - 0.55f) / 0.45f * 255);
            Color spark = new Color(
                Math.Min(255, p.Color.R + 80),
                Math.Min(255, p.Color.G + 80),
                Math.Min(255, p.Color.B + 80),
                (int)sa);
            int sr = (int)(20f * z);
            R(armEndX - 2, armEndY - sr, 4, sr * 2, spark);
            R(armEndX - sr, armEndY - 2, sr * 2, 4, spark);
            DrawEllipse(armEndX, armEndY, Math.Max(1, (int)(sr * 0.55f)), Math.Max(1, (int)(sr * 0.55f)),
                        new Color(Math.Min(255, p.Color.R + 80),
                                  Math.Min(255, p.Color.G + 80),
                                  Math.Min(255, p.Color.B + 80),
                                  (int)sa / 3));
        }
    }

    // ── Primitives ───────────────────────────────────────────────────────────

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

// Moved to separate files:
//   Player      → Entities/Player.cs
//   RectF       → Core/RectF.cs
//   PlayerInput → Core/PlayerInput.cs
//   GameNet     → Net/GameNet.cs
//   Platform    → World/Platform.cs
//   Particle    → World/Particle.cs
//   Glyphs      → Rendering/Glyphs.cs
