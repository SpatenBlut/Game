using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

public partial class Game1
{
    GraphicsDeviceManager _graphics;
    SpriteBatch           _spriteBatch;
    Texture2D             _pixel;

    bool _debugOpen = false;
    const int DEBUG_W = 285;

    bool   _termOpen          = false;
    bool   _commandsUnlocked  = false;
    string _termInput = "";
    List<(string text, Color col)> _termLines = new();
    int       _termScrollOffset = 0;   // 0 = unten (neueste Zeile), >0 = nach oben gescrollt
    const int TERM_MAX_LINES = 500;
    const int TERM_H         = 220;

    GameState _state = GameState.Menu;
    int       _roundWinner = -1;
    string    _lastKillText = "";

    int  _scoreP1    = 0;
    int  _scoreEnemy = 0;
    const int SCORE_TO_WIN = 3;

    // Col2 / Animated only used for animated skins; regular skins set Col2=Color.White, Animated=false
    static readonly (string Name, Color Col, Color Col2, bool Animated)[] SKINS =
    {
        // ── index 0  — always free ───────────────────────────────────────────
        ("DEFAULT",        new Color( 80, 160, 255), Color.White,              false),
        // ── index 1-7 — regular unlockable ───────────────────────────────────
        ("CRIMSON",        new Color(255,  85,  95), Color.White,              false),
        ("EMERALD",        new Color( 50, 200, 100), Color.White,              false),
        ("GOLD",           new Color(255, 200,  50), Color.White,              false),
        ("PURPLE",         new Color(160,  80, 220), Color.White,              false),
        ("ORANGE",         new Color(255, 140,  30), Color.White,              false),
        ("PINK",           new Color(255, 100, 180), Color.White,              false),
        ("CYAN",           new Color( 50, 220, 220), Color.White,              false),
        // ── index 8  — always free (CPU) ─────────────────────────────────────
        ("NAVY",           new Color( 30,  50, 160), Color.White,              false),
        // ── index 9-14 — more regular skins ──────────────────────────────────
        ("SILVER",         new Color(180, 190, 210), Color.White,              false),
        ("TOXIC",          new Color( 80, 255,  30), Color.White,              false),
        ("SHADOW",         new Color(140,  85,  40), Color.White,              false),
        ("VOID",           new Color( 12,  12,  18), Color.White,              false),
        ("BLOOD",          new Color(100,  10,  18), Color.White,              false),
        // ── index 14-15 — ANIMATED (≤5 % chest chance) ───────────────────────
        ("RAINBOW",        new Color(255,   0,   0), Color.White,              true),
        ("AURORA",         new Color( 40, 200, 180), new Color(140,  30, 200), true),
        // ── index 16 — EPIC (chest only) ─────────────────────────────────────
        ("MOLTEN",         new Color(255, 150,  10), new Color(160,  20,  10), true),
        // ── index 17 — LEGENDARY (chest only) ────────────────────────────────
        ("CASE HARDENED",  new Color( 30, 120, 255), new Color( 80, 180, 255), true),
        // ── index 18 — DAMASCUS (texture-based pattern) ──────────────────────
        ("DAMASCUS",       new Color( 40, 160, 180), Color.White,              false),
        // ── index 19 — 2145 (texture-based pattern, legendary) ───────────────
        ("2145",           new Color(180, 140, 255), Color.White,              false),
    };

    // Named skin index constants — use these instead of magic numbers
    const int SKIN_RAINBOW      = 14;
    const int SKIN_AURORA       = 15;
    const int SKIN_MOLTEN       = 16;
    const int SKIN_CASEHARDENED = 17;
    const int SKIN_DAMASCUS     = 18;
    const int SKIN_2145         = 19;

