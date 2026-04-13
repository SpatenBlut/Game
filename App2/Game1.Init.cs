using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public partial class Game1
{
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

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _fontSmall = Content.Load<SpriteFont>("Fonts/UiSmall");
        _fontMed   = Content.Load<SpriteFont>("Fonts/UiMed");
        _fontBig   = Content.Load<SpriteFont>("Fonts/UiBig");
        _fontHuge  = Content.Load<SpriteFont>("Fonts/UiHuge");

        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("damascus.png");
        if (stream != null)
        {
            var tex = Texture2D.FromStream(GraphicsDevice, stream);
            _damascusW = tex.Width;
            _damascusH = tex.Height;
            _damascusPixels = new Color[_damascusW * _damascusH];
            tex.GetData(_damascusPixels);
            tex.Dispose();
        }

        using var chStream = asm.GetManifestResourceStream("casehardened.png");
        if (chStream != null)
        {
            var tex = Texture2D.FromStream(GraphicsDevice, chStream);
            _chW = tex.Width;
            _chH = tex.Height;
            _chPixels = new Color[_chW * _chH];
            tex.GetData(_chPixels);
            tex.Dispose();
        }

        using var s2145Stream = asm.GetManifestResourceStream("skin2145.png");
        if (s2145Stream != null)
        {
            var tex = Texture2D.FromStream(GraphicsDevice, s2145Stream);
            _skin2145W = tex.Width;
            _skin2145H = tex.Height;
            _skin2145Pixels = new Color[_skin2145W * _skin2145H];
            tex.GetData(_skin2145Pixels);
            tex.Dispose();
        }

        if (_damascusPixels != null)
            _damascusBaked = BakeCircularSkin(_damascusPixels, _damascusW, _damascusH);

        if (_skin2145Pixels != null)
            _skin2145Baked = BakeCircularSkin(_skin2145Pixels, _skin2145W, _skin2145H);

        if (_chPixels != null)
        {
            _chBaked = new Texture2D[81];
            int tW = _chW / 9, tH = _chH / 9;
            for (int idx = 0; idx < 81; idx++)
                _chBaked[idx] = BakeCircularSkin(_chPixels, _chW, _chH, 64,
                                    (idx % 9) * tW, (idx / 9) * tH, tW, tH);
        }
    }

    Texture2D BakeCircularSkin(Color[] srcPixels, int srcW, int srcH,
                                int bakeSize = 64,
                                int tileOffX = 0, int tileOffY = 0,
                                int tileW = 0, int tileH = 0)
    {
        if (tileW == 0) tileW = srcW;
        if (tileH == 0) tileH = srcH;
        var pixels = new Color[bakeSize * bakeSize];
        for (int py = 0; py < bakeSize; py++)
        for (int px = 0; px < bakeSize; px++)
        {
            float fx = (px / (bakeSize - 1f)) * 2f - 1f;
            float fy = (py / (bakeSize - 1f)) * 2f - 1f;
            if (fx * fx + fy * fy > 1f) { pixels[py * bakeSize + px] = Color.Transparent; continue; }
            int srcX = tileOffX + Math.Clamp((int)((fx * 0.5f + 0.5f) * (tileW - 1)), 0, tileW - 1);
            int srcY = tileOffY + Math.Clamp((int)((fy * 0.5f + 0.5f) * (tileH - 1)), 0, tileH - 1);
            pixels[py * bakeSize + px] = srcPixels[srcY * srcW + srcX];
        }
        var tex = new Texture2D(GraphicsDevice, bakeSize, bakeSize);
        tex.SetData(pixels);
        return tex;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        SaveGame();
        Logger.Close();
        base.OnExiting(sender, args);
    }
}
