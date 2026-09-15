using Crypt.Core.Engine;
using Crypt.Core.Entities;
using Crypt.Core.Puzzles;
using Crypt.Core.Utility;
using Crypt.Core.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crypt.Rendering;

/// <summary>Draws the current playable slice with pixel-art primitives and a SpriteFont.</summary>
public sealed class GameRenderer
{
    private const int Pixel = GameConstants.PixelSize;

    private static readonly Color Background = new(0x0A, 0x0D, 0x18);
    private static readonly Color BackgroundDust = new(0x1D, 0x28, 0x42);

    private static readonly Color FrameShadow = new(0x05, 0x06, 0x0A);
    private static readonly Color FrameDark = new(0x22, 0x25, 0x3D);
    private static readonly Color FrameMid = new(0x3D, 0x42, 0x69);
    private static readonly Color FrameLight = new(0x72, 0x79, 0xA9);

    private static readonly Color TileOutline = new(0x15, 0x16, 0x24);
    private static readonly Color Stone = new(0x45, 0x48, 0x63);
    private static readonly Color StoneLight = new(0x68, 0x6E, 0x92);
    private static readonly Color StoneShadow = new(0x2D, 0x30, 0x47);
    private static readonly Color StoneChip = new(0x85, 0x8B, 0xAB);

    private static readonly Color CrackWash = new(0x62, 0x46, 0x53);
    private static readonly Color CrackDark = new(0x25, 0x1B, 0x2A);
    private static readonly Color CrackLight = new(0xC1, 0x7B, 0x5F);

    private static readonly Color LavaDark = new(0x4B, 0x19, 0x20);
    private static readonly Color LavaRed = new(0xA8, 0x2D, 0x28);
    private static readonly Color LavaOrange = new(0xED, 0x6A, 0x2D);
    private static readonly Color LavaHot = new(0xFF, 0xD1, 0x66);

    private static readonly Color PlateOutline = new(0x20, 0x1B, 0x35);
    private static readonly Color Plate = new(0x6E, 0x5A, 0xA8);
    private static readonly Color PlateLight = new(0xB8, 0xA8, 0xF0);

    private static readonly Color DoorOutline = new(0x19, 0x14, 0x21);
    private static readonly Color DoorWood = new(0x75, 0x4D, 0x38);
    private static readonly Color DoorOpen = new(0x47, 0x78, 0x50);
    private static readonly Color DoorMetal = new(0xC3, 0xB0, 0x8A);

    private static readonly Color HeroOutline = new(0x15, 0x14, 0x24);
    private static readonly Color HeroHelmet = new(0x81, 0x75, 0xB8);
    private static readonly Color HeroHelmetLight = new(0xB3, 0xA8, 0xE8);
    private static readonly Color HeroSkin = new(0xE9, 0xAD, 0x74);
    private static readonly Color HeroArmor = new(0x3E, 0x8A, 0x92);
    private static readonly Color HeroArmorLight = new(0x69, 0xC6, 0xC7);
    private static readonly Color HeroCape = new(0xB3, 0x48, 0x59);
    private static readonly Color HeroBoot = new(0x35, 0x27, 0x3E);
    private static readonly Color HeroEye = new(0x1B, 0x1A, 0x2B);

    private static readonly Color SwordHilt = new(0xC6, 0x8D, 0x4F);
    private static readonly Color SwordOutline = new(0x29, 0x27, 0x3A);
    private static readonly Color SwordBlade = new(0xD7, 0xED, 0xF3);

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;

    public GameRenderer(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _font = font;
    }

    public void Draw(DungeonSession session, TimeSpan now)
    {
        DrawBackground();

        if (session.State == GameState.Title)
        {
            DrawMainMenu(session);
            return;
        }

        if (session.State == GameState.IntroStory)
        {
            DrawIntroStoryBackdrop();
            DrawDialogue(session.Dialogue);
            return;
        }

        DrawBoardFrame();

        foreach (var position in session.Room.Positions())
        {
            DrawTile(session.Room, position, now);
        }

        if (session.Room.AnswerSigilPuzzle is { } answerSigilPuzzle)
        {
            DrawAnswerSigils(answerSigilPuzzle);
        }

        if (session.Room.HasSpecialMovePaper && session.Room.SpecialMovePaper is { } paper)
        {
            DrawSpecialMovePaper(paper, now);
        }

        if (session.Room.IsEchoRoom)
        {
            foreach (var decoy in session.Room.EchoDecoys)
            {
                DrawEchoDecoy(decoy, now);
            }
        }

        if (session.Room.HasLibraryBook && session.Room.LibraryBook is { } libraryBook)
        {
            DrawLibraryBook(libraryBook, now);
        }

        DrawDoor(session.Room, session.Player);

        if (session.Room.IsKeyRevealed && !session.Player.HasKey && session.Room.RevealedKey is { } key)
        {
            DrawKey(key, now);
        }

        DrawPlayer(session.Player, now);
        DrawHeader(session, now);

        switch (session.State)
        {
            case GameState.PaperReading:
                DrawPaperReading(session);
                break;
            case GameState.LibraryReading:
                DrawLibraryBookReading(session);
                break;
            case GameState.CipherPuzzle:
                DrawCipherPuzzle(session);
                break;
            case GameState.Minesweeper:
                DrawMinesweeperPuzzle(session);
                break;
            case GameState.Dying:
                DrawDyingOverlay(session.GetDeathProgress(now), session.DeathReason, session.DeathKind, session.Player);
                break;
            case GameState.DeathRoast:
                DrawDeathRoast(session);
                break;
            case GameState.GameOver:
                DrawGameOver();
                break;
            case GameState.Victory:
                DrawVictory(session.CurrentLevel);
                break;
        }

        DrawDialogue(session.Dialogue);
    }