    static readonly (string Name, Color Col)[] ARM_SKINS =
    {
        ("DEFAULT",  new Color( 80, 160, 255)),
        ("CRIMSON",  new Color(255,  85,  95)),
        ("EMERALD",  new Color( 50, 200, 100)),
        ("GOLD",     new Color(255, 200,  50)),
        ("PURPLE",   new Color(160,  80, 220)),
        ("ORANGE",   new Color(255, 140,  30)),
        ("PINK",     new Color(255, 100, 180)),
        ("CYAN",     new Color( 50, 220, 220)),
        ("SILVER",   new Color(180, 190, 210)),
        ("TOXIC",    new Color( 80, 255,  30)),
        ("SHADOW",   new Color(140,  85,  40)),
        ("VOID",     new Color( 12,  12,  18)),
        ("BLOOD",    new Color(100,  10,  18)),
        ("WHITE",    new Color(240, 240, 240)),
        ("BLACK",    new Color( 20,  20,  20)),
    };

    static readonly (string Title, string Desc, int Target, int Coins)[] CHALLENGES =
    {
        // ── WINS (indices 0-19) ───────────────────────────────────────────────
        ("FIRST WIN",    "Win 1 match",              1,    100),
        ("BRAWLER",      "Win 3 matches",            3,    160),
        ("FIGHTER",      "Win 5 matches",            5,    220),
        ("WARRIOR",      "Win 10 matches",          10,    320),
        ("VETERAN",      "Win 15 matches",          15,    430),
        ("CHAMPION",     "Win 20 matches",          20,    550),
        ("GLADIATOR",    "Win 25 matches",          25,    680),
        ("DOMINATOR",    "Win 30 matches",          30,    830),
        ("CONQUEROR",    "Win 40 matches",          40,   1050),
        ("LEGEND",       "Win 50 matches",          50,   1280),
        ("WARLORD",      "Win 60 matches",          60,   1550),
        ("TITAN",        "Win 75 matches",          75,   1900),
        ("OVERLORD",     "Win 100 matches",        100,   2400),
        ("WARCHIEF",     "Win 125 matches",        125,   2950),
        ("SUPREME",      "Win 150 matches",        150,   3500),
        ("IMMORTAL",     "Win 175 matches",        175,   4100),
        ("GOD OF WAR",   "Win 200 matches",        200,   4800),
        ("UNSTOPPABLE",  "Win 250 matches",        250,   6000),
        ("INVINCIBLE",   "Win 300 matches",        300,   7500),
        ("ETERNAL",      "Win 500 matches",        500,  12000),
        // ── HITS (indices 20-33) ──────────────────────────────────────────────
        ("SLUGGER",      "Land 20 hits",            20,    110),
        ("STRIKER",      "Land 50 hits",            50,    170),
        ("HIT MACHINE",  "Land 100 hits",          100,    270),
        ("PUNISHER",     "Land 200 hits",          200,    400),
        ("BRUISER",      "Land 350 hits",          350,    570),
        ("HAMMER",       "Land 500 hits",          500,    770),
        ("CRUSHER",      "Land 750 hits",          750,   1030),
        ("DESTROYER",    "Land 1000 hits",        1000,   1350),
        ("MASSACRE",     "Land 1500 hits",        1500,   1900),
        ("DEVASTATOR",   "Land 2000 hits",        2000,   2500),
        ("ANNIHILATOR",  "Land 3000 hits",        3000,   3400),
        ("OBLITERATOR",  "Land 5000 hits",        5000,   4800),
        ("EXTINCTION",   "Land 7500 hits",        7500,   6500),
        ("HIT LEGEND",   "Land 10000 hits",      10000,   8500),
        // ── DAMAGE (indices 34-45) ────────────────────────────────────────────
        ("ENFORCER",     "Deal 200 damage",         200,    120),
        ("POWERHOUSE",   "Deal 500 damage",         500,    210),
        ("RAVAGER",      "Deal 1000 damage",       1000,    350),
        ("DECIMATOR",    "Deal 2500 damage",       2500,    560),
        ("RUINER",       "Deal 5000 damage",       5000,    850),
        ("DEMOLISHER",   "Deal 10000 damage",     10000,   1300),
        ("DOOMSDAY",     "Deal 20000 damage",     20000,   1950),
        ("APOCALYPSE",   "Deal 40000 damage",     40000,   2850),
        ("ARMAGEDDON",   "Deal 75000 damage",     75000,   4100),
        ("CALAMITY",     "Deal 125000 damage",   125000,   5800),
        ("CATACLYSM",    "Deal 200000 damage",   200000,   8200),
        ("OBLIVION",     "Deal 350000 damage",   350000,  12000),
        // ── STREAK (indices 46-52) ────────────────────────────────────────────
        ("ON A ROLL",    "Win 2 in a row",           2,    160),
        ("HOT STREAK",   "Win 3 in a row",           3,    270),
        ("RELENTLESS",   "Win 5 in a row",           5,    470),
        ("UNBEATABLE",   "Win 7 in a row",           7,    740),
        ("ENDLESS",      "Win 10 in a row",         10,   1150),
        ("GODLIKE",      "Win 15 in a row",         15,   2100),
        ("INFINITE",     "Win 20 in a row",         20,   3400),
        // ── PERFECT WINS (indices 53-58) ──────────────────────────────────────
        ("UNTOUCHABLE",  "Win without getting hit",  1,    210),
        ("FLAWLESS",     "Win perfectly 3 times",    3,    440),
        ("PERFECT",      "Win perfectly 5 times",    5,    730),
        ("PRISTINE",     "Win perfectly 10 times",  10,   1450),
        ("IMMACULATE",   "Win perfectly 20 times",  20,   2700),
        ("DIVINE",       "Win perfectly 30 times",  30,   4200),
        // ── FAST WINS (indices 59-63) ─────────────────────────────────────────
        ("SPEED RUN",    "Win in under 60s",         1,    210),
        ("SPEEDSTER",    "Speed win 3 times",        3,    390),
        ("LIGHTNING",    "Speed win 5 times",        5,    670),
        ("FLASH",        "Speed win 10 times",      10,   1250),
        ("SONIC",        "Speed win 20 times",      20,   2300),
    };

