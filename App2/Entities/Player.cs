using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public class Player
{
    public const float W = 42f, H = 58f;
    public const int   MAX_STOCKS = 3;
    public const float DODGE_CD       = 5.0f;
    public const float DASH_DUR       = 0.14f;
    public const float DASH_SPEED     = 880f;
    public const float ATK_DUR       = 0.30f;  // Gesamtdauer Animation
    public const float ATK_HITBOX_DUR = 0.10f; // Aktives Trefferfenster
    public const float GRAVITY    = 2100f;
    public const float JUMP_F     = -790f;
    public const float DJ_F       = -690f;
    public const float SPEED      = 385f;

    // Default aim angles for local play (right-upward / left-upward), ~-30° / 210°
    public const byte AIM_RIGHT = 234;
    public const byte AIM_LEFT  = 149;

    public int     Id;
    public int     SkinIdx;
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

    // Wird aufgerufen wenn dieser Spieler einen Treffer landet (Vel/Dmg/Hitstun des Opfers)
    public Action<Player, Vector2, float, float> OnHitLanded;

    public bool    AttackActive;
    public float   AtkTimer;
    public bool    IsHeavy;
    public bool    AtkDir;   // horizontal direction (true = right) at attack start
    public byte    AtkAngle; // full aim angle encoded as 0-255

    // OBB (Oriented Bounding Box) der aktuellen Hitbox
    public Vector2 HitboxCenter;
    public float   HitboxHalfW, HitboxHalfH, HitboxAngle;

    Vector2 _spawn;
    float   _coyote, _jBuf, _dashTimer;
    bool    _touchWallL, _touchWallR;

    public Player(int id, Vector2 spawn, Color col)
    {
        Id=id; Pos=_spawn=spawn; Color=col;
    }

    public static Vector2 DecodeAim(byte b)
    {
        float a = b / 256f * MathF.PI * 2f;
        return new Vector2(MathF.Cos(a), MathF.Sin(a));
    }

    public Vector2 GetAtkAimDir() => DecodeAim(AtkAngle);

    public void LoseStock()  { Stocks--; Dead = Stocks <= 0; Damage = 0; }
    public void ResetFull()  { Stocks = MAX_STOCKS; Respawn(); }
    public void Respawn()
    {
        Pos=_spawn; Vel=Vector2.Zero; Damage=0;
        Jumps=2; InvTime=2f; Hitstun=0;
        SX=SY=1f; WalkTimer=0f; Dead=false; State="SPAWN";
    }

    // applyHits=false: Angriff-Animation läuft, aber Treffer werden nicht lokal angewendet
    // (der Angreifer auf der anderen Seite ist autoritativ und schickt ein Hit-Event)
    public void Update(float dt, PlayerInput input,
                       List<Platform> plats, Player other, Game1 game, bool applyHits = true)
    {
        try { UpdateInner(dt, input, plats, other, game, applyHits); }
        catch (Exception ex)
        {
            Logger.LogException($"Player.Update P{Id}", ex);
            Logger.Log($"  Pos={Pos} Vel={Vel} State={State} Grounded={Grounded} Dead={Dead}");
            Logger.Log($"  AtkActive={AttackActive} Hitstun={Hitstun:F3} InvTime={InvTime:F3}");
            throw;
        }
    }

    void UpdateInner(float dt, PlayerInput input,
                     List<Platform> plats, Player other, Game1 game, bool applyHits)
    {
        if (Dead) return;

        bool l  = input.Left;
        bool r  = input.Right;
        bool u  = input.Up;
        bool d  = input.Down;
        bool uj = input.Jump;
        bool attack = input.HeavyAttack;
        bool dodge  = input.Dodge;

        if (InvTime    > 0) InvTime    -= dt;
        if (DodgeCD    > 0) DodgeCD    -= dt;
        if (AtkCD      > 0) AtkCD      -= dt;
        if (Hitstun    > 0) Hitstun    -= dt;
        if (_dashTimer > 0) _dashTimer -= dt;
        SX += (1f-SX) * 14f*dt;
        SY += (1f-SY) * 14f*dt;

        if (MathF.Abs(Vel.X) > 20f && Grounded) WalkTimer += dt*9f;

        if (AttackActive)
        {
            AtkTimer -= dt;
            if (AtkTimer <= 0) { AttackActive = false; }
            else
            {
                bool hitboxLive = AtkTimer > (ATK_DUR - ATK_HITBOX_DUR);

                var  aimD = GetAtkAimDir();
                float hw = IsHeavy ? 85f : 58f, hh = IsHeavy ? 52f : 40f;
                // Hitbox-Mittelpunkt in Aim-Richtung vom Spieler-Mittelpunkt aus
                float pcx  = Pos.X;
                float pcy  = Pos.Y - H * 0.5f;
                float reach = W * 0.5f + hw * 0.5f;
                float hcx  = pcx + aimD.X * reach;
                float hcy  = pcy + aimD.Y * reach;
                HitboxCenter = new Vector2(hcx, hcy);
                HitboxHalfW  = hw * 0.5f;
                HitboxHalfH  = hh * 0.5f;
                HitboxAngle  = MathF.Atan2(aimD.Y, aimD.X);

                if (hitboxLive && other != null && applyHits)
                {
                    var oBox = new RectF(other.Pos.X-W/2f, other.Pos.Y-H, W, H);
                    if (OBBvsAABB(HitboxCenter, HitboxHalfW, HitboxHalfH, HitboxAngle, oBox)
                        && other.Hitstun<=0 && !other.Invincible)
                    {
                        float dmg     = IsHeavy ? 19f : 8f;
                        float kbBase  = IsHeavy ? 496f : 288f;  // -20 % Knockback
                        float kb      = kbBase * (1f + other.Damage/55f);
                        float hitstun = IsHeavy ? 0.42f : 0.22f;
                        var   hitVel  = new Vector2(aimD.X * kb, aimD.Y * kb);
                        other.Vel     = hitVel;
                        other.Damage += dmg;
                        other.Hitstun = hitstun;
                        other.SX=0.62f; other.SY=1.42f;
                        game.SpawnHitFlash(new Vector2(other.Pos.X, other.Pos.Y-H/2f), other.Color, IsHeavy);
                        game.AddShake(IsHeavy?7f:3.5f);
                        OnHitLanded?.Invoke(other, hitVel, dmg, hitstun);
                        // AttackActive bleibt aktiv → Animation läuft bis zum Ende aus
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
        if (!AttackActive && _dashTimer <= 0)
        {
            if (l) FacingRight=false;
            if (r) FacingRight=true;
        }

        // Bewegungs-Override nur außerhalb des Dash-Fensters
        if (_dashTimer <= 0)
        {
            if (Grounded) Vel.X += (tVX-Vel.X)*0.88f;
            else          Vel.X += (tVX-Vel.X)*dt*22f;
        }

        if (dodge && DodgeCD <= 0)
        {
            var dashDir = DecodeAim(input.AimAngle);
            Vel         = dashDir * DASH_SPEED;
            InvTime     = 0.30f;
            DodgeCD     = DODGE_CD;
            _dashTimer  = DASH_DUR;
            FacingRight = dashDir.X >= 0;
            // Squash/Stretch entlang der Dash-Richtung
            float absX = MathF.Abs(dashDir.X), absY = MathF.Abs(dashDir.Y);
            if (absX >= absY) { SX = dashDir.X > 0 ? 1.55f : 0.60f; SY = 0.68f; }
            else              { SX = 0.75f; SY = dashDir.Y < 0 ? 0.62f : 1.45f; }
            game.SpawnBurst(Pos, new Color((int)Color.R, (int)Color.G, (int)Color.B, 200), 12);
        }
        else if (attack && !AttackActive && AtkCD<=0)
        {
            AttackActive=true; IsHeavy=true; AtkTimer=ATK_DUR; AtkCD=0.58f;
            AtkAngle = input.AimAngle;
            AtkDir   = GetAtkAimDir().X >= 0f;
            SX=AtkDir?1.52f:0.58f; SY=0.80f;
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
        DoPhysics(plats);

        // Wall Jump: Wand berührt + Jump-Buffer aktiv + nicht am Boden
        if (_jBuf > 0 && !Grounded && (_touchWallL || _touchWallR))
        {
            float dir = _touchWallL ? 1f : -1f;   // weg von der Wand abspringen
            Vel.X  = dir * SPEED * 1.6f;
            Vel.Y  = JUMP_F * 0.92f;
            Jumps  = 2;                            // Doppelsprung aufladen
            _jBuf  = 0;
            SX = 0.70f; SY = 1.40f;
            game.SpawnHit(new Vector2(Pos.X, Pos.Y), new Color(160, 210, 255, 160), 6);
        }

        UpdateState();
    }

    void DoPhysics(List<Platform> plats)
    {
        bool was=Grounded; Grounded=false;
        _touchWallL = false; _touchWallR = false;
        foreach (var p in plats)
        {
            var pr = new RectF(Pos.X-W/2f, Pos.Y-H, W, H);
            if (!pr.Intersects(new RectF(p.X,p.Y,p.W,p.H))) continue;
            float oT=pr.Bottom-p.Y, oB=(p.Y+p.H)-pr.Top;
            float oL=pr.Right-p.X,  oR=(p.X+p.W)-pr.Left;
            float mn=Math.Min(Math.Min(oT,oB),Math.Min(oL,oR));
            if      (mn==oT && Vel.Y>=0) { Pos.Y=p.Y;           Vel.Y=0; Grounded=true; Jumps=2; if(!was){SX=1.35f;SY=0.65f;} }
            else if (mn==oB && Vel.Y<0)  { Pos.Y=p.Y+p.H+H;    Vel.Y=0; }
            else if (mn==oL && Vel.X>0)  { Pos.X=p.X-W/2f;     Vel.X=0; _touchWallR=true; }
            else if (mn==oR && Vel.X<0)  { Pos.X=p.X+p.W+W/2f; Vel.X=0; _touchWallL=true; }
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

    // SAT-Test: OBB gegen AABB
    static bool OBBvsAABB(Vector2 obbC, float obbHW, float obbHH, float angle, RectF aabb)
    {
        float aabbCX = aabb.X + aabb.Width  * 0.5f;
        float aabbCY = aabb.Y + aabb.Height * 0.5f;
        float ahw    = aabb.Width  * 0.5f;
        float ahh    = aabb.Height * 0.5f;
        float cosA   = MathF.Cos(angle), sinA = MathF.Sin(angle);
        float absCos = MathF.Abs(cosA),  absSin = MathF.Abs(sinA);
        float dx     = aabbCX - obbC.X,  dy = aabbCY - obbC.Y;
        // Trennachse: lokale OBB X-Achse
        if (MathF.Abs( dx*cosA + dy*sinA) > obbHW + ahw*absCos + ahh*absSin) return false;
        // Trennachse: lokale OBB Y-Achse
        if (MathF.Abs(-dx*sinA + dy*cosA) > obbHH + ahw*absSin + ahh*absCos) return false;
        // Trennachse: Welt-X
        if (MathF.Abs(dx) > ahw + obbHW*absCos + obbHH*absSin) return false;
        // Trennachse: Welt-Y
        if (MathF.Abs(dy) > ahh + obbHW*absSin + obbHH*absCos) return false;
        return true;
    }
}
