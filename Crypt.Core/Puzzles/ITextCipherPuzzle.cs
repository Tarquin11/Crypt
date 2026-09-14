namespace Crypt.Core.Puzzles;

/// <summary>Common player-facing information for a text cipher challenge.</summary>
public interface ITextCipherPuzzle
{
    string DecodedText { get; }

    string DisplayedRuneText { get; }

    string PanelTitle { get; }

    string Hint { get; }

    string HintFooter { get; }
}
