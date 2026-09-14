namespace Crypt.Core.Puzzles;

/// <summary>Tracks typed input, hints, and the limited attempts for a cipher panel.</summary>
public sealed class CipherAttemptState
{
    public const int MaximumAttempts = 3;

    private readonly ITextCipherPuzzle _puzzle;

    public CipherAttemptState(ITextCipherPuzzle puzzle)
    {
        _puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
    }

    public string Guess { get; private set; } = string.Empty;

    public int AttemptsUsed { get; private set; }

    public int AttemptsRemaining => MaximumAttempts - AttemptsUsed;

    public bool IsHintVisible { get; private set; }

    public string Feedback { get; private set; } = "Type the decoded phrase, then press ENTER.";

    public void Append(string characters)
    {
        ArgumentNullException.ThrowIfNull(characters);

        foreach (var character in characters)
        {
            if (char.IsAsciiLetter(character) && Guess.Length < 32)
            {
                Guess += char.ToUpperInvariant(character);
            }
            else if (char.IsWhiteSpace(character) && Guess.Length > 0 && Guess[^1] != ' ' && Guess.Length < 32)
            {
                Guess += ' ';
            }
        }
    }

    public void Backspace()
    {
        if (Guess.Length > 0)
        {
            Guess = Guess[..^1];
        }
    }

    public void ToggleHint() => IsHintVisible = !IsHintVisible;

    public CipherSubmission Submit()
    {
        if (MatchesDecodedPhrase(Guess))
        {
            Feedback = "The runes accept your answer.";
            return CipherSubmission.Correct;
        }

        AttemptsUsed++;
        Guess = string.Empty;

        if (AttemptsRemaining == 0)
        {
            Feedback = "The tablet rejects your final answer.";
            return CipherSubmission.Exhausted;
        }

        Feedback = $"Incorrect. {AttemptsRemaining} attempts remain.";
        return CipherSubmission.Incorrect;
    }

    private bool MatchesDecodedPhrase(string guess) =>
        Normalize(guess) == Normalize(_puzzle.DecodedText);

    private static string Normalize(string value) =>
        string.Concat(value.Where(char.IsAsciiLetter).Select(char.ToUpperInvariant));
}

public enum CipherSubmission
{
    Incorrect,
    Correct,
    Exhausted,
}
