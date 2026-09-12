using Crypt.Core.Engine;
using Crypt.Core.Entities;
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
        DrawBoardFrame();

        foreach (var position in session.Room.Positions())
        {
            DrawTile(session.Room, position, now);
        }

        DrawDoor(session.Room, session.Player);

        if (session.Room.IsKeyRevealed && !session.Player.HasKey && session.Room.RevealedKey is { } key)
        {
            DrawKey(key, now);
        }

        DrawPlayer(session.Player, now);
        DrawHeader(session.Player, session.Room);

        switch (session.State)
        {
            case GameState.Dying:
                DrawDyingOverlay(session.GetDeathProgress(now));
                break;
            case GameState.GameOver:
                DrawGameOver(session.DeathReason);
                break;
            case GameState.Victory:
                DrawVictory();
                break;
        }

        DrawDialogue(session.Dialogue);
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
            DrawPressurePlate(x, y);
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

    private void DrawPressurePlate(int x, int y)
    {
        FillRectangle(x + 14, y + 23, 36, 20, PlateOutline);
        FillRectangle(x + 18, y + 26, 28, 12, Plate);
        FillRectangle(x + 22, y + 29, 20, 4, PlateLight);
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

    private void DrawHeader(Player player, Room room)
    {
        DrawText("CRYPT", new Vector2(GameConstants.BoardLeft, 20), new Color(0xFF, 0x70, 0x43), 1.7f);
        DrawText("LEVEL 1  -  The floor remembers every step", new Vector2(GameConstants.BoardLeft, 61), new Color(0xAE, 0xBD, 0xCA), 0.82f);
        FillRectangle(GameConstants.BoardLeft, 92, 932, 2, new Color(0x25, 0x31, 0x3E));
        DrawText("HP", new Vector2(730, 29), new Color(0xAE, 0xBD, 0xCA), 0.84f);
        DrawHearts(765, 30, player);

        if (room.IsCipherActive && room.CipherPuzzle is not null)
        {
            DrawText(
                $"RUNE LOCK {room.CipherProgress}/{room.CipherPuzzle.Route.Count}",
                new Vector2(522, 61),
                new Color(0xFF, 0xD1, 0x66),
                0.76f);
        }
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

    private void DrawVictory()
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(5, 13, 20, 199));
        DrawText("ROOM CLEARED", new Vector2(334, 280), new Color(0x5E, 0xEA, 0xD4), 2.2f);
        DrawText("The path held. Press R to play another room.", new Vector2(302, 332), new Color(0xE2, 0xE8, 0xF0), 1f);
    }

    private void DrawDyingOverlay(float progress)
    {
        var opacity = Math.Clamp(0.15f + Math.Min(0.60f, progress * 0.60f), 0.15f, 0.75f);
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(0.35f, 0.02f, 0.03f, opacity));
        DrawText("THE LAVA CLOSES IN...", new Vector2(312, 305), LavaHot, 1.35f);
    }

    private void DrawGameOver(string reason)
    {
        FillRectangle(0, 0, GameConstants.CanvasWidth, GameConstants.CanvasHeight, new Color(5, 5, 15, 209));

        const int x = 282;
        const int y = 210;
        const int width = 436;
        const int height = 276;
        FillRectangle(x + 5, y + 5, width, height, new Color(0x07, 0x08, 0x12));
        FillRectangle(x, y, width, height, new Color(0xF6, 0xE4, 0xBD));
        FillRectangle(x + 5, y + 5, width - 10, height - 10, new Color(0x2B, 0x2D, 0x46));
        FillRectangle(x + 10, y + 10, width - 20, height - 20, new Color(0xF8, 0xF0, 0xDA));
        DrawText("YOU DIED", new Vector2(x + 119, y + 44), new Color(0xB5, 0x3D, 0x4B), 1.85f);
        DrawText(reason, new Vector2(x + 38, y + 110), new Color(0x24, 0x24, 0x3A), 0.78f);
        FillRectangle(x + 34, y + 156, width - 68, 2, new Color(0x6B, 0x63, 0x8D));
        DrawText("Z / ENTER  TRY AGAIN", new Vector2(x + 96, y + 188), new Color(0x24, 0x24, 0x3A), 0.82f);
        DrawText("R          RESTART ROOM", new Vector2(x + 80, y + 222), new Color(0x24, 0x24, 0x3A), 0.82f);
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
        DrawText(dialogue.Speaker.ToUpperInvariant(), new Vector2(x + 30, y + 18), new Color(0xFF, 0xF4, 0xD2), 0.88f);
        DrawWrappedText(dialogue.VisibleText, x + 28, y + 62, width - 56, 24, new Color(0x1A, 0x1B, 0x2B), 0.93f);

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
