using Crypt.Core.Engine;
using Crypt.Core.Utility;
using Crypt.Core.World;
using Microsoft.Xna.Framework.Input;

namespace Crypt.Input;

/// <summary>Turns MonoGame's held-key polling into the game's discrete command stream.</summary>
public sealed class CommandInput
{
    private const int HintButtonLeft = 932;
    private const int HintButtonTop = 18;
    private const int HintButtonSize = 50;

    private KeyboardState _previous;
    private KeyboardState _current;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public bool QuitRequested { get; private set; }

    public string TypedCipherText { get; private set; } = string.Empty;

    public GridPosition? MinesweeperCell { get; private set; }

    public bool IsMinesweeperFlagClick { get; private set; }

    public IReadOnlyList<GameCommand> Read(GameState state)
    {
        _current = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
        var commands = new List<GameCommand>();
        QuitRequested = WasPressed(Keys.Escape);
        TypedCipherText = string.Empty;
        MinesweeperCell = null;
        IsMinesweeperFlagClick = false;

        // R restarts normal gameplay, but is a required letter in cipher answers
        // such as NORTH and must not restart the room while the panel is open.
        if (state != GameState.CipherPuzzle && WasPressed(Keys.R))
        {
            commands.Add(GameCommand.Restart);
        }

        if (state is GameState.Dialogue or GameState.PaperReading or GameState.LibraryReading or GameState.DeathRoast or GameState.GameOver)
        {
            if (WasPressed(Keys.Enter) || WasPressed(Keys.Z))
            {
                commands.Add(GameCommand.Confirm);
            }
        }
        else if (state == GameState.Victory)
        {
            if (WasPressed(Keys.P))
            {
                commands.Add(GameCommand.NextLevel);
            }
        }
        else if (state == GameState.CipherPuzzle)
        {
            TypedCipherText = ReadTypedText();

            if (WasPressed(Keys.Back))
            {
                commands.Add(GameCommand.CipherBackspace);
            }

            if (WasPressed(Keys.Enter))
            {
                commands.Add(GameCommand.CipherSubmit);
            }

            if (WasHintClicked())
            {
                commands.Add(GameCommand.CipherHint);
            }
        }
        else if (state == GameState.Minesweeper && TryReadMinesweeperClick(out var cell, out var isFlagClick))
        {
            MinesweeperCell = cell;
            IsMinesweeperFlagClick = isFlagClick;
        }
        else if (state == GameState.Playing)
        {
            if (WasPressed(Keys.W) || WasPressed(Keys.Up) || WasPressed(Keys.Z))
            {
                commands.Add(GameCommand.MoveUp);
            }

            if (WasPressed(Keys.S) || WasPressed(Keys.Down))
            {
                commands.Add(GameCommand.MoveDown);
            }

            if (WasPressed(Keys.A) || WasPressed(Keys.Q) || WasPressed(Keys.Left))
            {
                commands.Add(GameCommand.MoveLeft);
            }

            if (WasPressed(Keys.D) || WasPressed(Keys.Right))
            {
                commands.Add(GameCommand.MoveRight);
            }

            if (WasPressed(Keys.Space))
            {
                commands.Add(GameCommand.Attack);
            }
        }

        _previous = _current;
        _previousMouse = _currentMouse;
        return commands;
    }

    private bool WasPressed(Keys key) => _current.IsKeyDown(key) && _previous.IsKeyUp(key);

    private string ReadTypedText()
    {
        Span<char> characters = stackalloc char[27];
        var count = 0;

        for (var offset = 0; offset < 26; offset++)
        {
            var key = Keys.A + offset;
            if (WasPressed(key))
            {
                characters[count++] = (char)('A' + offset);
            }
        }

        if (WasPressed(Keys.Space))
        {
            characters[count++] = ' ';
        }

        return new string(characters[..count]);
    }

    private bool WasHintClicked() =>
        _currentMouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released &&
        _currentMouse.X >= HintButtonLeft &&
        _currentMouse.X < HintButtonLeft + HintButtonSize &&
        _currentMouse.Y >= HintButtonTop &&
        _currentMouse.Y < HintButtonTop + HintButtonSize;

    private bool TryReadMinesweeperClick(out GridPosition cell, out bool isFlagClick)
    {
        cell = default;
        isFlagClick = false;
        var leftPressed = _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        var rightPressed = _currentMouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;

        if (!leftPressed && !rightPressed)
        {
            return false;
        }

        var relativeX = _currentMouse.X - GameConstants.MinefieldBoardLeft;
        var relativeY = _currentMouse.Y - GameConstants.MinefieldBoardTop;
        if (relativeX < 0 || relativeY < 0 ||
            relativeX >= GameConstants.RoomColumns * GameConstants.MinefieldCellSize ||
            relativeY >= GameConstants.RoomRows * GameConstants.MinefieldCellSize)
        {
            return false;
        }

        cell = new GridPosition(
            relativeY / GameConstants.MinefieldCellSize,
            relativeX / GameConstants.MinefieldCellSize);
        isFlagClick = rightPressed;
        return true;
    }
}
