namespace Crypt.Core.Utility;

public static class GameConstants
{
    public const int RoomColumns = 14;
    public const int RoomRows = 8;
    public const int TileSize = 64;
    public const int CanvasWidth = 1_000;
    public const int CanvasHeight = 700;
    public const int BoardLeft = 34;
    public const int BoardTop = 126;
    public const int PixelSize = 4;
    public const int ForceRevealSafeTiles = 16;
    public const int MinefieldBoardLeft = 250;
    public const int MinefieldBoardTop = 184;
    public const int MinefieldCellSize = 50;
    public const int MinefieldColumns = 10;
    public const int MinefieldRows = 8;
    public const int MenuButtonLeft = 350;
    public const int MenuButtonWidth = 300;
    public const int MenuButtonHeight = 48;
    public const int MenuPlayTop = 350;
    public const int MenuLanguageTop = 416;
    public const int MenuDifficultyTop = 482;

    public static readonly TimeSpan MoveDuration = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan CrackDuration = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan LavaSpreadDelay = TimeSpan.FromMilliseconds(700);
    public static readonly TimeSpan SwordHitStart = TimeSpan.FromMilliseconds(60);
    public static readonly TimeSpan SwordHitEnd = TimeSpan.FromMilliseconds(140);
    public static readonly TimeSpan SwordSwingDuration = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan SwordCooldown = SwordSwingDuration;
    public static readonly TimeSpan DeathSequenceDuration = TimeSpan.FromMilliseconds(1_600);
    public static readonly TimeSpan EnemyHitFlashDuration = TimeSpan.FromMilliseconds(90);
}