    private void DrawMainMenu(DungeonSession session)
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(7, 9, 18));

        DrawText("CRYPT", new Vector2(370, 92), LavaHot, 2f);
        DrawText("PLACEHOLDER LOGO", new Vector2(406, 150), new Color(0xAE, 0xBD, 0xCA), 0.55f);

        FillRectangle(454, 202, 92, 92, FrameShadow);
        FillRectangle(460, 208, 80, 80, new Color(0x45, 0x2A, 0x59));
        FillRectangle(472, 220, 56, 12, PlateLight);
        FillRectangle(484, 232, 32, 40, new Color(0x1B, 0x1D, 0x31));
        FillRectangle(492, 244, 16, 16, LavaHot);

        DrawMenuButton("PLAY", GameConstants.MenuPlayTop, true);

        DrawMenuButton(
            "LANGUAGE  -  COMING SOON",
            GameConstants.MenuLanguageTop,
            false);

        DrawMenuButton(
            $"DIFFICULTY  -  {session.Difficulty.ToString().ToUpperInvariant()}",
            GameConstants.MenuDifficultyTop,
            true);

        DrawText(
            "CLICK DIFFICULTY OR PRESS D",
            new Vector2(394, 550),
            new Color(0xAE, 0xBD, 0xCA),
            0.55f);

        DrawText(
            "ENTER OR CLICK PLAY",
            new Vector2(412, 580),
            new Color(0xAE, 0xBD, 0xCA),
            0.55f);
    }

    private void DrawMenuButton(string label, int top, bool enabled)
    {
        var x = GameConstants.MenuButtonLeft;
        var width = GameConstants.MenuButtonWidth;
        var height = GameConstants.MenuButtonHeight;

        var border = enabled ? FrameLight : FrameMid;
        var fill = enabled ? new Color(0x2B, 0x2D, 0x46) : new Color(0x22, 0x25, 0x3D);
        var textColor = enabled ? new Color(0xF8, 0xF0, 0xDA) : new Color(0x6F, 0x78, 0x93);

        FillRectangle(x + 4, top + 4, width, height, FrameShadow);
        FillRectangle(x, top, width, height, border);
        FillRectangle(x + 4, top + 4, width - 8, height - 8, fill);

        var labelWidth = MeasureText(label, 0.72f);

        DrawText(
            label,
            new Vector2(x + ((width - labelWidth) / 2f), top + 14),
            textColor,
            0.72f);
    }

    private void DrawIntroStoryBackdrop()
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(7, 9, 18));

        DrawText(
            "THE CRYPT AWAKENS",
            new Vector2(338, 92),
            LavaHot,
            1.15f);

        FillRectangle(438, 166, 124, 230, FrameShadow);
        FillRectangle(446, 174, 108, 214, new Color(0x22, 0x25, 0x3D));
        FillRectangle(470, 204, 60, 184, new Color(0x1B, 0x1D, 0x31));
        FillRectangle(490, 274, 20, 20, PlateLight);
    }

    private void DrawBackground()
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, Background);

        for (var y = 18; y < GameConstants.CanvasHeight; y += 44)
        {
            for (var x = 20; x < GameConstants.CanvasWidth; x += 52)
            {
                if (((x / 52) + ((y / 44) * 2)) % 5 == 0)
                {
                    FillRectangle(x, y, 2, 2, BackgroundDust);
                }
            }
        }
    }

    private void DrawBoardFrame()
    {
        var width = GameConstants.RoomColumns * GameConstants.TileSize;
        var height = GameConstants.RoomRows * GameConstants.TileSize;
        var x = GameConstants.BoardLeft - 12;
        var y = GameConstants.BoardTop - 12;

        FillRectangle(x + 5, y + 6, width + 24, height + 24, FrameShadow);
        FillRectangle(x, y, width + 24, height + 24, FrameDark);
        FillRectangle(x + 4, y + 4, width + 16, height + 16, FrameMid);
        FillRectangle(x + 4, y + 4, width + 16, 4, FrameLight);
        FillRectangle(x + 4, y + 4, 4, height + 16, FrameLight);
        FillRectangle(x + 4, y + height + 16, width + 16, 4, FrameShadow);
        FillRectangle(x + width + 16, y + 4, 4, height + 16, FrameShadow);
    }

    private void DrawTile(Room room, GridPosition position, TimeSpan now)
    {
        var x = TileX(position);
        var y = TileY(position);
        var tile = room.TileAt(position);

        switch (tile.Type)
        {
            case TileType.Safe:
                DrawStoneTile(x, y, position);
                break;
            case TileType.Cracked:
                DrawCrackedTile(x, y, position);
                break;
            case TileType.Lava:
                DrawLavaTile(x, y, now);
                break;
        }

        if (tile.IsPressurePlate && tile.Type != TileType.Lava)
        {
            DrawRuneTablet(x, y, room.IsCipherActive, now);
        }

        if (position == room.Entrance)
        {
            DrawEntranceRune(x, y);
        }
    }

    private void DrawStoneTile(int x, int y, GridPosition position)
    {
        FillRectangle(x, y, GameConstants.TileSize, GameConstants.TileSize, TileOutline);
        FillRectangle(x + 2, y + 2, GameConstants.TileSize - 4, GameConstants.TileSize - 4, Stone);
        FillRectangle(x + 4, y + 4, GameConstants.TileSize - 8, 4, StoneLight);
        FillRectangle(x + 4, y + GameConstants.TileSize - 8, GameConstants.TileSize - 8, 4, StoneShadow);
        FillRectangle(x + GameConstants.TileSize - 8, y + 4, 4, GameConstants.TileSize - 8, StoneShadow);

        switch (Math.Abs((position.Row * 13) + (position.Column * 7)) % 3)
        {
            case 0:
                FillRectangle(x + 10, y + 16, 12, 4, StoneChip);
                FillRectangle(x + 39, y + 11, 8, 4, StoneChip);
                FillRectangle(x + 29, y + 43, 14, 4, StoneChip);
                break;
            case 1:
                FillRectangle(x + 14, y + 31, 8, 4, StoneChip);
                FillRectangle(x + 40, y + 22, 12, 4, StoneChip);
                FillRectangle(x + 8, y + 48, 10, 4, StoneChip);
                break;
            default:
                FillRectangle(x + 28, y + 13, 10, 4, StoneChip);
                FillRectangle(x + 13, y + 40, 14, 4, StoneChip);
                FillRectangle(x + 45, y + 47, 6, 4, StoneChip);
                break;
        }
    }

    private void DrawCrackedTile(int x, int y, GridPosition position)
    {
        DrawStoneTile(x, y, position);
        FillRectangle(x + 6, y + 6, GameConstants.TileSize - 12, GameConstants.TileSize - 12, CrackWash);
        FillRectangle(x + 30, y + 8, 4, 16, CrackDark);
        FillRectangle(x + 26, y + 20, 8, 4, CrackDark);
        FillRectangle(x + 22, y + 24, 4, 12, CrackDark);
        FillRectangle(x + 18, y + 32, 8, 4, CrackDark);
        FillRectangle(x + 34, y + 24, 4, 12, CrackDark);
        FillRectangle(x + 38, y + 32, 10, 4, CrackDark);
        FillRectangle(x + 44, y + 36, 4, 12, CrackDark);
        FillRectangle(x + 34, y + 12, 4, 8, CrackLight);
        FillRectangle(x + 38, y + 28, 4, 4, CrackLight);
    }

    private void DrawLavaTile(int x, int y, TimeSpan now)
    {
        var phase = (int)((now.TotalMilliseconds / 150d) % 4);
        FillRectangle(x, y, GameConstants.TileSize, GameConstants.TileSize, TileOutline);
        FillRectangle(x + 2, y + 2, GameConstants.TileSize - 4, GameConstants.TileSize - 4, LavaDark);
        FillRectangle(x + 5, y + 7, GameConstants.TileSize - 10, GameConstants.TileSize - 14, LavaRed);

        if (phase is 0 or 2)
        {
            FillRectangle(x + 9, y + 16, 16, 4, LavaOrange);
            FillRectangle(x + 34, y + 28, 18, 4, LavaOrange);
            FillRectangle(x + 16, y + 43, 20, 4, LavaOrange);
        }
        else
        {
            FillRectangle(x + 29, y + 14, 18, 4, LavaOrange);
            FillRectangle(x + 10, y + 31, 17, 4, LavaOrange);
            FillRectangle(x + 37, y + 45, 12, 4, LavaOrange);
        }

        FillRectangle(x + 18 + (phase * 2), y + 24, 8, 4, LavaHot);
        FillRectangle(x + 39 - (phase * 2), y + 39, 8, 4, LavaHot);
    }

    private void DrawRuneTablet(int x, int y, bool isActive, TimeSpan now)
    {
        var pulse = isActive && ((int)(now.TotalMilliseconds / 180d) % 2 == 0);
        var runeGlow = pulse ? LavaHot : PlateLight;
        var body = isActive ? new Color(0x82, 0x67, 0xC6) : Plate;

        // A raised monolith, rather than a floor switch: it signals that this
        // object is something to read and solve.
        FillRectangle(x + 16, y + 12, 32, 44, PlateOutline);
        FillRectangle(x + 19, y + 15, 26, 37, body);
        FillRectangle(x + 22, y + 18, 20, 4, PlateLight);
        FillRectangle(x + 12, y + 54, 40, 6, PlateOutline);
        FillRectangle(x + 16, y + 51, 32, 5, FrameMid);

        // Three simple rune marks: a player can spot this from across the room.
        FillRectangle(x + 28, y + 25, 8, 4, runeGlow);
        FillRectangle(x + 24, y + 29, 4, 8, runeGlow);
        FillRectangle(x + 36, y + 29, 4, 8, runeGlow);
        FillRectangle(x + 28, y + 37, 8, 4, runeGlow);
        FillRectangle(x + 28, y + 42, 4, 4, runeGlow);
        FillRectangle(x + 32, y + 46, 4, 4, runeGlow);
    }

    private void DrawAnswerSigils(AnswerSigilPuzzle puzzle)
    {
        for (var index = 0; index < puzzle.Positions.Count; index++)
        {
            var position = puzzle.Positions[index];
            var x = TileX(position);
            var y = TileY(position);
            var label = puzzle.Challenge.Answers[index];
            var labelScale = label.Length > 2 ? 0.65f : 0.82f;
            var labelWidth = MeasureText(label, labelScale);

            FillRectangle(x + 8, y + 8, 48, 48, PlateOutline);
            FillRectangle(x + 12, y + 12, 40, 40, new Color(0x5D, 0x4A, 0x92));
            FillRectangle(x + 16, y + 16, 32, 32, new Color(0xA5, 0x8B, 0xD1));
            FillRectangle(x + 20, y + 20, 24, 24, new Color(0x31, 0x2A, 0x57));
            FillRectangle(x + 12, y + 12, 40, 3, PlateLight);
            FillRectangle(x + 12, y + 49, 40, 3, FrameShadow);
            DrawText(label, new Vector2(x + ((GameConstants.TileSize - labelWidth) / 2f), y + 23), new Color(0xFF, 0xF4, 0xD2), labelScale);
        }
    }

    private void DrawEntranceRune(int x, int y)
    {
        FillRectangle(x + 12, y + 12, 40, 4, FrameLight);
        FillRectangle(x + 12, y + 12, 4, 40, FrameLight);
        FillRectangle(x + 48, y + 12, 4, 40, FrameLight);
        FillRectangle(x + 28, y + 22, 8, 4, HeroArmorLight);
        FillRectangle(x + 28, y + 34, 8, 4, HeroArmorLight);
    }

    private void DrawDoor(Room room, Player player)
    {
        var x = TileX(room.Door);
        var y = TileY(room.Door);
        FillRectangle(x + 8, y + 5, 48, 56, DoorOutline);
        FillRectangle(x + 12, y + 9, 40, 52, player.HasKey ? DoorOpen : DoorWood);
        FillRectangle(x + 14, y + 13, 36, 4, DoorMetal);
        FillRectangle(x + 14, y + 31, 36, 4, DoorMetal);
        FillRectangle(x + 30, y + 9, 4, 52, DoorOutline);
        FillRectangle(x + 42, y + 41, 4, 4, DoorMetal);
    }

    private void DrawKey(GridPosition position, TimeSpan now)
    {
        var bob = (int)((now.TotalMilliseconds / 250d) % 2) * 2;
        var x = TileX(position) + 20;
        var y = TileY(position) + 25 - bob;
        FillRectangle(x - 4, y + 4, 20, 12, DoorOutline);
        FillRectangle(x, y, 12, 4, LavaHot);
        FillRectangle(x - 4, y + 4, 4, 8, LavaHot);
        FillRectangle(x + 8, y + 4, 4, 8, LavaHot);
        FillRectangle(x, y + 12, 12, 4, LavaHot);
        FillRectangle(x + 12, y + 4, 20, 4, LavaHot);
        FillRectangle(x + 24, y + 8, 4, 8, LavaHot);
        FillRectangle(x + 16, y + 8, 4, 4, LavaHot);
    }

    private void DrawPlayer(Player player, TimeSpan now)
    {
        var progress = player.GetMoveProgress(now);
        var tileX = MathHelper.Lerp(TileX(player.MoveStart), TileX(player.MoveTarget), progress);
        var tileY = MathHelper.Lerp(TileY(player.MoveStart), TileY(player.MoveTarget), progress);
        var x = (int)MathF.Round(tileX) + 14;
        var y = (int)MathF.Round(tileY) + 12;
        var bob = progress < 1f ? (int)MathF.Round(MathF.Sin(progress * MathF.PI) * Pixel) : 0;

        FillRectangle(x + 4, y + 40, 28, 4, FrameShadow);
        DrawHero(x, y - bob, player.Facing);

        if (player.State == PlayerState.Attacking)
        {
            var swingProgress = player.Sword.GetSwingProgress(now);
            DrawSword(x, y - bob, player.Facing);
            DrawSwingArc(x, y - bob, player.Facing, swingProgress);
        }
    }

    private void DrawHero(int x, int y, Facing facing)
    {
        Block(x, y, 3, 0, 3, 1, HeroOutline);
        Block(x, y, 2, 1, 5, 1, HeroOutline);
        Block(x, y, 1, 2, 7, 2, HeroOutline);
        Block(x, y, 2, 4, 5, 4, HeroOutline);
        Block(x, y, 1, 5, 1, 2, HeroOutline);
        Block(x, y, 7, 5, 1, 2, HeroOutline);
        Block(x, y, 1, 7, 7, 1, HeroOutline);
        Block(x, y, 2, 8, 2, 1, HeroOutline);
        Block(x, y, 5, 8, 2, 1, HeroOutline);

        Block(x, y, 3, 1, 3, 1, HeroHelmet);
        Block(x, y, 2, 2, 5, 2, HeroSkin);
        Block(x, y, 3, 4, 3, 1, HeroArmorLight);
        Block(x, y, 2, 5, 5, 2, HeroArmor);
        Block(x, y, 1, 6, 1, 1, HeroArmor);
        Block(x, y, 7, 6, 1, 1, HeroArmor);
        Block(x, y, 2, 7, 5, 1, HeroCape);
        Block(x, y, 2, 8, 2, 1, HeroBoot);
        Block(x, y, 5, 8, 2, 1, HeroBoot);

        switch (facing)
        {
            case Facing.Left:
                Block(x, y, 2, 2, 1, 1, HeroEye);
                break;
            case Facing.Right:
                Block(x, y, 6, 2, 1, 1, HeroEye);
                break;
            case Facing.Down:
                Block(x, y, 3, 2, 1, 1, HeroEye);
                Block(x, y, 5, 2, 1, 1, HeroEye);
                break;
            case Facing.Up:
                Block(x, y, 2, 2, 5, 2, HeroCape);
                Block(x, y, 3, 2, 3, 1, HeroHelmetLight);
                break;
        }
    }

    private void DrawSwingArc(int x, int y, Facing facing, float progress)
    {
        var frame = Math.Min(2, (int)(progress * 3));

        switch (facing)
        {
            case Facing.Right:
                FillRectangle(x + 40, y + 4 + (frame * 8), 8, 4, new Color(0xFF, 0xF4, 0xD2));
                FillRectangle(x + 36, y + 8 + (frame * 8), 4, 4, new Color(0xFF, 0xF4, 0xD2));
                break;
            case Facing.Left:
                FillRectangle(x - 12, y + 4 + (frame * 8), 8, 4, new Color(0xFF, 0xF4, 0xD2));
                FillRectangle(x - 8, y + 8 + (frame * 8), 4, 4, new Color(0xFF, 0xF4, 0xD2));
                break;
            case Facing.Up:
                FillRectangle(x + 4 + (frame * 8), y - 12, 4, 8, new Color(0xFF, 0xF4, 0xD2));
                FillRectangle(x + 8 + (frame * 8), y - 8, 4, 4, new Color(0xFF, 0xF4, 0xD2));
                break;
            case Facing.Down:
                FillRectangle(x + 4 + (frame * 8), y + 44, 4, 8, new Color(0xFF, 0xF4, 0xD2));
                FillRectangle(x + 8 + (frame * 8), y + 40, 4, 4, new Color(0xFF, 0xF4, 0xD2));
                break;
        }
    }

    private void DrawSword(int x, int y, Facing facing)
    {
        switch (facing)
        {
            case Facing.Right:
                Block(x, y, 7, 5, 1, 1, SwordHilt);
                Block(x, y, 8, 4, 1, 3, SwordOutline);
                Block(x, y, 9, 3, 1, 3, SwordOutline);
                Block(x, y, 9, 3, 1, 2, SwordBlade);
                Block(x, y, 10, 2, 1, 1, SwordOutline);
                break;
            case Facing.Left:
                Block(x, y, 1, 5, 1, 1, SwordHilt);
                Block(x, y, 0, 4, 1, 3, SwordOutline);
                Block(x, y, -1, 3, 1, 3, SwordOutline);
                Block(x, y, -1, 3, 1, 2, SwordBlade);
                Block(x, y, -2, 2, 1, 1, SwordOutline);
                break;
            case Facing.Up:
                Block(x, y, 4, 1, 1, 1, SwordHilt);
                Block(x, y, 3, 0, 3, 1, SwordOutline);
                Block(x, y, 4, -2, 1, 2, SwordOutline);
                Block(x, y, 4, -2, 1, 1, SwordBlade);
                Block(x, y, 4, -3, 1, 1, SwordOutline);
                break;
            case Facing.Down:
                Block(x, y, 4, 8, 1, 1, SwordHilt);
                Block(x, y, 3, 9, 3, 1, SwordOutline);
                Block(x, y, 4, 10, 1, 2, SwordOutline);
                Block(x, y, 4, 10, 1, 1, SwordBlade);
                Block(x, y, 4, 12, 1, 1, SwordOutline);
                break;
        }
    }

    private void DrawHeader(DungeonSession session, TimeSpan now)
    {
        var boardWidth = GameConstants.RoomColumns * GameConstants.TileSize;

        var subtitle = session.CurrentLevel switch
        {
            1 => "LEVEL 1  -  THE FLOOR REMEMBERS",
            2 => "LEVEL 2  -  CRACKED STONE",
            3 => "LEVEL 3  -  FOUR SIGILS",
            4 => "LEVEL 4  -  VIGENERE LIBRARY",
            5 => "LEVEL 5  -  QUIET MINEFIELD",
            6 => "LEVEL 6  -  ECHO ROOM",
            _ => "DUNGEON",
        };

        DrawText(
            subtitle,
            new Vector2(GameConstants.BoardLeft, 30),
            new Color(0xE2, 0xE8, 0xF0),
            0.82f);

        DrawText(
            $"DEATHS {session.TotalDeaths} / {session.DeathsOnCurrentLevel}",
            new Vector2(GameConstants.BoardLeft + boardWidth - 190, 34),
            new Color(0xAE, 0xBD, 0xCA),
            0.54f);

        FillRectangle(
            GameConstants.BoardLeft,
            76,
            boardWidth,
            2,
            new Color(0x25, 0x31, 0x3E));

        if (session.CurrentLevel == 3 &&
            session.State == GameState.Playing &&
            session.Room.AnswerSigilPuzzle is { IsComplete: false } puzzle)
        {
            DrawText(
                puzzle.Challenge.Question.ToUpperInvariant(),
                new Vector2(GameConstants.BoardLeft, 90),
                LavaHot,
                0.68f);

            if (puzzle.Challenge.TimeLimit is { } timeLimit)
            {
                var remaining = session.GetAnswerSigilTimeRemaining(now);
                var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
                var urgency = seconds <= 3
                    ? new Color(0xFF, 0x70, 0x43)
                    : new Color(0xE2, 0xE8, 0xF0);

                var timerX = GameConstants.BoardLeft + 510;
                const int timerY = 61;
                const int timerWidth = 164;
                var fillWidth = (int)MathF.Round(
                    timerWidth * (float)(remaining.TotalSeconds / timeLimit.TotalSeconds));

                DrawText(
                    $"TIME: {seconds}",
                    new Vector2(timerX, timerY),
                    urgency,
                    0.78f);

                FillRectangle(timerX, 68, timerWidth, 4, FrameDark);
                FillRectangle(
                    timerX,
                    68,
                    Math.Clamp(fillWidth, 0, timerWidth),
                    4,
                    urgency);
            }
        }
    }

    private void DrawSpecialMovePaper(GridPosition position, TimeSpan now)
    {
        var x = TileX(position) + 17;
        var y = TileY(position) + 23;
        var shimmer = (int)(now.TotalMilliseconds / 240d) % 2 == 0;

        FillRectangle(x + 3, y + 3, 31, 21, FrameShadow);
        FillRectangle(x, y, 31, 21, new Color(0xD7, 0xC8, 0x9A));
        FillRectangle(x + 3, y + 3, 25, 15, new Color(0xF3, 0xE9, 0xC7));
        FillRectangle(x + 7, y + 7, 16, 2, new Color(0x6B, 0x63, 0x5A));
        FillRectangle(x + 7, y + 11, 12, 2, new Color(0x6B, 0x63, 0x5A));
        FillRectangle(x + 7, y + 15, 14, 2, shimmer ? LavaOrange : new Color(0x6B, 0x63, 0x5A));
    }

    private void DrawEchoDecoy(EchoDecoySymbol decoy, TimeSpan now)
    {
        var x = TileX(decoy.Position) + 16;
        var y = TileY(decoy.Position) + 16;
        var pulse = (int)(now.TotalMilliseconds / 260d) % 2 == 0;

        var glow = pulse
            ? new Color(0xFF, 0x70, 0x43)
            : new Color(0xB8, 0xA8, 0xF0);

        FillRectangle(x + 4, y + 4, 30, 30, FrameShadow);
        FillRectangle(x, y, 30, 30, new Color(0x2B, 0x2D, 0x46));
        FillRectangle(x + 3, y + 3, 24, 24, new Color(0x45, 0x2A, 0x59));

        switch (decoy.Variant)
        {
            case 0:
                FillRectangle(x + 12, y + 6, 4, 18, glow);
                FillRectangle(x + 8, y + 6, 12, 4, glow);
                break;

            case 1:
                FillRectangle(x + 6, y + 12, 18, 4, glow);
                FillRectangle(x + 18, y + 8, 4, 12, glow);
                break;

            default:
                FillRectangle(x + 8, y + 8, 12, 4, glow);
                FillRectangle(x + 8, y + 18, 12, 4, glow);
                FillRectangle(x + 12, y + 8, 4, 14, glow);
                break;
        }
    }

    private void DrawLibraryBook(GridPosition position, TimeSpan now)
    {
        var x = TileX(position) + 18;
        var y = TileY(position) + 17;
        var glint = (int)(now.TotalMilliseconds / 300d) % 2 == 0;
        var cover = new Color(0x45, 0x2A, 0x59);
        var coverLight = new Color(0x76, 0x4B, 0x88);
        var page = new Color(0xE6, 0xD5, 0xA5);

        FillRectangle(x + 3, y + 4, 28, 31, FrameShadow);
        FillRectangle(x, y, 28, 31, cover);
        FillRectangle(x + 4, y + 3, 20, 25, coverLight);
        FillRectangle(x + 7, y + 6, 14, 19, page);
        FillRectangle(x + 13, y + 6, 2, 19, new Color(0x9C, 0x7D, 0x69));
        FillRectangle(x + 9, y + 10, 4, 2, new Color(0x5B, 0x4A, 0x42));
        FillRectangle(x + 16, y + 10, 3, 2, new Color(0x5B, 0x4A, 0x42));
        FillRectangle(x + 9, y + 15, 10, 2, new Color(0x5B, 0x4A, 0x42));
        FillRectangle(x + 10, y + 29, 8, 2, glint ? LavaHot : DoorMetal);
    }

    private void DrawPaperReading(DungeonSession session)
    {
        var pattern = session.Room.SpecialMovePattern;
        var start = session.Room.SpecialMoveStart;
        if (pattern is null || !start.HasValue)
        {
            return;
        }

        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(4, 6, 12, 190));

        const int x = 182;
        const int y = 104;
        const int width = 636;
        const int height = 492;
        var paper = new Color(0xE8, 0xD9, 0xAC);
        var paperLight = new Color(0xF8, 0xEE, 0xC9);
        var paperShade = new Color(0xB5, 0x95, 0x69);
        var ink = new Color(0x2B, 0x27, 0x31);

        FillRectangle(x + 8, y + 10, width, height, FrameShadow);
        FillRectangle(x, y, width, height, paperShade);
        FillRectangle(x + 6, y + 5, width - 12, height - 10, paper);
        FillRectangle(x + 12, y + 11, width - 24, height - 22, paperLight);

        // Parchment grain and worn edge details, built from pixel primitives so no external asset is required.
        for (var grainY = y + 24; grainY < y + height - 24; grainY += 26)
        {
            for (var grainX = x + 24; grainX < x + width - 24; grainX += 38)
            {
                var grainWidth = ((grainX + grainY) / 8) % 3 == 0 ? 8 : 4;
                FillRectangle(grainX, grainY, grainWidth, 2, new Color(0xD1, 0xBE, 0x90));
            }
        }

        FillRectangle(x + 24, y + 30, width - 48, 3, paperShade);
        FillRectangle(x + 24, y + height - 40, width - 48, 3, paperShade);
        FillRectangle(x, y, 26, 26, paperShade);
        FillRectangle(x + width - 26, y + height - 26, 26, 26, paperShade);

        var isEchoRoom = session.Room.IsEchoRoom;

        DrawText(
            isEchoRoom ? "ECHO NOTE" : "FOLDED NOTE",
            new Vector2(x + 42, y + 54),
            ink,
            1.25f);
        DrawText(
            isEchoRoom
                ? "ONLY THIS PATH IS TRUE. IGNORE THE SYMBOLS."
                : "THE RUNES REMEMBER A PATH.",
            new Vector2(x + 42, y + 100),
            new Color(0x5B, 0x4A, 0x42),
            0.74f);
        FillRectangle(x + 42, y + 136, width - 84, 2, paperShade);

        DrawText("BEGIN AT", new Vector2(x + 42, y + 166), new Color(0x5B, 0x4A, 0x42), 0.74f);
        var startText = $"ROW {start.Value.Row + 1}    COLUMN {start.Value.Column + 1}";
        DrawText(startText, new Vector2(x + 42, y + 197), ink, 1.15f);

        DrawText("THEN WALK", new Vector2(x + 42, y + 264), new Color(0x5B, 0x4A, 0x42), 0.74f);
        DrawWrappedText(pattern.InstructionText, x + 42, y + 296, width - 84, 34, ink, 0.82f);

        DrawText("MEMORIZE THIS. THE PAPER WILL CRUMBLE.", new Vector2(x + 42, y + 405), new Color(0x5B, 0x4A, 0x42), 0.67f);
        DrawText("PRESS ENTER OR Z", new Vector2(x + 203, y + 448), new Color(0x5B, 0x4A, 0x42), 0.75f);
    }

    private void DrawLibraryBookReading(DungeonSession session)
    {
        var puzzle = session.Room.CipherPuzzle as VigenereRunePuzzle;
        if (puzzle is null)
        {
            return;
        }

        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(4, 6, 12, 190));

        const int x = 182;
        const int y = 104;
        const int width = 636;
        const int height = 492;
        var paper = new Color(0xE8, 0xD9, 0xAC);
        var paperLight = new Color(0xF8, 0xEE, 0xC9);
        var paperShade = new Color(0xB5, 0x95, 0x69);
        var ink = new Color(0x2B, 0x27, 0x31);

        FillRectangle(x + 8, y + 10, width, height, FrameShadow);
        FillRectangle(x, y, width, height, paperShade);
        FillRectangle(x + 6, y + 5, width - 12, height - 10, paper);
        FillRectangle(x + 12, y + 11, width - 24, height - 22, paperLight);
        FillRectangle(x + 24, y + 30, width - 48, 3, paperShade);
        FillRectangle(x + 24, y + height - 40, width - 48, 3, paperShade);

        DrawText("THE VIGENERE LIBRARY", new Vector2(x + 42, y + 54), ink, 1.08f);
        DrawText("A BOOKMARK HAS BEEN LEFT FOR YOU.", new Vector2(x + 42, y + 102), new Color(0x5B, 0x4A, 0x42), 0.72f);
        FillRectangle(x + 42, y + 136, width - 84, 2, paperShade);

        DrawText("KEYWORD", new Vector2(x + 42, y + 170), new Color(0x5B, 0x4A, 0x42), 0.74f);
        DrawText(puzzle.Key, new Vector2(x + 42, y + 204), new Color(0x45, 0x2A, 0x59), 1.55f);
        FillRectangle(x + 42, y + 264, width - 84, 2, paperShade);

        DrawText("THE WORD REPEATS ACROSS EVERY LETTER.", new Vector2(x + 42, y + 302), ink, 0.73f);
        DrawText("CARRY IT TO THE RUNE LECTERN.", new Vector2(x + 42, y + 336), new Color(0x5B, 0x4A, 0x42), 0.73f);
        DrawText("MEMORIZE THIS. THE BOOK LOCKS WHEN CLOSED.", new Vector2(x + 42, y + 405), new Color(0x5B, 0x4A, 0x42), 0.64f);
        DrawText("PRESS ENTER OR Z", new Vector2(x + 203, y + 448), new Color(0x5B, 0x4A, 0x42), 0.75f);
    }

    private void DrawHearts(int x, int y, Player player)
    {
        for (var index = 0; index < Player.MaxHealth; index++)
        {
            DrawHeart(x + (index * 30), y, index < player.Health);
        }
    }

    private void DrawHeart(int x, int y, bool filled)
    {
        var color = filled ? new Color(0xD9, 0x4B, 0x5D) : new Color(0x45, 0x44, 0x5D);
        FillRectangle(x + 4, y, 8, 4, color);
        FillRectangle(x + 16, y, 8, 4, color);
        FillRectangle(x, y + 4, 28, 8, color);
        FillRectangle(x + 4, y + 12, 20, 4, color);
        FillRectangle(x + 8, y + 16, 12, 4, color);
        FillRectangle(x + 12, y + 20, 4, 4, color);
    }

    private void DrawVictory(int level)
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(5, 13, 20, 199));
        DrawText("ROOM CLEARED", new Vector2(334, 280), new Color(0x5E, 0xEA, 0xD4), 2.2f);
        var instruction = level < 6
            ? $"Press P to pass to Level {level + 1}."
            : "The path held. Press R to play this level again.";
        DrawText(instruction, new Vector2((GameConstants.CanvasWidth - MeasureText(instruction, 1f)) / 2f, 332), new Color(0xE2, 0xE8, 0xF0), 1f);
    }

    private void DrawCipherPuzzle(DungeonSession session)
    {
        var attempt = session.CipherAttempt;
        var puzzle = session.Room.CipherPuzzle;
        if (attempt is null || puzzle is null)
        {
            return;
        }

        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(5, 8, 16, 205));

        const int x = 112;
        const int y = 126;
        const int width = 776;
        const int height = 418;
        FillRectangle(x + 6, y + 6, width, height, FrameShadow);
        FillRectangle(x, y, width, height, FrameLight);
        FillRectangle(x + 5, y + 5, width - 10, height - 10, new Color(0x1B, 0x1D, 0x31));
        FillRectangle(x + 18, y + 18, width - 36, 58, new Color(0x2B, 0x2D, 0x46));
        DrawText(puzzle.PanelTitle, new Vector2(x + 32, y + 31), LavaHot, 1.2f);
        DrawText($"ATTEMPTS: {attempt.AttemptsRemaining}/3", new Vector2(x + 522, y + 35), new Color(0xE2, 0xE8, 0xF0), 0.78f);

        DrawText("DECRYPT THE RUNES", new Vector2(x + 32, y + 105), new Color(0xAE, 0xBD, 0xCA), 0.78f);
        DrawText(puzzle.DisplayedRuneText, new Vector2(x + 32, y + 137), new Color(0xFF, 0xD1, 0x66), 1.2f);

        const int inputX = x + 32;
        const int inputY = y + 206;
        const int inputWidth = width - 64;
        FillRectangle(inputX, inputY, inputWidth, 62, FrameMid);
        FillRectangle(inputX + 4, inputY + 4, inputWidth - 8, 54, new Color(0x0E, 0x10, 0x1E));
        var shownGuess = string.IsNullOrEmpty(attempt.Guess) ? "TYPE ANSWER..." : attempt.Guess;
        var guessColor = string.IsNullOrEmpty(attempt.Guess) ? new Color(0x6F, 0x78, 0x93) : new Color(0xF8, 0xF0, 0xDA);
        DrawText(shownGuess, new Vector2(inputX + 16, inputY + 14), guessColor, 1f);

        DrawText(attempt.Feedback, new Vector2(x + 32, y + 294), new Color(0xAE, 0xBD, 0xCA), 0.78f);

        // The input code uses this same rectangle, so the hint is genuinely clickable.
        const int hintX = 932;
        const int hintY = 18;
        FillRectangle(hintX + 3, hintY + 3, 50, 50, FrameShadow);
        FillRectangle(hintX, hintY, 50, 50, attempt.IsHintVisible ? LavaOrange : Plate);
        DrawText("!", new Vector2(hintX + 18, hintY + 7), new Color(0xFF, 0xF4, 0xD2), 1.35f);

        if (attempt.IsHintVisible)
        {
            FillRectangle(x + 32, y + 328, width - 64, 2, LavaOrange);
            DrawText($"HINT: {puzzle.Hint}", new Vector2(x + 32, y + 348), LavaHot, 0.72f);
            DrawText(puzzle.HintFooter, new Vector2(x + 32, y + 374), LavaHot, 0.72f);
        }
        else
        {
            DrawText("ENTER  SUBMIT     BACKSPACE  ERASE", new Vector2(x + 32, y + 348), new Color(0xE2, 0xE8, 0xF0), 0.76f);
        }
    }

    private void DrawMinesweeperPuzzle(DungeonSession session)
    {
        var puzzle = session.MinesweeperPuzzle;
        if (puzzle is null)
        {
            return;
        }

        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(5, 8, 16, 218));

        const int panelX = 172;
        const int panelY = 74;
        const int panelWidth = 656;
        const int panelHeight = 566;
        FillRectangle(panelX + 6, panelY + 6, panelWidth, panelHeight, FrameShadow);
        FillRectangle(panelX, panelY, panelWidth, panelHeight, FrameLight);
        FillRectangle(panelX + 5, panelY + 5, panelWidth - 10, panelHeight - 10, new Color(0x1B, 0x1D, 0x31));
        FillRectangle(panelX + 18, panelY + 18, panelWidth - 36, 58, new Color(0x2B, 0x2D, 0x46));

        DrawText("MINEFIELD", new Vector2(panelX + 32, panelY + 30), LavaHot, 1.25f);
        DrawText($"MINES: {puzzle.MineCount}     FLAGS: {puzzle.FlagsPlaced}", new Vector2(panelX + 330, panelY + 35), new Color(0xE2, 0xE8, 0xF0), 0.68f);
        DrawText("LEFT CLICK  REVEAL      RIGHT CLICK  FLAG", new Vector2(panelX + 71, panelY + 94), new Color(0xAE, 0xBD, 0xCA), 0.68f);

        for (var row = 0; row < puzzle.Rows; row++)
        {
            for (var column = 0; column < puzzle.Columns; column++)
            {
                var position = new GridPosition(row, column);
                DrawMinefieldCell(position, puzzle.GetCell(position));
            }
        }

        DrawText("THE FIRST REVEAL IS SAFE. CLEAR EVERY SAFE CELL.", new Vector2(panelX + 49, panelY + 530), new Color(0xAE, 0xBD, 0xCA), 0.62f);
    }

    private void DrawMinefieldCell(GridPosition position, MineCellView cell)
    {
        var x = GameConstants.MinefieldBoardLeft + (position.Column * GameConstants.MinefieldCellSize);
        var y = GameConstants.MinefieldBoardTop + (position.Row * GameConstants.MinefieldCellSize);
        const int size = GameConstants.MinefieldCellSize;

        FillRectangle(x, y, size, size, FrameShadow);

        if (!cell.IsRevealed)
        {
            FillRectangle(x + 2, y + 2, size - 4, size - 4, new Color(0x4B, 0x50, 0x73));
            FillRectangle(x + 5, y + 5, size - 10, 4, new Color(0x7A, 0x83, 0xB5));
            FillRectangle(x + 5, y + size - 9, size - 10, 4, new Color(0x2A, 0x2E, 0x48));

            if (cell.IsFlagged)
            {
                DrawText("F", new Vector2(x + 17, y + 11), LavaHot, 1.2f);
            }

            return;
        }

        FillRectangle(x + 2, y + 2, size - 4, size - 4, new Color(0x2A, 0x2E, 0x48));
        if (cell.IsMine)
        {
            FillRectangle(x + 14, y + 14, 22, 22, LavaRed);
            FillRectangle(x + 20, y + 8, 10, 34, LavaHot);
            FillRectangle(x + 8, y + 20, 34, 10, LavaHot);
            return;
        }

        if (cell.AdjacentMines > 0)
        {
            var number = cell.AdjacentMines.ToString();
            const float scale = 1.05f;
            var numberWidth = MeasureText(number, scale);
            DrawText(number, new Vector2(x + ((size - numberWidth) / 2f), y + 10), MineNumberColor(cell.AdjacentMines), scale);
        }
    }

    private static Color MineNumberColor(int mines) => mines switch
    {
        1 => new Color(0x70, 0xCF, 0xFF),
        2 => new Color(0x76, 0xD6, 0x87),
        3 => new Color(0xFF, 0x8A, 0x6A),
        _ => new Color(0xD4, 0x9B, 0xFF),
    };

    private void DrawDyingOverlay(float progress, string reason, DeathKind deathKind, Player player)
    {
        if (deathKind == DeathKind.Sinkhole)
        {
            DrawSinkholeDeath(progress, reason, player);
            return;
        }

        var easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
        var darkness = Math.Clamp(0.18f + (easedProgress * 0.42f), 0.18f, 0.60f);
        var lavaTop = GameConstants.CanvasHeight - (int)MathF.Round(easedProgress * GameConstants.CanvasHeight);

        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(0.08f, 0.01f, 0.03f, darkness));
        FillRectangle(0, lavaTop, GameConstants.CanvasWidth, GameConstants.CanvasHeight - lavaTop, LavaDark);

        for (var x = -16; x < GameConstants.CanvasWidth; x += 44)
        {
            var wave = ((x / 44) % 2 == 0 ? 8 : 0) + (int)(progress * 12f);
            FillRectangle(x, lavaTop - wave, 32, 8, LavaOrange);
            FillRectangle(x + 8, lavaTop - wave - 4, 12, 4, LavaHot);
        }

        var message = string.IsNullOrWhiteSpace(reason) ? "THE LAVA CLOSES IN" : reason.ToUpperInvariant();
        var messageY = 260f - (easedProgress * 90f);
        DrawText(message, new Vector2((GameConstants.CanvasWidth - MeasureText(message, 1.35f)) / 2f, messageY), LavaHot, 1.35f);
    }

    private void DrawSinkholeDeath(float progress, string reason, Player player)
    {
        var easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
        var centerX = TileX(player.Position) + (GameConstants.TileSize / 2);
        var centerY = TileY(player.Position) + (GameConstants.TileSize / 2);
        var radius = 12 + (int)MathF.Round(easedProgress * 82f);

        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(0.02f, 0.01f, 0.05f, 0.42f));
        FillRectangle(centerX - radius - 4, centerY - radius - 4, (radius * 2) + 8, (radius * 2) + 8, new Color(0x4B, 0x19, 0x20));
        FillRectangle(centerX - radius, centerY - radius, radius * 2, radius * 2, new Color(0x12, 0x0A, 0x1E));
        FillRectangle(centerX - radius + 10, centerY - radius + 10, Math.Max(0, (radius * 2) - 20), Math.Max(0, (radius * 2) - 20), Color.Black);

        var message = string.IsNullOrWhiteSpace(reason) ? "THE GROUND GIVES WAY" : reason.ToUpperInvariant();
        DrawText(message, new Vector2((GameConstants.CanvasWidth - MeasureText(message, 1.2f)) / 2f, 88), new Color(0xE2, 0xE8, 0xF0), 1.2f);
    }

    private void DrawGameOver()
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(5, 5, 15, 209));

        const int x = 332;
        const int y = 226;
        const int width = 336;
        const int height = 218;
        FillRectangle(x + 5, y + 5, width, height, new Color(0x07, 0x08, 0x12));
        FillRectangle(x, y, width, height, FrameLight);
        FillRectangle(x + 5, y + 5, width - 10, height - 10, new Color(0x1B, 0x1D, 0x31));
        DrawText("GAME OVER", new Vector2(x + 57, y + 45), new Color(0xFF, 0x70, 0x43), 1.6f);

        const int buttonX = x + 46;
        const int buttonY = y + 132;
        const int buttonWidth = 244;
        const int buttonHeight = 46;
        FillRectangle(buttonX + 3, buttonY + 3, buttonWidth, buttonHeight, FrameShadow);
        FillRectangle(buttonX, buttonY, buttonWidth, buttonHeight, new Color(0x6E, 0x5A, 0xA8));
        FillRectangle(buttonX + 4, buttonY + 4, buttonWidth - 8, buttonHeight - 8, new Color(0xB8, 0xA8, 0xF0));
        DrawText("ENTER / Z  RETRY", new Vector2(buttonX + 36, buttonY + 11), new Color(0x20, 0x22, 0x38), 0.84f);
    }

    private void DrawDeathRoast(DungeonSession session)
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(8, 5, 18, 224));

        const int x = 152;
        const int y = 198;
        const int width = 696;
        const int height = 294;
        FillRectangle(x + 6, y + 6, width, height, FrameShadow);
        FillRectangle(x, y, width, height, new Color(0xB8, 0xA8, 0xF0));
        FillRectangle(x + 5, y + 5, width - 10, height - 10, new Color(0x1B, 0x1D, 0x31));
        FillRectangle(x + 18, y + 18, width - 36, 48, new Color(0x4B, 0x19, 0x20));

        DrawText("THE DUNGEON HAS A NOTE", new Vector2(x + 42, y + 29), LavaHot, 1.05f);
        DrawWrappedText(DungeonSession.ThirdDeathRoast.ToUpperInvariant(), x + 48, y + 103, width - 96, 34, new Color(0xF8, 0xF0, 0xDA), 0.95f);
        DrawText($"TOTAL DEATHS: {session.TotalDeaths}     THIS STAGE: {session.DeathsOnCurrentLevel}", new Vector2(x + 86, y + 222), new Color(0xAE, 0xBD, 0xCA), 0.65f);
        DrawText("PRESS ENTER OR Z", new Vector2(x + 226, y + 254), new Color(0xB8, 0xA8, 0xF0), 0.75f);
    }

    private void DrawDialogue(DialogueState dialogue)
    {
        if (!dialogue.IsVisible)
        {
            return;
        }

        const int x = 18;
        const int y = 526;
        const int width = 964;
        const int height = 156;
        FillRectangle(x + 4, y + 4, width, height, new Color(0x07, 0x08, 0x12));
        FillRectangle(x, y, width, height, new Color(0xF6, 0xE4, 0xBD));
        FillRectangle(x + 4, y + 4, width - 8, height - 8, new Color(0x2B, 0x2D, 0x46));
        FillRectangle(x + 8, y + 8, width - 16, height - 16, new Color(0xF8, 0xF0, 0xDA));
        FillRectangle(x + 18, y + 14, 220, 28, new Color(0x20, 0x22, 0x38));
        DrawText(
            dialogue.Speaker.ToUpperInvariant(),
            new Vector2(x + 30, y + 18),
            new Color(0xFF, 0xF4, 0xD2),
            1f);
        DrawWrappedText(
            dialogue.VisibleText,
            x + 28,
            y + 62,
            width - 56,
            26,
            new Color(0x1A, 0x1B, 0x2B),
            1f);

        if (dialogue.IsCurrentPageComplete)
        {
            var markerX = x + width - 42;
            var markerY = y + height - 30;
            FillRectangle(markerX, markerY, 16, 4, new Color(0x20, 0x22, 0x38));
            FillRectangle(markerX + 4, markerY + 4, 8, 4, new Color(0x20, 0x22, 0x38));
            FillRectangle(markerX + 8, markerY + 8, 4, 4, new Color(0x20, 0x22, 0x38));
        }
    }

    private void DrawWrappedText(string value, int x, int y, int width, int lineHeight, Color color, float scale)
    {
        var lineY = y;

        foreach (var paragraph in value.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lineY += lineHeight;
                continue;
            }

            var line = string.Empty;
            foreach (var word in paragraph.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
                if (!string.IsNullOrEmpty(line) && MeasureText(candidate, scale) > width)
                {
                    DrawText(line, new Vector2(x, lineY), color, scale);
                    lineY += lineHeight;
                    line = string.Empty;
                }

                line = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            }

            if (!string.IsNullOrEmpty(line))
            {
                DrawText(line, new Vector2(x, lineY), color, scale);
                lineY += lineHeight;
            }
        }
    }

    private float MeasureText(string value, float scale) => _font.MeasureString(value).X * scale;

    private void DrawText(string value, Vector2 position, Color color, float scale)
    {
        var snappedPosition = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
        var shadow = new Color(0, 0, 0, color.A / 2);

        _spriteBatch.DrawString(
            _font,
            value,
            snappedPosition + new Vector2(2f, 2f),
            shadow,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f);
        _spriteBatch.DrawString(
            _font,
            value,
            snappedPosition,
            color,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f);
    }

    private void FillRectangle(int x, int y, int width, int height, Color color) =>
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), color);

    private void Block(int x, int y, int column, int row, int width, int height, Color color) =>
        FillRectangle(x + (column * Pixel), y + (row * Pixel), width * Pixel, height * Pixel, color);

    private static int TileX(GridPosition position) => GameConstants.BoardLeft + (position.Column * GameConstants.TileSize);

    private static int TileY(GridPosition position) => GameConstants.BoardTop + (position.Row * GameConstants.TileSize);
}
