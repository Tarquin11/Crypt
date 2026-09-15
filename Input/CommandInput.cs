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
    private Keys? _heldMovementKey;
    private TimeSpan _nextMovementRepeatAt;

    private static readonly TimeSpan MovementRepeatDelay =
        TimeSpan.FromMilliseconds(260);

    private static readonly TimeSpan MovementRepeatInterval =
        TimeSpan.FromMilliseconds(70);

    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public bool QuitRequested { get; private set; }

    public string TypedCipherText { get; private set; } = string.Empty;

    public GridPosition? MinesweeperCell { get; private set; }

    public bool IsMinesweeperFlagClick { get; private set; }

    public IReadOnlyList<GameCommand> Read(GameState state, TimeSpan now)
    {
        _current = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
        var commands = new List<GameCommand>();
        QuitRequested = WasPressed(Keys.Escape);
        TypedCipherText = string.Empty;
        MinesweeperCell = null;
        IsMinesweeperFlagClick = false;

        // R restarts only active gameplay, not menus or dialogue.
        if ((state is GameState.Playing or GameState.Minesweeper) && WasPressed(Keys.R))
        {
            commands.Add(GameCommand.Restart);
        }

        if (state == GameState.Title)
        {
            if (WasPressed(Keys.Enter) ||
                WasPressed(Keys.Space) ||
                WasMenuPlayClicked())
            {
                commands.Add(GameCommand.StartGame);
            }

            if (WasPressed(Keys.D) || WasMenuDifficultyClicked())
            {
                commands.Add(GameCommand.CycleDifficulty);
            }
        }
        else if (state is GameState.Dialogue or
             GameState.IntroStory or
             GameState.PaperReading or
             GameState.LibraryReading or
             GameState.DeathRoast or
             GameState.GameOver)
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
            if (TryReadHeldMovement(now, out var movement))
            {
                commands.Add(movement);
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

    private bool TryReadHeldMovement(TimeSpan now, out GameCommand command)
    {
        var heldMovement = GetHeldMovement();

        if (!heldMovement.HasValue)
        {
            _heldMovementKey = null;
            command = default;
            return false;
        }

        var (key, movementCommand) = heldMovement.Value;

        if (_heldMovementKey != key)
        {
            _heldMovementKey = key;
            _nextMovementRepeatAt = now + MovementRepeatDelay;
            command = movementCommand;
            return true;
        }

        if (now < _nextMovementRepeatAt)
        {
            command = default;
            return false;
        }

        _nextMovementRepeatAt = now + MovementRepeatInterval;
        command = movementCommand;
        return true;
    }

    private (Keys Key, GameCommand Command)? GetHeldMovement()
    {
        if (_current.IsKeyDown(Keys.W) || _current.IsKeyDown(Keys.Up) || _current.IsKeyDown(Keys.Z))
        {
            return (Keys.W, GameCommand.MoveUp);
        }

        if (_current.IsKeyDown(Keys.S) || _current.IsKeyDown(Keys.Down))
        {
            return (Keys.S, GameCommand.MoveDown);
        }

        if (_current.IsKeyDown(Keys.A) || _current.IsKeyDown(Keys.Q) || _current.IsKeyDown(Keys.Left))
        {
            return (Keys.A, GameCommand.MoveLeft);
        }

        if (_current.IsKeyDown(Keys.D) || _current.IsKeyDown(Keys.Right))
        {
            return (Keys.D, GameCommand.MoveRight);
        }

        return null;
    }

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

    private bool WasMenuPlayClicked() =>
        WasLeftClickInside(
            GameConstants.MenuButtonLeft,
            GameConstants.MenuPlayTop,
            GameConstants.MenuButtonWidth,
            GameConstants.MenuButtonHeight);

    private bool WasMenuDifficultyClicked() =>
        WasLeftClickInside(
            GameConstants.MenuButtonLeft,
            GameConstants.MenuDifficultyTop,
            GameConstants.MenuButtonWidth,
            GameConstants.MenuButtonHeight);

    private bool WasLeftClickInside(int x, int y, int width, int height) =>
        _currentMouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released &&
        _currentMouse.X >= x &&
        _currentMouse.X < x + width &&
        _currentMouse.Y >= y &&
        _currentMouse.Y < y + height;

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
            relativeX >= GameConstants.MinefieldColumns * GameConstants.MinefieldCellSize ||
            relativeY >= GameConstants.MinefieldRows * GameConstants.MinefieldCellSize)
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
