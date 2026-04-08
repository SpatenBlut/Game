using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

public partial class Game1
{
    int GameStartX => _debugOpen ? DEBUG_W : 0;
    int GameW      => SW - GameStartX;

    Vector2 W2S(Vector2 world)
    {
        Vector2 center = new Vector2(GameStartX + GameW / 2f, SH / 2f);
        return (world - _camPos) * _camZoom + center + _camShakeOffset;
    }

    bool KeyJustPressed(KeyboardState cur, Keys k) =>
        cur.IsKeyDown(k) && !_prevKeys.IsKeyDown(k);

    Color SkinColor(int idx, float t)
    {
        var s = SKINS[idx];
        if (!s.Animated) return s.Col;
        if (s.Name == "RAINBOW") return HsvToRgb((t * 0.3f) % 1f, 1f, 1f);
        float f = MathF.Sin(t * 1.5f) * 0.5f + 0.5f;
        return new Color(
            (int)(s.Col.R + (s.Col2.R - s.Col.R) * f),
            (int)(s.Col.G + (s.Col2.G - s.Col.G) * f),
            (int)(s.Col.B + (s.Col2.B - s.Col.B) * f));
    }

    static Color HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - MathF.Abs((h * 6f) % 2f - 1f));
        float m = v - c;
        float r, g, b;
        switch ((int)(h * 6) % 6) {
            case 0:  r=c; g=x; b=0; break;
            case 1:  r=x; g=c; b=0; break;
            case 2:  r=0; g=c; b=x; break;
            case 3:  r=0; g=x; b=c; break;
            case 4:  r=x; g=0; b=c; break;
            default: r=c; g=0; b=x; break;
        }
        return new Color((int)((r+m)*255), (int)((g+m)*255), (int)((b+m)*255));
    }

    void OnTextInput(object sender, TextInputEventArgs e)
    {
        char c = e.Character;
        if (_termOpen && _state != GameState.NameEntry)
        {
            if (c == '\b' && _termInput.Length > 0)
                _termInput = _termInput[..^1];
            else if (c == '\r' || c == '\n')
                SubmitTermCmd();
            else if (!char.IsControl(c))
                _termInput += c;
            return;
        }
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
}
