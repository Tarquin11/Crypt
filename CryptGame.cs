using Crypt.Core.Engine;
using Crypt.Core.Utility;
using Crypt.Input;
using Crypt.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crypt;

/// <summary>The MonoGame shell around the framework-independent dungeon session.</summary>
public sealed class CryptGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly CommandInput _input = new();
    private readonly DungeonSession _session = new();

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private GameRenderer _renderer = null!;

    public CryptGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameConstants.CanvasWidth,
            PreferredBackBufferHeight = GameConstants.CanvasHeight,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        Window.Title = "CRYPT";
    }

    protected override void Initialize()
    {
        Window.AllowUserResizing = false;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _font = Content.Load<SpriteFont>("DefaultFont");
        _font.DefaultCharacter = '?';
        _renderer = new GameRenderer(_spriteBatch, _pixel, _font);
    }

    protected override void Update(GameTime gameTime)
    {
        var commands = _input.Read(_session.State, gameTime.TotalGameTime);

        _session.AppendCipherText(_input.TypedCipherText);

        if (_input.QuitRequested)
        {
            Exit();
            return;
        }

        foreach (var command in commands)
        {
            _session.Handle(command, gameTime.TotalGameTime);
        }

        if (_input.MinesweeperCell is { } mineCell)
        {
            _session.HandleMinesweeperClick(mineCell, _input.IsMinesweeperFlagClick, gameTime.TotalGameTime);
        }

        _session.Update(gameTime.TotalGameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            // The world is drawn from crisp integer-aligned primitives. Text, however,
            // comes from an anti-aliased font atlas and needs linear sampling to remain
            // readable at the UI's fractional display scales.
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        _renderer.Draw(_session, gameTime.TotalGameTime);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
    
}
