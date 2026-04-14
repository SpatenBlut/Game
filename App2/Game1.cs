using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public partial class Game1 : Game
{
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
        _menuTime += dt;
        ActivateVisibleChallenges();
        var keys  = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool mouseClick      = mouse.LeftButton  == ButtonState.Pressed && _prevMouse.LeftButton  == ButtonState.Released;
        bool mouseRightClick = mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

        _mousePos = new Vector2(mouse.X, mouse.Y);

        if (KeyJustPressed(keys, Keys.Escape) && _showCHPicker) { _showCHPicker = false; return; }

        if (KeyJustPressed(keys, Keys.Escape) && _state != GameState.Menu && _state != GameState.NameEntry)
        {
            if (_state == GameState.PlayMenu || _state == GameState.Shop || _state == GameState.Challenges || _state == GameState.SkinConfig || _state == GameState.MapSelect)
                _state = GameState.PlayMenu;
            else
            {
                if (!_isLocalMode) { _net.Dispose(); _net = new GameNet(); }
                _isLocalMode = false;
                _state = GameState.PlayMenu;
            }
        }
        if (KeyJustPressed(keys, Keys.F1))     _debugOpen = !_debugOpen;
        if (KeyJustPressed(keys, Keys.Insert)) _termOpen  = !_termOpen;

        if (_termOpen)
        {
            int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                int lineH      = _fontSmall.LineSpacing + 2;
                int headerH    = _fontSmall.LineSpacing + 6;
                int linesAreaH = TERM_H - headerH - 20;
                int maxVisible = linesAreaH / lineH;
                int maxScroll  = Math.Max(0, _termLines.Count - maxVisible);
                _termScrollOffset = Math.Clamp(_termScrollOffset + Math.Sign(scrollDelta), 0, maxScroll);
            }
        }

        if (_state == GameState.NameEntry)
        {
            if (KeyJustPressed(keys, Keys.Enter) && _playerName.Length >= 1)
            {
                SaveGame();
                _state = GameState.PlayMenu;
            }
        }
        else if (_state == GameState.Menu)          HandlePlayMenuClick(mouseClick);
        else if (_state == GameState.PlayMenu)      HandlePlayMenuClick(mouseClick);
        else if (_state == GameState.Shop)
        {
            if (_chestAnimating)
            {
                _chestAnimTimer += dt;
                if (_chestAnimTimer >= CHEST_ANIM_DUR)
                {
                    if (_chestIsArm)
                    {
                        if (_armChestResult != _chestAnimPick) AwardArmChestResult();
                    }
                    else
                    {
                        if (_chestResult != _chestAnimPick) AwardChestResult();
                    }
                    if (mouseClick) _chestAnimating = false;
                }
            }
            else HandleShopClick(mouseClick);
        }
        else if (_state == GameState.MapSelect)      HandleMapSelectClick(mouseClick);
        else if (_state == GameState.Challenges)    HandleChallengesClick(mouseClick);
        else if (_state == GameState.SkinConfig)
        {
            int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            _configScrollY -= scrollDelta / 3;
            if (_configScrollY < 0) _configScrollY = 0;
            HandleSkinConfigClick(mouseClick, mouseRightClick);
        }
        else if (_state == GameState.Lobby)
        {
            _net.Update(dt);
            _net.ReceiveInput();
            if (!_net.IsSeeking && KeyJustPressed(keys, Keys.Enter) && _codeInput.Length == 4)
                if (int.TryParse(_codeInput, out int joinCode)) _net.TryJoin(joinCode);
            if (_net.Connected) ResetRound();
        }
        else if (_state == GameState.Playing)
        {
            if (_timerRunning)
            {
                _roundTime -= dt;
                if (_roundTime <= 0f) { _roundTime = 0f; TimerExpired(); }
            }

            byte p1AimAngle = MouseAimAngle(_p1);
            PlayerInput p1Local = PlayerInput.FromKeyboard(keys, _prevKeys, mouseClick, p1AimAngle,
                Keys.A, Keys.D, Keys.W, Keys.S, Keys.D3);

            PlayerInput p1Input, p2Input;
            if (_isLocalMode)
            {
                p1Input = p1Local;
                p2Input = _bot.Update(dt, _p2, _p1);
            }
            else
            {
                byte p2AimAngle = MouseAimAngle(_p2);
                PlayerInput p2Local = PlayerInput.FromKeyboard(keys, _prevKeys, mouseClick, p2AimAngle,
                    Keys.A, Keys.D, Keys.W, Keys.S, Keys.D3);

                _net.Update(dt);
                PlayerInput myInput = _net.Role == NetRole.Host ? p1Local : p2Local;
                _net.SendInput(myInput);
                PlayerInput remote = _net.ReceiveInput();
                p1Input = _net.Role == NetRole.Host ? p1Local : remote;
                p2Input = _net.Role == NetRole.Host ? remote  : p2Local;

                if (_net.ConsumePendingSync(out var syncData))
                    ApplySyncFromBytes(syncData);

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

                _syncTimer -= dt;
                if (_syncTimer <= 0f)
                {
                    _net.SendStateSync(_p1.Pos, _p1.Vel, _p1.Damage, _p1.Stocks,
                                       _p2.Pos, _p2.Vel, _p2.Damage, _p2.Stocks,
                                       _scoreP1, _scoreEnemy);
                    _syncTimer = 0.1f;
                }

                if (_p1.OnHitLanded == null)
                {
                    _p1.OnHitLanded = (v, vel, dmg, hs) => { _statHits++; _statDmgDealt += (int)dmg; if (dmg >= 15f) _statHeavyHits++; _chalDirty = true; _net.SendHit(v.Id, vel, dmg, hs); };
                    _p2.OnHitLanded = (v, vel, dmg, hs) => _net.SendHit(v.Id, vel, dmg, hs);
                }
            }

            if (_isLocalMode && _p1.OnHitLanded == null)
                _p1.OnHitLanded = (v, vel, dmg, hs) => { _statHits++; _statDmgDealt += (int)dmg; if (dmg >= 15f) _statHeavyHits++; _chalDirty = true; };

            bool p1Auth = _isLocalMode || _net.Role == NetRole.Host;
            bool p2Auth = _isLocalMode || _net.Role == NetRole.Client;
            _p1.Update(dt, p1Input, _platforms, _p2, this, p1Auth);
            _p2.Update(dt, p2Input, _platforms, _p1, this, p2Auth);
            if (SKINS[_mySkin].Animated) _p1.Color = SkinColor(_mySkin, _menuTime);
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
            _state = GameState.PlayMenu;
        }

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(dt);
            if (!_particles[i].Alive)
            {
                _particles[i] = _particles[_particles.Count - 1];
                _particles.RemoveAt(_particles.Count - 1);
            }
        }

        UpdateCamera(dt);

        _fpsFrames++;
        _fpsTimer += dt;
        if (_fpsTimer >= 0.5f) { _fps = _fpsFrames / _fpsTimer; _fpsFrames = 0; _fpsTimer = 0f; }

        _prevKeys  = keys;
        _prevMouse = mouse;
        base.Update(gt);
    }

    protected override void Draw(GameTime gt)
    {
        try { DrawInner(gt); }
        catch (Exception ex)
        {
            Logger.LogException("Draw", ex);
            Logger.Log($"GameState={_state}");
            Logger.Close();
            throw;
        }
    }

    void DrawInner(GameTime gt)
    {
        GraphicsDevice.Clear(new Color(40, 44, 65));
        _spriteBatch.Begin();

        switch (_state)
        {
            case GameState.NameEntry:   DrawNameEntry();  break;
            case GameState.Menu:        DrawPlayMenu();   break;
            case GameState.PlayMenu:    DrawPlayMenu();   break;
            case GameState.Shop:        DrawShop();       break;
            case GameState.Challenges:  DrawChallenges(); break;
            case GameState.SkinConfig:  DrawSkinConfig(); break;
            case GameState.MapSelect:   DrawMapSelect();  break;
            case GameState.Lobby:       DrawLobby();      break;
            default:
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
                break;
        }

        if (_termOpen) DrawTerminal();
        string fpsStr = $"FPS {(int)_fps}";
        Txt(fpsStr, 8, SH - _fontSmall.LineSpacing - 6, new Color(70, 80, 110));
        _spriteBatch.End();
        base.Draw(gt);
    }
}
