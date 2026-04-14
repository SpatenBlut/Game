using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public partial class Game1
{
    void BuildMap()
    {
        _platforms.Clear();
        switch (_selectedMap)
        {
            default:
            case 0: // CLASSIC
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
                break;

            case 1: // SPACE — offen, wenige Plattformen
                _platforms.Add(new Platform(-420,  220,  840, 30, PlatType.Main));
                _platforms.Add(new Platform(-820,   80,  220, 22, PlatType.Side));
                _platforms.Add(new Platform( 600,   80,  220, 22, PlatType.Side));
                _platforms.Add(new Platform(-140,  -80,  280, 22, PlatType.Side));
                _platforms.Add(new Platform(-820, -220,  160, 18, PlatType.Small));
                _platforms.Add(new Platform( 660, -220,  160, 18, PlatType.Small));
                _platforms.Add(new Platform(-100, -300,  200, 18, PlatType.Small));
                break;

            case 2: // BRIDGE — lange Brücke mit Etagen
                _platforms.Add(new Platform(-680,  250, 1360, 28, PlatType.Main));
                _platforms.Add(new Platform(-420,   60,  260, 22, PlatType.Side));
                _platforms.Add(new Platform( 160,   60,  260, 22, PlatType.Side));
                _platforms.Add(new Platform(-160, -120,  320, 22, PlatType.Side));
                _platforms.Add(new Platform(-880,  120,  160, 18, PlatType.Small));
                _platforms.Add(new Platform( 720,  120,  160, 18, PlatType.Small));
                _platforms.Add(new Platform( -90, -280,  180, 18, PlatType.Small));
                break;

            case 3: // RUINS — zwei getrennte Böden, Trümmer
                _platforms.Add(new Platform(-880,  260,  440, 28, PlatType.Main));
                _platforms.Add(new Platform( 440,  260,  440, 28, PlatType.Main));
                _platforms.Add(new Platform(-200,  160,  400, 22, PlatType.Side));
                _platforms.Add(new Platform(-640,   40,  200, 22, PlatType.Side));
                _platforms.Add(new Platform( 440,   40,  200, 22, PlatType.Side));
                _platforms.Add(new Platform(-140, -100,  280, 22, PlatType.Side));
                _platforms.Add(new Platform(-880,  -60,  150, 18, PlatType.Small));
                _platforms.Add(new Platform( 730,  -60,  150, 18, PlatType.Small));
                _platforms.Add(new Platform(-100, -280,  200, 18, PlatType.Small));
                break;
        }
    }

    void ResetRound()
    {
        _p1 = new Player(1, new Vector2(-200, 160), SKINS[_mySkin].Col) { SkinIdx = _mySkin };
        _p2 = new Player(2, new Vector2( 200, 160), SKINS[8].Col)      { SkinIdx = 8 };
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
        BuildMap();
        ResetRound();
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

        _statMatches++;
        if (localWon)
        {
            _statWins++;
            _statCurStreak++;
            if (_statCurStreak > _statBestStreak) _statBestStreak = _statCurStreak;
            if (_scoreEnemy == 0)   _statPerfectWins++;
            if (_roundTime > 60f)   _statFastWin++;
            _coins += 30;
            _chalDirty = true;
        }
        else if (winner != 0)
        {
            _statCurStreak = 0;
        }
        _chalDirty = true;

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

    public void SpawnHit(Vector2 pos, Color col, int n) => SpawnBurst(pos, col, n);

    public void SpawnHitFlash(Vector2 pos, Color col, bool heavy)
    {
        int n = heavy ? 28 : 15;
        SpawnBurst(pos, col, n);
        int flashCount = heavy ? 10 : 5;
        for (int i = 0; i < flashCount; i++)
        {
            if (_particles.Count >= MAX_PARTICLES) break;
            float a = (float)(_rng.NextDouble() * MathHelper.TwoPi);
            float s = (float)(_rng.NextDouble() * 250 + 80);
            _particles.Add(new Particle(pos,
                new Vector2(MathF.Cos(a), MathF.Sin(a)) * s,
                Color.White, heavy ? 0.22f : 0.14f, heavy ? 9f : 6f));
        }
    }

    public void SpawnBurst(Vector2 pos, Color col, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (_particles.Count >= MAX_PARTICLES) break;
            float a    = (float)(_rng.NextDouble() * MathHelper.TwoPi);
            float s    = (float)(_rng.NextDouble() * 400 + 60);
            float life = (float)(_rng.NextDouble() * 0.5f + 0.15f);
            _particles.Add(new Particle(pos,
                new Vector2(MathF.Cos(a), MathF.Sin(a)) * s, col, life, (float)(_rng.NextDouble() * 5 + 2)));
        }
    }

    byte MouseAimAngle(Player p)
    {
        Vector2 pScreen = W2S(p.Pos);
        float dx = _mousePos.X - pScreen.X;
        float dy = _mousePos.Y - pScreen.Y;
        if (dx == 0f && dy == 0f) dx = 1f;
        float angle = MathF.Atan2(dy, dx);
        return (byte)((int)((angle / (MathF.PI * 2f) + 1f) * 256f) & 0xFF);
    }
}
