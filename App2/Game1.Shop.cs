using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public partial class Game1
{
    string ChestDisplayName(int i)
    {
        if (i == 17 && _chestPickedPattern >= 0)
            return $"CASE HARDENED #{_chestPickedPattern + 1}";
        return SKINS[i].Name;
    }

    // Called once when the chest animation finishes to award the skin/pattern
    void AwardChestResult()
    {
        if (_chestAnimPick == 17) // Case Hardened — award one specific pattern
        {
            _chestLastDuplicate = CHPatternOwned(_chestPickedPattern);
            if (!_chestLastDuplicate)
            {
                UnlockCHPattern(_chestPickedPattern);
                _myCHPattern = _chestPickedPattern; // auto-select the new pattern
            }
        }
        else
        {
            _chestLastDuplicate = SkinOwned(_chestAnimPick);
            if (!_chestLastDuplicate)
                _ownedSkins |= (1L << _chestAnimPick);
        }
        _chestResult = _chestAnimPick;
        SaveGame();
    }

    void DrawShop()
    {
        DrawMenuBg();
        int cx = SW / 2;

        // ── CS2-style case opening animation overlay ─────────────────────────
        if (_chestAnimating)
        {
            R(0, 0, SW, SH, new Color(0, 0, 0, 180));

            const int slotW = 128, slotH = 110, slotGap = 8;
            int slotStep = slotW + slotGap;
            float progress = Math.Min(_chestAnimTimer / CHEST_ANIM_DUR, 1f);
            // Quartic ease-out: fast at start, very slow at end
            float ease   = 1f - MathF.Pow(1f - progress, 4);
            float scrollX = CHEST_TARGET_IDX * slotStep * ease;

            int stripY = SH / 2 - slotH / 2;

            R(0, stripY - 24, SW, slotH + 48, new Color(22, 24, 44));
            R(0, stripY - 26, SW, 2, new Color(70, 90, 160));
            R(0, stripY + slotH + 22, SW, 2, new Color(70, 90, 160));

            int firstI = (int)Math.Floor((scrollX - SW * 0.6f) / slotStep);
            int lastI  = (int)Math.Ceiling((scrollX + SW * 0.6f) / slotStep);
            for (int i = firstI; i <= lastI; i++)
            {
                int poolIdx    = ((i % CHEST_POOL_SIZE) + CHEST_POOL_SIZE) % CHEST_POOL_SIZE;
                int skinIdx    = _chestPool[poolIdx];
                int itemCenterX = cx + (int)(i * slotStep - scrollX);
                int itemX = itemCenterX - slotW / 2;

                bool isLegendary = skinIdx == 17 || skinIdx == 18;
                bool isRare      = skinIdx == 14 || skinIdx == 15 || skinIdx == 16;
                bool isLanded    = progress >= 1f && i == CHEST_TARGET_IDX;
                Color itemBg   = isLanded ? new Color(55, 45, 12)  : new Color(34, 38, 64);
                Color itemEdge = isLanded    ? new Color(255, 200, 50)
                               : isLegendary ? new Color(255, 180, 20)
                               : isRare      ? new Color(180, 60, 255)
                               :               new Color(42, 48, 78);

                R(itemX + 3, stripY + 3, slotW, slotH, new Color(0, 0, 0, 70));
                R(itemX, stripY, slotW, slotH, itemBg);
                R(itemX, stripY, slotW, 2, itemEdge);
                R(itemX, stripY + slotH - 2, slotW, 2, itemEdge);
                R(itemX, stripY, 2, slotH, itemEdge);
                R(itemX + slotW - 2, stripY, 2, slotH, itemEdge);

                DrawSkinEllipse(itemCenterX, stripY + slotH / 2 - 12, 20, 20, skinIdx, _menuTime);
                string sn = ChestDisplayName(skinIdx);
                Txt(sn, itemCenterX - sn.Length * 3, stripY + slotH / 2 + 14, SkinColor(skinIdx, _menuTime));
            }

            // Center marker lines
            R(cx - 2, stripY - 22, 4, 18, new Color(255, 220, 50));
            R(cx - 2, stripY + slotH + 4, 4, 18, new Color(255, 220, 50));

            // Edge fade-out gradient
            int fadeW = Math.Min(220, SW / 4);
            for (int fx = 0; fx < fadeW; fx++)
            {
                int a = (int)(180 * (1f - (float)fx / fadeW));
                R(fx, stripY - 24, 1, slotH + 48, new Color(0, 0, 0, a));
                R(SW - 1 - fx, stripY - 24, 1, slotH + 48, new Color(0, 0, 0, a));
            }

            if (progress >= 1f)
            {
                Color resultCol = SkinColor(_chestAnimPick, _menuTime);
                string gotStr = _chestLastDuplicate
                    ? $"DUPLICATE: {ChestDisplayName(_chestAnimPick)}"
                    : $"NEW: {ChestDisplayName(_chestAnimPick)}!";
                Color gotCol = _chestLastDuplicate ? new Color(130, 130, 150) : resultCol;
                TxtBig(gotStr, cx - gotStr.Length * 6, stripY - 90, gotCol);
                int pulse = (int)(_chestAnimTimer * 3f) % 2;
                if (pulse == 0)
                    TxtMed("CLICK TO CONTINUE", cx - 102, stripY + slotH + 50, new Color(180, 190, 220));
            }
            else
            {
                TxtMed("OPENING...", cx - 60, stripY + slotH + 50, new Color(120, 130, 170));
            }
            return;
        }

        // ── Normal shop view ─────────────────────────────────────────────────
        string title = "SHOP";
        TxtBig(title, cx - title.Length * 6, 28, new Color(100, 140, 255));

        string coinsStr = $"COINS: {_coins}";
        TxtBig(coinsStr, SW - coinsStr.Length * 12 - 20, 20, new Color(255, 200, 50));

        // ── Chest panel ───────────────────────────────────────────────────────
        const int chestPanW = 420, chestPanH = 100;
        int chestX = cx - chestPanW / 2, chestY = SH / 2 - chestPanH / 2 - 20;
        bool canAffordChest = _coins >= CHEST_PRICE;
        bool hoverChest = canAffordChest &&
                          _mousePos.X >= chestX && _mousePos.X <= chestX + chestPanW &&
                          _mousePos.Y >= chestY && _mousePos.Y <= chestY + chestPanH;

        Color chestEdge = !canAffordChest ? new Color(70, 70, 55)
                        : hoverChest      ? new Color(255, 220, 80)
                                          : new Color(200, 160, 40);

        R(chestX + 3, chestY + 3, chestPanW, chestPanH, new Color(0, 0, 0, 70));
        R(chestX, chestY, chestPanW, chestPanH, new Color(40, 43, 68));
        R(chestX, chestY, chestPanW, 2, chestEdge);
        R(chestX, chestY + chestPanH - 2, chestPanW, 2, chestEdge);
        R(chestX, chestY, 2, chestPanH, chestEdge);
        R(chestX + chestPanW - 2, chestY, 2, chestPanH, chestEdge);

        // Chest icon
        int cix = chestX + 52, ciy = chestY + chestPanH / 2;
        R(cix - 22, ciy - 13, 44, 30, new Color(120, 80, 10));
        R(cix - 24, ciy - 15, 44, 30, new Color(210, 145, 25));
        R(cix - 24, ciy - 15, 44, 13, new Color(255, 195, 45));
        R(cix - 24, ciy - 3,  44, 2,  new Color(90, 60, 5));
        DrawEllipse(cix, ciy + 6, 5, 5, new Color(255, 215, 60));

        int tx = chestX + 98;
        TxtMed("MYSTERY CHEST", tx, chestY + 14, new Color(255, 200, 50));
        {
            string prStr = $"OPEN FOR {CHEST_PRICE} COINS";
            Color  prCol = canAffordChest ? (hoverChest ? Color.White : new Color(255, 200, 50))
                                          : new Color(80, 80, 70);
            TxtMed(prStr, tx, chestY + 46, prCol);
        }

        if (_chestResult >= 0)
        {
            string gotStr = _chestLastDuplicate
                ? $"LAST: {ChestDisplayName(_chestResult)} (DUPLICATE)"
                : $"LAST: {ChestDisplayName(_chestResult)}";
            Color gotCol = _chestLastDuplicate ? new Color(120, 120, 140) : SkinColor(_chestResult, _menuTime);
            TxtMed(gotStr, cx - gotStr.Length * 6, chestY + chestPanH + 18, gotCol);
        }

        TxtMed("[ESC] BACK", cx - 60, SH - 36, new Color(100, 110, 160));
    }

    void HandleShopClick(bool click)
    {
        if (!click) return;
        int cx = SW / 2;

        const int chestPanW = 420, chestPanH = 100;
        int chestX = cx - chestPanW / 2, chestY = SH / 2 - chestPanH / 2 - 20;
        if (Clicked(click, chestX, chestY, chestPanW, chestPanH))
        {
            if (_coins < CHEST_PRICE) return;

            // ── Weighted rarity ───────────────────────────────────────────────
            // 0.5% LEGENDARY (Case Hardened + Damascus)
            // 2.0% RARE      (Molten + Aurora + Rainbow)
            // 97.5% COMMON
            const int SKIN_RAINBOW = 14, SKIN_AURORA = 15, SKIN_MOLTEN = 16,
                      SKIN_CASEHARDENED = 17, SKIN_DAMASCUS = 18, SKIN_2145 = 19;
            var legendaryPool = new List<int> { SKIN_CASEHARDENED, SKIN_DAMASCUS, SKIN_2145 };
            var rarePool      = new List<int> { SKIN_RAINBOW, SKIN_AURORA, SKIN_MOLTEN };
            var commonPool    = new List<int>();
            for (int i = 0; i < SKINS.Length; i++)
            {
                if (i == 0 || i == 8) continue;
                if (legendaryPool.Contains(i) || rarePool.Contains(i)) continue;
                commonPool.Add(i);
            }
            float roll = (float)_rng.NextDouble();
            if (roll < 0.005f)
                _chestAnimPick = legendaryPool[_rng.Next(legendaryPool.Count)];
            else if (roll < 0.025f)
                _chestAnimPick = rarePool[_rng.Next(rarePool.Count)];
            else
                _chestAnimPick = commonPool[_rng.Next(commonPool.Count)];

            // For Case Hardened: pick one random unowned pattern (or any if all owned)
            if (_chestAnimPick == SKIN_CASEHARDENED)
            {
                var unowned = new System.Collections.Generic.List<int>();
                for (int i = 0; i < 81; i++)
                    if (!CHPatternOwned(i)) unowned.Add(i);
                _chestPickedPattern = unowned.Count > 0
                    ? unowned[_rng.Next(unowned.Count)]
                    : _rng.Next(81);
            }
            else _chestPickedPattern = -1;

            _coins -= CHEST_PRICE;
            SaveGame();

            var stripPool = new List<int>();
            for (int i = 0; i < SKINS.Length; i++)
            {
                if (i == 0 || i == 8) continue;
                if (legendaryPool.Contains(i)) continue; // legendary never in background strip
                int w = rarePool.Contains(i) ? 1 : 6;
                for (int n = 0; n < w; n++) stripPool.Add(i);
            }
            _chestPool = new int[CHEST_POOL_SIZE];
            for (int i = 0; i < CHEST_POOL_SIZE; i++)
                _chestPool[i] = stripPool[_rng.Next(stripPool.Count)];
            _chestPool[CHEST_TARGET_IDX % CHEST_POOL_SIZE] = _chestAnimPick;

            _chestAnimTimer  = 0f;
            _chestAnimating  = true;
        }
    }
}
