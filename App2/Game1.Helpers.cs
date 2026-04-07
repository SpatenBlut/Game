using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
