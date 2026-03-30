using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public struct PlayerInput
{
    public byte Raw;
    public byte AimAngle; // 0-255 → 0..2π; 0 = right, 64 = down, 128 = left, 192 = up

    // Held states
    public bool Left  => (Raw & 0x80) != 0;
    public bool Right => (Raw & 0x40) != 0;
    public bool Up    => (Raw & 0x20) != 0;
    public bool Down  => (Raw & 0x10) != 0;

    // Edge-triggered (set for exactly one frame by the caller)
    public bool Jump        => (Raw & 0x08) != 0;  // rising edge of Up key
    public bool HeavyAttack => (Raw & 0x04) != 0;  // attack key / mouse
    public bool Dodge       => (Raw & 0x02) != 0;  // dodge key rising edge

    public Vector2 GetAimDir()
    {
        float a = AimAngle / 256f * MathF.PI * 2f;
        return new Vector2(MathF.Cos(a), MathF.Sin(a));
    }

    public static PlayerInput FromKeyboard(
        KeyboardState keys, KeyboardState prev,
        bool heavyAttackTrigger, byte aimAngle,
        Keys kL, Keys kR, Keys kU, Keys kD, Keys kDodge)
    {
        byte b = 0;
        if (keys.IsKeyDown(kL))                                b |= 0x80;
        if (keys.IsKeyDown(kR))                                b |= 0x40;
        if (keys.IsKeyDown(kU))                                b |= 0x20;
        if (keys.IsKeyDown(kD))                                b |= 0x10;
        if (keys.IsKeyDown(kU) && !prev.IsKeyDown(kU))        b |= 0x08;
        if (heavyAttackTrigger)                                b |= 0x04;
        if (keys.IsKeyDown(kDodge) && !prev.IsKeyDown(kDodge)) b |= 0x02;
        return new PlayerInput { Raw = b, AimAngle = aimAngle };
    }
}