    string _playerName      = "";
    int    _mySkin          = 0;
    int    _myArmSkin       = 0;
    bool   _showEyes        = true;
    int    _coins           = 0;
    long   _ownedSkins      = (1L << 0) | (1L << 8);  // DEFAULT + NAVY always owned
    long   _chalClaimed     = 0;
    long   _chalActivated   = 0;
    int[]  _chalBaselines   = new int[64];
    int    _statWins        = 0;
    int    _statHits        = 0;
    int    _statDmgDealt    = 0;
    int    _statBestStreak  = 0;
    int    _statPerfectWins = 0;
    int    _statFastWin     = 0;
    int    _statCurStreak   = 0;

    Vector2 _mousePos;

    float _roundTime    = 120f;
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

    GameNet       _net         = new GameNet();
    bool          _isLocalMode = false;
    BotController _bot         = new BotController();
    int           _myCode      = 0;
    string        _codeInput   = "";

    float _syncTimer = 0f;
    static readonly System.Random _rng = new System.Random();

    const int   CHEST_PRICE      = 150;
    const float CHEST_ANIM_DUR   = 3.5f;
    const int   CHEST_POOL_SIZE  = 60;
    const int   CHEST_TARGET_IDX = 45;

    int     _chestResult        = -2;
    bool    _chestLastDuplicate = false;
    bool    _chestAnimating     = false;
    float   _chestAnimTimer     = 0f;
    int     _chestAnimPick      = -1;
    int[]   _chestPool          = null;

    float _menuTime = 0f;

    int _configTab     = 0;
    int _configScrollY = 0;

    Color[] _damascusPixels;
    int     _damascusW, _damascusH;
    Color[] _chPixels;
    int     _chW, _chH;
    Color[] _skin2145Pixels;
    int     _skin2145W, _skin2145H;
    int     _myCHPattern    = 0;   // currently selected CH pattern (0-80)
    long    _ownedCHPatternsLo = 0; // owned patterns 0-63
    int     _ownedCHPatternsHi = 0; // owned patterns 64-80
    int     _chestPickedPattern = -1; // which CH pattern this chest opening picked
    bool    _showCHPicker;

    Texture2D   _damascusBaked;
    Texture2D   _skin2145Baked;
    Texture2D[] _chBaked;

    bool _chalDirty = true;
    const int MAX_PARTICLES = 250;

    KeyboardState _prevKeys;
    MouseState    _prevMouse;

    int SW, SH;
}
