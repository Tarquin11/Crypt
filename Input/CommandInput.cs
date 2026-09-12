using Crypt.Core.Engine;
using Microsoft.Xna.Framework.Input;

namespace Crypt.Input;

/// <summary>Turns MonoGame's held-key polling into the game's discrete command stream.</summary>
public sealed class CommandInput
{
    private KeyboardState _previous;
    private KeyboardState _current;

    public bool QuitRequested { get; private set; }

    public IReadOnlyList<GameCommand> Read(GameState state)
    {
        _current = Keyboard.GetState();
        var commands = new List<GameCommand>();
        QuitRequested = WasPressed(Keys.Escape);

        if (WasPressed(Keys.R))
        {
            commands.Add(GameCommand.Restart);
        }

        if (state is GameState.Dialogue or GameState.GameOver)
        {
            if (WasPressed(Keys.Enter) || WasPressed(Keys.Z))
            {
                commands.Add(GameCommand.Confirm);
            }
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
        return commands;
    }

    private bool WasPressed(Keys key) => _current.IsKeyDown(key) && _previous.IsKeyUp(key);
}
