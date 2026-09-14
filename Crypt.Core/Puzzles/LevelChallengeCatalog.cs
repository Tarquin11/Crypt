namespace Crypt.Core.Puzzles;

/// <summary>Reusable challenge data for levels introduced after the first rune cipher.</summary>
public static class LevelChallengeCatalog
{
    private static readonly IReadOnlyDictionary<int, LevelChallenge> Challenges =
        new Dictionary<int, LevelChallenge>
        {
            [1] = new TextCipherChallenge(
                1,
                CipherMethod.Caesar,
                "EAST EAST NORTH",
                "3",
                "Every letter was moved by the same amount."),
            [2] = new TextCipherChallenge(
                2,
                CipherMethod.Vigenere,
                "ATTACK AT DAWN",
                "LEMON",
                "A repeated word can be the key, not just a shift."),
            [4] = new MultipleChoiceChallenge(
                4,
                ChallengeTopic.History,
                "Which civilization built Machu Picchu?",
                ["Roman", "Inca", "Viking", "Mayan"],
                CorrectAnswerIndex: 1,
                MaximumAttempts: 2),
            [5] = new MultipleChoiceChallenge(
                5,
                ChallengeTopic.Geography,
                "Which river runs through Egypt?",
                ["Danube", "Nile", "Amazon", "Yangtze"],
                CorrectAnswerIndex: 1,
                MaximumAttempts: 2),
        };

    public static bool TryGet(int level, out LevelChallenge? challenge) =>
        Challenges.TryGetValue(level, out challenge);

    public static MultipleChoiceChallenge CreateLevelThreeMathChallenge(Random random) =>
        MathChallengeGenerator.Create(random);
}

public abstract record LevelChallenge(int Level);

public sealed record TextCipherChallenge(
    int Level,
    CipherMethod Method,
    string PlainText,
    string Key,
    string Hint) : LevelChallenge(Level)
{
    public string EncodedText => Method switch
    {
        CipherMethod.Caesar => CaesarRunePuzzle.Encode(PlainText, int.Parse(Key)),
        CipherMethod.Vigenere => VigenereCipher.Encode(PlainText, Key),
        _ => throw new ArgumentOutOfRangeException(nameof(Method), Method, null),
    };
}

public sealed record MultipleChoiceChallenge(
    int Level,
    ChallengeTopic Topic,
    string Question,
    IReadOnlyList<string> Answers,
    int CorrectAnswerIndex,
    int MaximumAttempts,
    TimeSpan? TimeLimit = null) : LevelChallenge(Level)
{
    public bool IsCorrectAnswer(int index) => index == CorrectAnswerIndex;
}

public enum CipherMethod
{
    Caesar,
    Vigenere,
}

public enum ChallengeTopic
{
    Math,
    History,
    Geography,
}
